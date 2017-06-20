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
using TimelapseCore.Configuration;
using System.Net;
using System.Security.AccessControl;
using BPUtil.SimpleHttp;
using BPUtil;

namespace TimelapseCore
{
	public class TimelapseServer : HttpServer
	{
		public static SessionManager sm = new SessionManager();
		public TimelapseServer(int port, int port_https)
			: base(port, port_https)
		{
		}
		public override void handleGETRequest(HttpProcessor p)
		{
			try
			{
				string requestedPage = Uri.UnescapeDataString(p.request_url.AbsolutePath.TrimStart('/'));
				string requestedPageLower = requestedPage.ToLower();

				if (requestedPage == "errors")
				{
					string errors = File.ReadAllText(Globals.ErrorFilePath);
					p.writeSuccess();
					p.outputStream.Write(HttpUtility.HtmlEncode(errors).Replace("\r\n", "<br/>").Replace("\r", "<br/>").Replace("\n", "<br/>"));
					return;
				}
				if (requestedPage == "error")
				{
					Logger.Debug("/error page loaded");
					p.writeSuccess();
					p.outputStream.Write("Error written to log");
					return;
				}
				if (requestedPage == "admin")
				{
					p.writeRedirect("admin/main");
					return;
				}

				if (requestedPage == "login")
				{
					LogOutUser(p, null);
					return;
				}

				Session s = sm.GetSession(p.requestCookies.GetValue("cps"), p.requestCookies.GetValue("auth"), p.GetParam("rawauth"));
				if (s != null && s.sid != null && s.sid.Length == 16)
					p.responseCookies.Add("cps", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));

				if (requestedPage == "logout")
				{
					LogOutUser(p, s);
					return;
				}


				if (requestedPage.StartsWith("admin/"))
				{
					string adminPage = requestedPage == "admin" ? "" : requestedPage.Substring("admin/".Length);
					if (string.IsNullOrWhiteSpace(adminPage))
						adminPage = "main";
					int idxQueryStringStart = adminPage.IndexOf('?');
					if (idxQueryStringStart == -1)
						idxQueryStringStart = adminPage.Length;
					adminPage = adminPage.Substring(0, idxQueryStringStart);
					Pages.Admin.AdminPage.HandleRequest(adminPage, p, s);
					return;
				}
				else if (requestedPage == "Navigation" || requestedPage == "NavigationNextDay")
				{
					if (HandlePublicServiceDisabled(p))
						return;
					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec(p.GetParam("cam"));
					if (cs == null || cs.type != CameraType.FTP)
						p.writeFailure("400 Bad Request");
					else
					{
						string path = p.GetParam("path");
						p.writeSuccess();
						try
						{
							if (requestedPage == "Navigation")
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
				else if (requestedPage == "TimeZoneList")
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
				else if (requestedPage == "GetFileListUrls")
				{
					if (HandlePublicServiceDisabled(p))
						return;
					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec(p.GetParam("cam"));
					if (cs == null || cs.type != CameraType.FTP)
						p.writeFailure("400 Bad Request");
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
					CameraSpec cs = null;
					if (p.request_url.Segments.Length > 1)
						cs = TimelapseWrapper.cfg.GetCameraSpec(p.request_url.Segments[1].Trim('/'));
					if (cs != null)
					{
						if (HandlePublicServiceDisabled(p))
							return;

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
										Handle3rdPartyZippedImage(p, cs);
										return;
									}
									else
									{
										p.writeFailure("400 Bad Request");
										return;
									}
								}
								if (!MaintainCamera(cs))
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

								List<KeyValuePair<string, string>> headers = GetCacheEtagHeaders(TimeSpan.Zero, path);
								FileInfo imgFile = new FileInfo(TimelapseGlobals.ImageArchiveDirectoryBase + path);
								headers.Add(new KeyValuePair<string, string>("Content-Disposition", "inline; filename=\"" + cs.name + " " + imgFile.Name.Substring(0, imgFile.Name.Length - imgFile.Extension.Length) + ".jpg\""));

								if (path == p.GetHeaderValue("if-none-match"))
								{
									p.writeSuccess("image/jpeg", -1, "304 Not Modified");
									return;
								}
								byte[] data = GetImageData(path);
								p.writeSuccess("image/jpeg", data.Length, additionalHeaders: headers);
								p.outputStream.Flush();
								p.rawOutputStream.Write(data, 0, data.Length);
							}
							else
							{
								if (cs.type != CameraType.FTP)
								{
									p.writeFailure("400 Bad Request");
									return;
								}

								List<KeyValuePair<string, string>> headers = GetCacheEtagHeaders(TimeSpan.FromDays(365), requestedPage);
								FileInfo imgFile = new FileInfo(TimelapseGlobals.ImageArchiveDirectoryBase + requestedPage);
								headers.Add(new KeyValuePair<string, string>("Content-Disposition", "inline; filename=\"" + cs.name + " " + imgFile.Name.Substring(0, imgFile.Name.Length - imgFile.Extension.Length) + ".jpg\""));

								if (requestedPage == p.GetHeaderValue("if-none-match"))
								{
									p.writeSuccess("image/jpeg", -1, "304 Not Modified");
									return;
								}
								byte[] data = GetImageData(requestedPage);
								p.writeSuccess("image/jpeg", data.Length, additionalHeaders: headers);
								p.outputStream.Flush();
								p.rawOutputStream.Write(data, 0, data.Length);
							}
						}
						else
							p.writeFailure();
					}
					else
					{
						#region www
						DirectoryInfo WWWDirectory = new DirectoryInfo(TimelapseGlobals.WWWDirectoryBase);
						string wwwDirectoryBase = WWWDirectory.FullName.Replace('\\', '/').TrimEnd('/') + '/';
						FileInfo fi = new FileInfo(wwwDirectoryBase + requestedPage);
						string targetFilePath = fi.FullName.Replace('\\', '/');
						if (!targetFilePath.StartsWith(wwwDirectoryBase) || targetFilePath.Contains("../"))
						{
							if (HandleAdminConfiguredRedirect(p))
								return;
							p.writeFailure("400 Bad Request");
							return;
						}
						if (!fi.Exists)
						{
							if (HandleAdminConfiguredRedirect(p))
								return;
							// This should be just a 404 error, but that causes ASP.NET to try to handle the request with another handler.  Like the static file handler.  Which we don't want.
							p.writeFailure("400 Bad Request");
							return;
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

								if (cs == null || cs.type == CameraType.ThirdPartyHosted || !MaintainCamera(cs))
								{
									p.writeFailure("400 Bad Request");
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
								html = html.Replace("%CSS%", GetCameraPageStyleCSS(cs));
								html = html.Replace("%CAMFRAME_GRADIENT%", cs.imgBackgroundGradient ? "true" : "false");
								html = html.Replace("%CAMNAME_HTML%", GetCameraNameHtml(cs));
								string latestImagePath = cs.id + "/" + latestImagePathPart + ".jpg";
								html = html.Replace("%LATEST_IMAGE%", latestImagePath);
								html = html.Replace("%LATEST_IMAGE_TIME%", HttpUtility.JavaScriptStringEncode(latestImgTime));
							}
							else if (fi.Name.ToLower() == "all.html")
							{
								// all.html triggers special macro strings
								html = html.Replace("%ALL_CAMERAS_JS_ARRAY%", GetAllCamerasJavascriptArray());
								html = html.Replace("%ALL_PAGE_HEADER%", TimelapseWrapper.cfg.options.allPageHeading);
							}
							try
							{
								html = html.Replace("%REMOTEIP%", p.RemoteIPAddress);
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
							if (requestedPage.StartsWith(".well-known/acme-challenge/"))
								mime = "text/plain";
							if (fi.LastWriteTimeUtc.ToString("R") == p.GetHeaderValue("if-modified-since"))
							{
								p.writeSuccess(mime, -1, "304 Not Modified");
								return;
							}
							p.writeSuccess(mime, fi.Length, additionalHeaders: GetCacheLastModifiedHeaders(TimeSpan.FromHours(1), fi.LastWriteTimeUtc));
							p.outputStream.Flush();
							using (FileStream fs = fi.OpenRead())
							{
								fs.CopyTo(p.rawOutputStream);
							}
							p.rawOutputStream.Flush();
						}
						#endregion
					}
				}
			}
			catch (Exception ex)
			{
				if (!p.isOrdinaryDisconnectException(ex))
					Logger.Debug(ex);
			}
		}

		private string GetAllCamerasJavascriptArray()
		{
			List<string> camerasList = new List<string>();
			foreach (CameraSpec cs in TimelapseWrapper.cfg.cameras)
			{
				if (cs.showOnAllPage)
				{
					string imgsrc = "", imglink = "", namelink = "";
					long imgDate;
					if (cs.type == CameraType.ThirdPartyHosted)
					{
						imgsrc = cs.path_3rdpartyimg.Replace("%TIME%", DateTime.Now.ToJavaScriptMilliseconds().ToString());
						imglink = cs.path_3rdpartyimglink;
						namelink = cs.path_3rdpartynamelink;
						imgDate = DateTime.UtcNow.ToJavaScriptMilliseconds();
					}
					else
					{
						//imgsrc = cs.id + "/latest.jpg";
						imglink = namelink = "Camera.html?cam=" + cs.id;
						string timeHtml;
						DateTime timeObj;
						try
						{
							Navigation.GetLatestImagePath(cs, out timeHtml, out timeObj);
						}
						catch (NavigationException)
						{
							timeObj = DateTime.Now;
						}
						imgsrc = cs.id + "/latest.jpg";
						imgDate = timeObj.ToJavaScriptMilliseconds();
					}
					camerasList.Add("['" + HttpUtility.JavaScriptStringEncode(cs.id) + "', '" + HttpUtility.JavaScriptStringEncode(cs.name) + "', '" + HttpUtility.JavaScriptStringEncode(imgsrc) + "', '" + HttpUtility.JavaScriptStringEncode(imglink) + "', '" + HttpUtility.JavaScriptStringEncode(namelink) + "', '" + HttpUtility.JavaScriptStringEncode(cs.allPageOverlayMessage) + "', " + imgDate + "]");
				}
			}
			return "[" + string.Join(",", camerasList) + "]";
		}

		private void Handle3rdPartyZippedImage(HttpProcessor p, CameraSpec cs)
		{
			WebClient wc = new WebClient();
			byte[] zipData = wc.DownloadData(cs.path_3rdpartyimgzippedURL);
			using (MemoryStream msZip = new MemoryStream(zipData))
			{
				Ionic.Zip.ZipFile zFile = Ionic.Zip.ZipFile.Read(msZip);
				foreach (Ionic.Zip.ZipEntry entry in zFile.Entries)
				{
					if (entry.FileName.EndsWith(".jpg"))
					{
						using (MemoryStream msJpg = new MemoryStream())
						{
							entry.Extract(msJpg);
							byte[] jpegData = msJpg.ToArray();
							List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
							headers.Add(new KeyValuePair<string, string>("Content-Disposition", "inline; filename=\"" + cs.name + " " + entry.LastModified.ToString("yyyy-MM-dd HH-mm-ss") + ".jpg\""));
							p.writeSuccess("image/jpeg", jpegData.Length, additionalHeaders: headers);
							p.outputStream.Flush();
							p.rawOutputStream.Write(jpegData, 0, jpegData.Length);
							p.rawOutputStream.Flush();
						}
					}
				}
			}
		}

		public static SortedList<string, string> staticUrlRedirections = new SortedList<string, string>();
		private static string lastUrlRedirectionList = "";
		private bool HandleAdminConfiguredRedirect(HttpProcessor p)
		{
			SortedList<string, string> urlRedirections;
			if (lastUrlRedirectionList == TimelapseWrapper.cfg.options.UrlRedirectList)
				urlRedirections = staticUrlRedirections;
			else
			{
				string currentRedirectList = TimelapseWrapper.cfg.options.UrlRedirectList;
				string[] rules = currentRedirectList.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				urlRedirections = new SortedList<string, string>(rules.Length);
				foreach (string urlRedirectionRule in rules)
				{
					string[] urls = urlRedirectionRule.Split(' ');
					if (urls.Length == 2)
						urlRedirections[urls[0].ToLower().TrimStart('/')] = urls[1].TrimStart('/');
				}
				staticUrlRedirections = urlRedirections;
				lastUrlRedirectionList = currentRedirectList;
			}
			string urlTarget;
			if (urlRedirections.TryGetValue(p.request_url.AbsolutePath.ToLower().TrimStart('/'), out urlTarget))
			{
				p.writeRedirect(urlTarget);
				return true;
			}
			return false;
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
		private string GetCameraNameHtml(CameraSpec cs)
		{
			StringBuilder sb = new StringBuilder();

			if (string.IsNullOrWhiteSpace(cs.cameraNameImageUrl))
			{
				string color = string.IsNullOrWhiteSpace(cs.cameraNameColor) ? "#999999" : cs.cameraNameColor;
				string bgColor = string.IsNullOrWhiteSpace(cs.cameraNameBackgroundColor) ? "rgba(0,0,0,0.5)" : cs.cameraNameBackgroundColor;
				sb.Append("<span style=\"color:").Append(color);
				sb.Append(";background-color:").Append(bgColor);
				sb.Append(";font-size:").Append(cs.cameraNameFontSizePts).Append("pt;\">");
				sb.Append(cs.name).Append("</span>");
			}
			else
			{
				sb.Append("<img src=\"");
				sb.Append(HttpUtility.HtmlEncode(cs.cameraNameImageUrl));
				sb.Append("\" title=\"").Append(cs.name);
				sb.Append("\" alt=\"").Append(cs.name);
				sb.Append("\" />");
			}
			return sb.ToString();
		}

		private string GetCameraPageStyleCSS(CameraSpec cs)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("body { ");

			sb.Append("background-color:");
			sb.Append(string.IsNullOrWhiteSpace(cs.backgroundColor) ? "#666666" : HttpUtility.HtmlEncode(cs.backgroundColor));
			sb.Append(";");

			if (!string.IsNullOrWhiteSpace(cs.backgroundImageUrl))
				sb.Append("background-image:url('" + HttpUtility.JavaScriptStringEncode(cs.backgroundImageUrl) + "');");

			if (!string.IsNullOrWhiteSpace(cs.textColor))
				sb.Append("color:").Append(cs.textColor).Append(";");

			sb.Append(" } a { ");

			if (!string.IsNullOrWhiteSpace(cs.linkColor))
				sb.Append("color:").Append(cs.linkColor).Append(";");
			if (!string.IsNullOrWhiteSpace(cs.linkBackgroundColor))
				sb.Append("background-color:").Append(cs.linkBackgroundColor).Append(";");

			sb.Append(" } .bgcolored {");

			if (!string.IsNullOrWhiteSpace(cs.textBackgroundColor))
				sb.Append("background-color:").Append(cs.textBackgroundColor).Append(";");
			sb.Append("}");
			sb.Append(" #navmenuwrapper { font-size:");
			sb.Append(cs.cameraNavigationFontSizePts);
			sb.Append("pt; }");

			return sb.ToString();
		}

		private List<KeyValuePair<string, string>> GetCacheEtagHeaders(TimeSpan maxAge, string etag)
		{
			List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
			additionalHeaders.Add(new KeyValuePair<string, string>("Cache-Control", "max-age=" + (long)maxAge.TotalSeconds + ", public"));
			additionalHeaders.Add(new KeyValuePair<string, string>("ETag", etag));
			return additionalHeaders;
		}
		private List<KeyValuePair<string, string>> GetCacheLastModifiedHeaders(TimeSpan maxAge, DateTime lastModifiedUTC)
		{
			List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
			additionalHeaders.Add(new KeyValuePair<string, string>("Cache-Control", "max-age=" + (long)maxAge.TotalSeconds + ", public"));
			additionalHeaders.Add(new KeyValuePair<string, string>("Last-Modified", lastModifiedUTC.ToString("R")));
			return additionalHeaders;
		}

		private byte[] GetImageData(string path)
		{
			FileInfo fi = new FileInfo(TimelapseGlobals.ImageArchiveDirectoryBase + path);
			string fileName = fi.Name.EndsWith(".jpg") ? fi.Name.Remove(fi.Name.Length - ".jpg".Length) : fi.Name;
			FileInfo bundleFile = new FileInfo(fi.Directory.FullName.TrimEnd('/', '\\') + ".bdl");
			if (bundleFile.Exists)
			{
				int tries = 0;
				int tryLimit = 5;
				while (tries < tryLimit)
				{
					try
					{
						IDictionary<string, byte[]> data = FileBundle.FileBundleManager.GetFiles(bundleFile.FullName, fileName);
						if (data.Count == 1)
							return data[fileName];
						break;
					}
					catch (Exception ex)
					{
						if (++tries < tryLimit)
						{
							Thread.Sleep(1000 * (tries));
							continue;
						}
						Logger.Debug(ex, "Tries: " + tries);
						break;
					}
				}
			}
			return new byte[0];
		}

		private bool MaintainCamera(CameraSpec cs)
		{
			if (cs == null)
				return false;

			// This maintenance lock ensures that only one request tries to maintain the files at once.  Additional concurrent requests simply skip the maintenance step.
			if (cs.MaintenanceLock.Wait(0))
			{
				try
				{

					if (HttpContext.Current != null && HttpContext.Current.Server != null)
						HttpContext.Current.Server.ScriptTimeout = 120;

					// A long-neglected camera will take a long time to maintain.
					// We don't want this request to time out because that could lead to file corruption.
					// So we will try not to spend longer than 25 seconds total, but with a minimum of 10 seconds of actual file copying.

					Stopwatch watchTotal = new Stopwatch();
					watchTotal.Start();

					DirectoryInfo diRoot = new DirectoryInfo(Globals.ApplicationDirectoryBase + cs.path_imgdump);
					if (!diRoot.Exists)
						diRoot.Create();


					FileInfo[] fis = diRoot.GetFiles("*.jpg", SearchOption.TopDirectoryOnly);
					Array.Sort(fis, new FileSystemInfoComparer());


					if (fis.Length > 0)
					{

						Regex rxDateTime = null;
						if (cs.timestampType == TimestampType.Regular_Expression)
							rxDateTime = new Regex(cs.timestamp_regex_input);

						Dictionary<string, byte[]> filesToSave = new Dictionary<string, byte[]>();

						Stopwatch watchCopying = new Stopwatch();
						watchCopying.Start();

						foreach (FileInfo fi in fis)
						{
							if (watchCopying.ElapsedMilliseconds > 10000 && watchTotal.ElapsedMilliseconds > 25000)
								break;
							if (!fi.Name.EndsWith(".jpg"))
								continue;
							// We try to store the images using the camera's local time zone so that
							// the public categorization matches the file system categorization.
							string tempF;
							DateTime fileTimeStamp = GetImageTimestamp(cs, rxDateTime, fi, out tempF);

							string fileKey = Util.GetBundleKeyForTimestamp(fileTimeStamp, tempF);
							string bundleFilePath = Util.GetBundleFilePathForTimestamp(cs, fileTimeStamp);

							int tries = 0;
							int tryLimit = 5;
							while (tries < tryLimit)
							{
								try
								{
									byte[] data = File.ReadAllBytes(fi.FullName);

									filesToSave.Clear();
									filesToSave.Add(fileKey, data);
									FileBundle.FileBundleManager.SaveFiles(bundleFilePath, filesToSave);

									// Delete the original file
									fi.Delete();

									break;
								}
								catch (Exception ex)
								{
									if (++tries < tryLimit)
									{
										Thread.Sleep(1000 * (tries));
										continue;
									}
									Logger.Debug(ex, "Tries: " + tries);
									break;
								}
							}
						}
					}
				}
				finally
				{
					cs.MaintenanceLock.Release();
				}
			}

			return true;
		}

		private static DateTime GetImageTimestamp(CameraSpec cs, Regex rxDateTime, FileInfo fi, out string tempF)
		{
			tempF = "";
			DateTime fileTimeStamp = DateTime.Now;
			if (cs.timestampType == TimestampType.File_Created)
			{
				fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.File_Modified)
			{
				fileTimeStamp = Util.FromUTC(fi.LastWriteTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.DateTime_FromBinary || cs.timestampType == TimestampType.DateTime_FromBinary_With_Temp_F)
			{
				string name = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
				if (cs.timestampType == TimestampType.DateTime_FromBinary_With_Temp_F)
				{
					string[] parts = name.Split(' ');
					if (parts.Length >= 2)
					{
						name = parts[0];
						tempF = parts[1];
					}
				}
				long binaryForm;
				bool success = false;
				if (long.TryParse(name, out binaryForm))
				{
					try
					{
						fileTimeStamp = DateTime.FromBinary(binaryForm); // We are honoring the sender's timestamp without modifying the time zone.
						fileTimeStamp = TimeZoneInfo.ConvertTimeToUtc(fileTimeStamp, TimeZoneInfo.Local);
						fileTimeStamp = Util.FromUTC(fileTimeStamp, cs.timezone);
						success = true;
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
				if (!success)
					fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.Regular_Expression)
			{
				Match m = rxDateTime.Match(fi.Name);
				if (m.Success)
				{
					fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);

					try
					{
						int year = cs.timestamp_regex_capture_year == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_year].Value);
						int month = cs.timestamp_regex_capture_month == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_month].Value);
						int day = cs.timestamp_regex_capture_day == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_day].Value);
						int hour = cs.timestamp_regex_capture_hour == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_hour].Value);
						int minute = cs.timestamp_regex_capture_minute == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_minute].Value);
						int second = cs.timestamp_regex_capture_second == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_second].Value);

						fileTimeStamp = new DateTime(year != -1 ? year : fileTimeStamp.Year,
													month != -1 ? month : fileTimeStamp.Month,
													day != -1 ? day : fileTimeStamp.Day,
													hour != -1 ? hour : fileTimeStamp.Hour,
													minute != -1 ? minute : fileTimeStamp.Minute,
													second != -1 ? second : fileTimeStamp.Second,
													DateTimeKind.Unspecified); // We are honoring the sender's timestamp without modifying the time zone.
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
				else
				{
					Logger.Debug("Regular expression match failed for camera '" + cs.id + "' file named '" + fi.Name + "'. Regex string was \"" + rxDateTime.ToString() + "\"");
					fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
				}
			}
			else
			{
				Logger.Debug("Unknown timestamp type set for camera " + cs.id);
				fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			return fileTimeStamp;
		}

		private void LogOutUser(HttpProcessor p, Session s)
		{
			if (s != null)
				sm.RemoveSession(s.sid);
			p.responseCookies.Add("cps", "", TimeSpan.Zero);
			p.responseCookies.Add("auth", "", TimeSpan.Zero);
			p.writeSuccess("text/html");
			p.outputStream.Write(Login.GetString());
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			try
			{
				Session s = sm.GetSession(p.requestCookies.GetValue("cps"), p.requestCookies.GetValue("auth"));
				if (s != null && s.permission == 100)
					p.responseCookies.Add("cps", s.sid, TimeSpan.FromMinutes(s.sessionLengthMinutes));
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
	}
}