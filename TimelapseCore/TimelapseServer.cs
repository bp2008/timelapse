using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using SimpleHttp;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Sockets;
using TimelapseCore.Configuration;

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

				if (requestedPage == "")
				{
					p.writeRedirect("admin/main");
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
				else if (requestedPage == "Navigation")
				{
					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec(p.GetParam("cam"));
					if (cs == null)
						p.writeFailure("400 Bad Request");
					else
					{
						string path = p.GetParam("path");
						p.writeSuccess();
						p.outputStream.Write(Navigation.GetNavHtml(cs, path));
					}
				}
				else if (requestedPage == "TimeZoneList")
				{
					p.writeSuccess();
					p.outputStream.Write(Pages.TimeZoneList.GetHtml());
				}
				else if (requestedPageLower.StartsWith("imgarchive/"))
				{
					p.writeFailure();
				}
				else
				{
					CameraSpec cs = null;
					if (p.request_url.Segments.Length > 1)
						cs = TimelapseWrapper.cfg.GetCameraSpec(p.request_url.Segments[1].Trim('/'));
					if (cs != null)
					{
						// This page is something involving a camera we have configured
						if (p.request_url.Segments.Length == 2)
						{
							// Return the camera page for this camera
							p.writeRedirect("Camera.html?cam=" + cs.id);
						}
						else if (p.request_url.Segments.Length >= 3)
						{
							if (p.request_url.Segments.Length == 3 && p.request_url.Segments[2] == "latest.jpg")
							{
								string path = cs.id + "/" + Navigation.GetLatestImagePath(cs);
								if (path == p.GetHeaderValue("if-none-match"))
								{
									p.writeSuccess("image/jpeg", -1, "304 Not Modified");
									return;
								}
								byte[] data = GetImageData(path);
								p.writeSuccess("image/jpeg", data.Length, additionalHeaders: GetCacheEtagHeaders(TimeSpan.Zero, path));
								p.outputStream.Flush();
								p.rawOutputStream.Write(data, 0, data.Length);
							}
							else
							{
								if (requestedPage == p.GetHeaderValue("if-none-match"))
								{
									p.writeSuccess("image/jpeg", -1, "304 Not Modified");
									return;
								}
								byte[] data = GetImageData(requestedPage);
								p.writeSuccess("image/jpeg", data.Length, additionalHeaders: GetCacheEtagHeaders(TimeSpan.FromDays(365), requestedPage));
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
						DirectoryInfo WWWDirectory = new DirectoryInfo(Globals.WWWDirectoryBase);
						string wwwDirectoryBase = WWWDirectory.FullName.Replace('\\', '/').TrimEnd('/') + '/';
						FileInfo fi = new FileInfo(wwwDirectoryBase + requestedPage);
						string targetFilePath = fi.FullName.Replace('\\', '/');
						if (!targetFilePath.StartsWith(wwwDirectoryBase) || targetFilePath.Contains("../"))
						{
							p.writeFailure("400 Bad Request");
							return;
						}
						if (!fi.Exists)
						{
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
								string camId = p.GetParam("cam");
								cs = TimelapseWrapper.cfg.GetCameraSpec(camId);
								if (cs == null || !MaintainCamera(cs))
								{
									p.writeFailure("400 Bad Request");
									return;
								}
								html = html.Replace("%NAVMENU%", Navigation.GetNavHtml(cs, Navigation.GetLatestPath(cs)));
								html = html.Replace("%CAMID%", cs.id);
								html = html.Replace("%CAMNAME%", cs.name);
								string latestImagePath = cs.id + "/" + Navigation.GetLatestImagePath(cs) + ".jpg";
								html = html.Replace("%LATEST_IMAGE%", latestImagePath);
							}
							try
							{
								html = html.Replace("%REMOTEIP%", p.RemoteIPAddress);
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
							if (fi.LastWriteTimeUtc.ToString("R") == p.GetHeaderValue("if-modified-since"))
							{
								p.writeSuccess("image/jpeg", -1, "304 Not Modified");
								return;
							}
							p.writeSuccess(Mime.GetMimeType(fi.Extension), additionalHeaders: GetCacheLastModifiedHeaders(TimeSpan.FromDays(1), fi.LastWriteTimeUtc));
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
			FileInfo fi = new FileInfo(Globals.ImageArchiveDirectoryBase + path);
			string fileName = fi.Name.EndsWith(".jpg") ? fi.Name.Remove(fi.Name.Length - ".jpg".Length) : fi.Name;
			FileInfo bundleFile = new FileInfo(fi.Directory.FullName.TrimEnd('/', '\\') + ".bdl");
			if (bundleFile.Exists)
			{
				IDictionary<string, byte[]> data = FileBundle.FileBundleManager.GetFiles(bundleFile.FullName, fileName);
				if (data.Count == 1)
					return data[fileName];
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
							DateTime fileTimeStamp = GetImageTimestamp(cs, rxDateTime, fi);

							string fileKey = Util.GetBundleKeyForTimestamp(fileTimeStamp);
							string bundleFilePath = Util.GetBundleFilePathForTimestamp(cs, fileTimeStamp);

							byte[] data = File.ReadAllBytes(fi.FullName);

							filesToSave.Clear();
							filesToSave.Add(fileKey, data);
							FileBundle.FileBundleManager.SaveFiles(bundleFilePath, filesToSave);

							// Delete the original file
							fi.Delete();
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

		private static DateTime GetImageTimestamp(CameraSpec cs, Regex rxDateTime, FileInfo fi)
		{
			DateTime fileTimeStamp = DateTime.Now;
			if (cs.timestampType == TimestampType.File_Created)
			{
				fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.File_Modified)
			{
				fileTimeStamp = Util.FromUTC(fi.LastWriteTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.DateTime_FromBinary)
			{
				string name = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
				long binaryForm;
				bool success = false;
				if (long.TryParse(name, out binaryForm))
				{
					try
					{
						fileTimeStamp = DateTime.FromBinary(binaryForm); // We are honoring the sender's timestamp without modifying the time zone.
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
					Logger.Debug("Regular expression match failed for camera '" + cs.id + "' file named '" + fi.Name + "'.");
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
				if (s.permission == 100)
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

		public override void stopServer()
		{
		}
	}
}