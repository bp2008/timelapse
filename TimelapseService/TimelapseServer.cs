using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Sockets;
using Timelapse.Configuration;
using System.Net;
using System.Security.AccessControl;
using BPUtil.SimpleHttp;
using BPUtil;
using turbojpegCLI;
using BPUtil.MVC;

namespace Timelapse
{
	public class TimelapseServer : HttpServer
	{
		public static SessionManager sm = new SessionManager();
		private static MVCMain mvcApi = new MVCMain(System.Reflection.Assembly.GetExecutingAssembly(), typeof(API.Handlers.AllCameras).Namespace);
		private WebpackProxy webpackProxy = null;
		public TimelapseServer(int port, int port_https)
			: base(port, port_https)
		{
			//Thread thr = new Thread(() =>
			//{
			//	try
			//	{
			//		x264net.X264Options options = new x264net.X264Options(2048, 1536);
			//		options.Quality = 25;
			//		options.QualityMinimum = 40;
			//		options.Preset = x264net.X264Preset.superfast;
			//		options.Profile = x264net.X264Profile.high;
			//		options.Tune = x264net.X264Tune.zerolatency;
			//		options.Threads = 1;
			//		options.MaxBitRate = 1536;
			//		using (FileStream fsOut = new FileStream("out-" + TimeUtil.GetTimeInMsSinceEpoch() + ".h264", FileMode.Create, FileAccess.Write, FileShare.Read))
			//		{
			//			// Create an X264Net instance. Be sure to dispose it when finished, either by calling Dispose() on it, or by creating it in a using block.
			//			using (x264net.X264Net encoder = new x264net.X264Net(options))
			//			{
			//				using (TJDecompressor decomp = new TJDecompressor())
			//				{
			//					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec("alpine2");
			//					string[] fileListURLs = Navigation.GetFileListUrls(cs, "2018/01/23").Split('\n');
			//					foreach (string url in fileListURLs)
			//					{
			//						byte[] jpeg_data = WebServerUtil.GetImageData(url);
			//						decomp.setSourceImage(jpeg_data, jpeg_data.Length);
			//						byte[] rgb_data = decomp.decompress();

			//						byte[] buf = encoder.EncodeFrameAsWholeArray(rgb_data);

			//						// Write frame to file
			//						fsOut.Write(buf, 0, buf.Length);
			//					}
			//				}
			//			}
			//		}
			//	}
			//	catch (Exception ex)
			//	{
			//		Logger.Debug(ex);
			//	}
			//	finally
			//	{
			//		Console.WriteLine("Finished writing h264 file");
			//	}
			//});
			//thr.IsBackground = true;
			//thr.Name = "VideoEncoder";
			//thr.Start();
			if (TimelapseWrapper.cfg.devMode)
			{
				DirectoryInfo debugWWW = GetDebugWWW();
				if (debugWWW != null)
				{
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.WriteLine("Starting web server in dev mode. Webpack Proxy is enabled.");
					Console.ResetColor();
					webpackProxy = new WebpackProxy(9000, debugWWW.Parent.FullName);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Unable to start web server in dev mode because debug www folder is invalid: \"" + TimelapseWrapper.cfg.debugWWWDir + "\"");
					Console.ResetColor();
				}
			}
		}

		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				string requestedPageLower = p.requestedPage.ToLower();

				if (p.requestedPage == "errors")
				{
					string errors = File.ReadAllText(Globals.ErrorFilePath);
					p.writeSuccess();
					p.outputStream.Write(HttpUtility.HtmlEncode(errors).Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>"));
					return;
				}
				if (p.requestedPage == "error")
				{
					Logger.Debug("/error page loaded");
					p.writeSuccess();
					p.outputStream.Write("Error written to log");
					return;
				}
				if (p.requestedPage == "admin")
				{
					p.writeRedirect("admin/main");
					return;
				}

				if (p.requestedPage == "login")
				{
					LogOutUser(p, null);
					return;
				}

				if (p.requestedPage == "testip")
				{
					p.writeSuccess("text/plain; charset=utf-8");
					p.outputStream.Write(p.RemoteIPAddressStr);
					return;
				}

				Session s = sm.GetSession(p.requestCookies.GetValue("tlsess"), p.requestCookies.GetValue("tlauth"), p.GetParam("rawauth"));
				if (s != null && s.sid != null && s.sid.Length == 16)
					p.responseCookies.Add("tlsess", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));

				if (p.requestedPage == "logout")
				{
					LogOutUser(p, s);
					return;
				}


				if (p.requestedPage.StartsWith("admin/"))
				{
					string adminPage = p.requestedPage == "admin" ? "" : p.requestedPage.Substring("admin/".Length);
					if (string.IsNullOrWhiteSpace(adminPage))
						adminPage = "main";
					int idxQueryStringStart = adminPage.IndexOf('?');
					if (idxQueryStringStart == -1)
						idxQueryStringStart = adminPage.Length;
					adminPage = adminPage.Substring(0, idxQueryStringStart);
					Pages.Admin.AdminPage.HandleRequest(adminPage, p, s);
					return;
				}
				else if (p.requestedPage == "Navigation" || p.requestedPage == "NavigationNextDay")
				{
					if (HandlePublicServiceDisabled(p))
						return;
					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec(p.GetParam("cam"));
					if (cs == null || cs.type != CameraType.FTP)
						p.writeFailure("400 Bad Request");
					else if (cs != null && !IpWhitelist.IsWhitelisted(p.RemoteIPAddressStr, cs.ipWhitelist))
						p.writeFailure("403 Forbidden");
					else
					{
						string path = p.GetParam("path");
						p.writeSuccess();
						try
						{
							if (p.requestedPage == "Navigation")
								p.outputStream.Write(Navigation.GetNavHtml(cs, path));
							else
								p.outputStream.Write(Navigation.GetNavHtmlForNextDay(cs, path));
						}
						catch (NavigationException)
						{
							p.outputStream.Write("Server busy. Please reload this page later.");
						}
					}
				}
				else if (p.requestedPage == "TimeZoneList")
				{
					if (HandlePublicServiceDisabled(p))
						return;
					p.writeSuccess();
					p.outputStream.Write(Pages.TimeZoneList.GetHtml());
				}
				else if (requestedPageLower.StartsWith("imgarchive/"))
				{
					p.writeFailure();
				}
				else if (p.requestedPage == "GetFileListUrls")
				{
					if (HandlePublicServiceDisabled(p))
						return;
					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec(p.GetParam("cam"));
					if (cs == null || cs.type != CameraType.FTP)
						p.writeFailure("400 Bad Request");
					else if (cs != null && !IpWhitelist.IsWhitelisted(p.RemoteIPAddressStr, cs.ipWhitelist))
						p.writeFailure("403 Forbidden");
					else
					{
						string path = p.GetParam("path");
						try
						{
							string response = Navigation.GetFileListUrls(cs, path);
							p.writeSuccess("text/plain");
							p.outputStream.Write(response);
						}
						catch (NavigationException)
						{
							p.writeFailure("503 Service Unavailable");
							p.outputStream.Write("");
						}
						catch (Exception ex)
						{
							Logger.Debug(ex);
							p.writeFailure("500 Internal Server Error");
						}
					}
				}
				else
				{
					if (p.requestedPage.StartsWith("TimelapseAPI/", StringComparison.OrdinalIgnoreCase))
					{
						mvcApi.ProcessRequest(p, p.requestedPage.Substring("TimelapseAPI/".Length));
						return;
					}

					CameraSpec cs = null;
					if (p.request_url.Segments.Length > 1)
						cs = TimelapseWrapper.cfg.GetCameraSpec(p.request_url.Segments[1].Trim('/'));
					if (cs != null)
					{
						if (HandlePublicServiceDisabled(p))
							return;

						if (cs != null && !IpWhitelist.IsWhitelisted(p.RemoteIPAddressStr, cs.ipWhitelist))
							p.writeFailure("403 Forbidden");
						else
						{

							// This page is something involving a camera we have configured
							if (p.request_url.Segments.Length == 2)
							{
								if (cs.type == CameraType.ThirdPartyHosted)
									p.writeFailure("400 Bad Request");
								else
									p.writeRedirect("Camera.html?cam=" + cs.id); // Redirect to the camera page for this camera
							}
							else if (p.request_url.Segments.Length >= 3)
							{
								if (p.request_url.Segments.Length == 3 && p.request_url.Segments[2] == "latest.jpg")
								{
									if (cs.type == CameraType.ThirdPartyHosted)
									{
										if (!string.IsNullOrWhiteSpace(cs.path_3rdpartyimgzippedURL))
										{
											WebServerUtil.Handle3rdPartyZippedImage(p, cs);
											return;
										}
										else
										{
											p.writeFailure("400 Bad Request");
											return;
										}
									}
									if (!CameraMaintenance.MaintainCamera(cs))
									{
										p.writeFailure("500 Internal Server Error");
										return;
									}
									string latestImgTime;
									DateTime dateTime;
									string latestImagePathPart;
									try
									{
										latestImagePathPart = Navigation.GetLatestImagePath(cs, out latestImgTime, out dateTime);
									}
									catch (NavigationException)
									{
										p.writeFailure("503 Service Unavailable");
										return;
									}
									string path = cs.id + "/" + latestImagePathPart;

									List<KeyValuePair<string, string>> headers = WebServerUtil.GetCacheEtagHeaders(TimeSpan.Zero, path);
									FileInfo imgFile = new FileInfo(TimelapseGlobals.ImageArchiveDirectoryBase + path);
									headers.Add(new KeyValuePair<string, string>("Content-Disposition", "inline; filename=\"" + cs.name + " " + imgFile.Name.Substring(0, imgFile.Name.Length - imgFile.Extension.Length) + ".jpg\""));

									if (path == p.GetHeaderValue("if-none-match"))
									{
										p.writeSuccess("image/jpeg", -1, "304 Not Modified");
										return;
									}
									byte[] data = WebServerUtil.GetImageData(path);
									p.writeSuccess("image/jpeg", data.Length, additionalHeaders: headers);
									p.outputStream.Flush();
									p.tcpStream.Write(data, 0, data.Length);
								}
								else
								{
									if (cs.type != CameraType.FTP)
									{
										p.writeFailure("400 Bad Request");
										return;
									}

									List<KeyValuePair<string, string>> headers = WebServerUtil.GetCacheEtagHeaders(TimeSpan.FromDays(365), p.requestedPage);
									FileInfo imgFile = new FileInfo(TimelapseGlobals.ImageArchiveDirectoryBase + p.requestedPage);
									headers.Add(new KeyValuePair<string, string>("Content-Disposition", "inline; filename=\"" + cs.name + " " + imgFile.Name.Substring(0, imgFile.Name.Length - imgFile.Extension.Length) + ".jpg\""));

									if (p.requestedPage == p.GetHeaderValue("if-none-match"))
									{
										p.writeSuccess("image/jpeg", -1, "304 Not Modified");
										return;
									}
									byte[] data = WebServerUtil.GetImageData(p.requestedPage);
									p.writeSuccess("image/jpeg", data.Length, additionalHeaders: headers);
									p.outputStream.Flush();
									p.tcpStream.Write(data, 0, data.Length);
								}
							}
							else
								p.writeFailure();
						}
					}
					else
					{
						#region www
						DirectoryInfo WWWDirectory = new DirectoryInfo(TimelapseGlobals.WWWDirectoryBase);
						DirectoryInfo debugWWW = GetDebugWWW();
						if (debugWWW != null)
							WWWDirectory = debugWWW;
						string wwwDirectoryBase = WWWDirectory.FullName.Replace('\\', '/').TrimEnd('/') + '/';
						string reqPage = p.requestedPage;
						//if (reqPage == "")
						//	reqPage = "Default.html";
						FileInfo fi = new FileInfo(wwwDirectoryBase + reqPage);
						string targetFilePath = fi.FullName.Replace('\\', '/');
						if (!targetFilePath.StartsWith(wwwDirectoryBase) || targetFilePath.Contains("../"))
						{
							if (WebServerUtil.HandleAdminConfiguredRedirect(p))
								return;
							p.writeFailure("400 Bad Request");
							return;
						}
						//if (webpackProxy != null)
						//{
						//	// Handle hot module reload provided by webpack dev server.
						//	switch (fi.Extension.ToLower())
						//	{
						//		case ".js":
						//		case ".map":
						//		case ".css":
						//		case ".json":
						//			webpackProxy.Proxy(p);
						//			return;
						//	}
						//}
						if (!fi.Exists)
						{
							if (WebServerUtil.HandleAdminConfiguredRedirect(p))
								return;
							//fi = new FileInfo(wwwDirectoryBase + "Default.html");
							//if (!fi.Exists)
							//{
								p.writeFailure("404 Not Found");
								return;
							//}
						}

						if ((fi.Extension == ".html" || fi.Extension == ".htm") && fi.Length < 256000)
						{
							string html = File.ReadAllText(fi.FullName);
							if (fi.Name.ToLower() == "camera.html")
							{
								// camera.html triggers special behavior

								if (HandlePublicServiceDisabled(p))
									return;

								string camId = p.GetParam("cam");
								cs = TimelapseWrapper.cfg.GetCameraSpec(camId);

								if (cs == null || cs.type == CameraType.ThirdPartyHosted || !CameraMaintenance.MaintainCamera(cs))
								{
									p.writeFailure("400 Bad Request");
									return;
								}

								if (cs != null && !IpWhitelist.IsWhitelisted(p.RemoteIPAddressStr, cs.ipWhitelist))
								{
									p.writeFailure("403 Forbidden");
									return;
								}

								string latestImgTime = "";
								DateTime dateTime = DateTime.MinValue;
								string navMenu;
								string latestImagePathPart;
								try
								{
									navMenu = Navigation.GetNavHtml(cs, Navigation.GetLatestPath(cs, out latestImgTime, out dateTime));
									latestImagePathPart = Navigation.GetLatestImagePath(cs, out latestImgTime, out dateTime);
								}
								catch (NavigationException)
								{
									navMenu = "Server busy. Please reload this page later.";
									latestImagePathPart = "latest";
								}
								html = html.Replace("%NAVMENU%", navMenu);
								html = html.Replace("%TOPMENU%", cs.topMenuHtml);
								html = html.Replace("%CAMID%", cs.id);
								html = html.Replace("%CAMNAME%", cs.name);
								html = html.Replace("%CSS%", WebServerUtil.GetCameraPageStyleCSS(cs));
								html = html.Replace("%CAMFRAME_GRADIENT%", cs.imgBackgroundGradient ? "true" : "false");
								html = html.Replace("%CAMNAME_HTML%", WebServerUtil.GetCameraNameHtml(cs));
								string latestImagePath = cs.id + "/" + latestImagePathPart + ".jpg";
								html = html.Replace("%LATEST_IMAGE%", latestImagePath);
								html = html.Replace("%LATEST_IMAGE_TIME%", HttpUtility.JavaScriptStringEncode(latestImgTime));
							}
							else if (fi.Name.ToLower() == "all.html")
							{
								// all.html triggers special macro strings
								html = html.Replace("%ALL_CAMERAS_JS_ARRAY%", WebServerUtil.GetAllCamerasJavascriptArray(p.RemoteIPAddressStr));
								html = html.Replace("%ALL_PAGE_HEADER%", TimelapseWrapper.cfg.options.allPageHeading);
							}
							try
							{
								html = html.Replace("%REMOTEIP%", p.RemoteIPAddressStr);
								html = html.Replace("%SYSTEM_NAME%", TimelapseWrapper.cfg.options.systemName);
								html = html.Replace("%APP_VERSION%", TimelapseGlobals.Version);
							}
							catch (Exception ex)
							{
								Logger.Debug(ex);
							}
							p.writeSuccess(Mime.GetMimeType(fi.Extension));
							p.outputStream.Write(html);
							p.outputStream.Flush();
						}
						else
						{
							string mime = Mime.GetMimeType(fi.Extension);
							if (p.requestedPage.StartsWith(".well-known/acme-challenge/"))
								mime = "text/plain";
							if (fi.LastWriteTimeUtc.ToString("R") == p.GetHeaderValue("if-modified-since"))
							{
								p.writeSuccess(mime, -1, "304 Not Modified");
								return;
							}
							p.writeSuccess(mime, fi.Length, additionalHeaders: WebServerUtil.GetCacheLastModifiedHeaders(TimeSpan.FromHours(1), fi.LastWriteTimeUtc));
							p.outputStream.Flush();
							using (FileStream fs = fi.OpenRead())
							{
								fs.CopyTo(p.tcpStream);
							}
							p.tcpStream.Flush();
						}
						#endregion
					}
				}
			}
			catch (Exception ex)
			{
				if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
					Logger.Debug(ex);
			}
		}

		private DirectoryInfo GetDebugWWW()
		{
			if (!string.IsNullOrWhiteSpace(TimelapseWrapper.cfg.debugWWWDir))
			{
				DirectoryInfo di = new DirectoryInfo(TimelapseWrapper.cfg.debugWWWDir);
				if (di.Exists)
					return di;
			}
			return null;
		}

		private bool HandlePublicServiceDisabled(HttpProcessor p)
		{
			if (!TimelapseWrapper.cfg.options.enabled)
			{
				p.writeFailure("503 Service Unavailable");
				return true;
			}
			return false;
		}

		private void LogOutUser(HttpProcessor p, Session s)
		{
			if (s != null)
				sm.RemoveSession(s.sid);
			p.responseCookies.Add("tlsess", "", TimeSpan.Zero);
			p.responseCookies.Add("tlauth", "", TimeSpan.Zero);
			p.writeSuccess("text/html");
			p.outputStream.Write(Login.GetString());
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			try
			{
				Session s = sm.GetSession(p.requestCookies.GetValue("tlsess"), p.requestCookies.GetValue("tlauth"));
				if (s != null && s.permission == 100)
					p.responseCookies.Add("tlsess", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));
				else
				{
					p.writeFailure("403 Forbidden");
					return;
				}

				string requestedPage = p.request_url.AbsolutePath.TrimStart('/');
				if (requestedPage == "admin/saveitem")
				{
					string result = TimelapseWrapper.cfg.SaveItem(p);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
				else if (requestedPage == "admin/deleteitems")
				{
					string result = TimelapseWrapper.cfg.DeleteItems(p);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
				else if (requestedPage == "admin/reordercam")
				{
					string result = TimelapseWrapper.cfg.ReorderCam(p);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
				else if (requestedPage == "admin/savelist")
				{
					string result = Pages.Admin.AdminPage.HandleSaveList(p, s);
					p.writeSuccess("text/plain");
					p.outputStream.Write(HttpUtility.HtmlEncode(result));
				}
				else
				{
					if (mvcApi.ProcessRequest(p, p.requestedPage) || p.responseWritten)
						return;
				}
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
		}

		protected override void stopServer()
		{
		}
		public override bool shouldLogRequestsToFile()
		{
			return true;
		}

		/// <summary>
		/// This method must return true for the <see cref="XForwardedForHeader"/> and <see cref="XRealIPHeader"/> flags to be honored.  This method should only return true if the provided remote IP address is trusted to provide the related headers.
		/// </summary>
		/// <param name="remoteIpAddress"></param>
		/// <returns></returns>
		public override bool IsTrustedProxyServer(IPAddress remoteIpAddress)
		{
			if (string.IsNullOrWhiteSpace(TimelapseWrapper.cfg.options.TrustedProxyServers))
				return false;
			return IpWhitelist.IsWhitelisted(remoteIpAddress.ToString(), TimelapseWrapper.cfg.options.TrustedProxyServers);
		}
	}
}