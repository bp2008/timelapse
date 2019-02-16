using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BPUtil;
using BPUtil.SimpleHttp;
using Timelapse.Configuration;

namespace Timelapse
{
	public static class WebServerUtil
	{
		public static string GetAllCamerasJavascriptArray()
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

		public static void Handle3rdPartyZippedImage(HttpProcessor p, CameraSpec cs)
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
							p.tcpStream.Write(jpegData, 0, jpegData.Length);
							p.tcpStream.Flush();
						}
					}
				}
			}
		}

		private static SortedList<string, string> staticUrlRedirections = new SortedList<string, string>();
		private static string lastUrlRedirectionList = "";
		public static bool HandleAdminConfiguredRedirect(HttpProcessor p)
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
		public static string GetCameraNameHtml(CameraSpec cs)
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

		public static string GetCameraPageStyleCSS(CameraSpec cs)
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

		public static List<KeyValuePair<string, string>> GetCacheEtagHeaders(TimeSpan maxAge, string etag)
		{
			if (etag.Length < 2 || !etag.StartsWith("\"") || !etag.EndsWith("\""))
				etag = '"' + etag + '"';
			List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
			additionalHeaders.Add(new KeyValuePair<string, string>("Cache-Control", "max-age=" + (long)maxAge.TotalSeconds + ", public"));
			additionalHeaders.Add(new KeyValuePair<string, string>("ETag", etag));
			return additionalHeaders;
		}
		public static List<KeyValuePair<string, string>> GetCacheLastModifiedHeaders(TimeSpan maxAge, DateTime lastModifiedUTC)
		{
			List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
			additionalHeaders.Add(new KeyValuePair<string, string>("Cache-Control", "max-age=" + (long)maxAge.TotalSeconds + ", public"));
			additionalHeaders.Add(new KeyValuePair<string, string>("Last-Modified", lastModifiedUTC.ToString("R")));
			return additionalHeaders;
		}

		public static byte[] GetImageData(string path)
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
	}
}
