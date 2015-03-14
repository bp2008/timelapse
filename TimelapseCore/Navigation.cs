using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TimelapseCore.Configuration;
using System.Web;

namespace TimelapseCore
{
	public static class Navigation
	{
		public static string GetNavHtmlForNextDay(CameraSpec cs, string path)
		{
			string[] parts = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 3)
				return "Invalid path format.";
			else
			{
				try
				{
					int year = int.Parse(parts[0]);
					int month = int.Parse(parts[1]);
					int day = int.Parse(parts[2]);
					// The goal is to find the next day that has images.
					DirectoryInfo diRoot = new DirectoryInfo(Globals.ImageArchiveDirectoryBase + cs.id);
					List<DateTime> monthsAvailable = new List<DateTime>();
					foreach (DirectoryInfo di in diRoot.GetDirectories("*", SearchOption.AllDirectories))
					{
						int y, m;
						if (int.TryParse(di.Name, out m) && m > 0 & m < 13 && di.Parent.Name.Length == 4 && int.TryParse(di.Parent.Name, out y))
							if (y > year || (y == year && m >= month))
								monthsAvailable.Add(new DateTime(y, m, 1));
					}
					foreach (DateTime date in monthsAvailable)
					{
						DateTime d = date;
						int thisMonth = d.Month;
						if (d.Year == year && d.Month == month && d.Day <= day)
							d = d.AddDays((day - d.Day) + 1);
						while (thisMonth == d.Month)
						{
							string testPath = d.Year + "/" + d.Month.ToString().PadLeft(2, '0') + "/" + d.Day.ToString().PadLeft(2, '0');
							FileInfo fiBdl = new FileInfo(Globals.ImageArchiveDirectoryBase + cs.id + "/" + testPath + ".bdl");
							if (fiBdl.Exists)
							{
								List<string> fileNames = FileBundle.FileBundleManager.GetFileList(fiBdl.FullName);
								if (fileNames.Count > 0)
									return GetNavHtml(cs, testPath);
							}
							d = d.AddDays(1);
						}
					}
					return "End of timelapse slideshow.<br/><a href=\"javascript:location.reload();\">Click here to reload the page.</a>";
				}
				catch (Exception)
				{
					return "An error occurred.";
				}
			}
		}
		public static string GetNavHtml(CameraSpec cs, string path)
		{
			StringBuilder sb = new StringBuilder();
			DirectoryInfo diArg = new DirectoryInfo(Globals.ImageArchiveDirectoryBase + cs.id + "/" + path);
			FileInfo fiBdl = new FileInfo(diArg.Parent.FullName + "/" + diArg.Name + ".bdl");

			if (!fiBdl.Exists && !diArg.Exists)
				sb.Append("Directory does not exist.  Try reloading this page.");
			else
			{
				if (diArg.Parent.Name.ToLower() != "imgarchive")
				{
					sb.Append("<div id=\"navheader\">");
					sb.Append("<a class=\"linkup\" href=\"");
					sb.Append("javascript:Navigate('");
					sb.Append(HttpUtility.JavaScriptStringEncode(GetLinkPath(diArg.Parent)));
					sb.Append("')\">UP</a>&nbsp; ");
					sb.Append("<span class=\"directorypath bgcolored\">");
					sb.Append(HttpUtility.JavaScriptStringEncode(GetLinkPath(diArg)));
					sb.Append("</span>");
					sb.Append("</div>");
				}

				List<string> links = new List<string>();

				if (diArg.Exists)
				{
					DirectoryInfo[] dis = diArg.GetDirectories("*", SearchOption.TopDirectoryOnly);
					FileInfo[] fis = diArg.GetFiles("*", SearchOption.TopDirectoryOnly);
					if (dis != null && dis.Length > 0)
					{
						Array.Sort(dis, new FileSystemInfoComparer());
						foreach (DirectoryInfo di in dis)
						{
							links.Add("<a href=\"javascript:Navigate('" + GetLinkPath(di) + "');\">" + di.Name + "</a>");
						}
					}

					if (fis != null && fis.Length > 0)
					{
						Array.Sort(fis, new FileSystemInfoComparer());
						foreach (FileInfo fi in fis)
						{
							if (fi.Extension != null && fi.Extension.ToLower() == ".bdl")
							{
								string nameWithoutExtension = fi.Name.Remove(fi.Name.Length - fi.Extension.Length);
								string fullNameWithoutExtension = fi.FullName.Remove(fi.FullName.Length - fi.Extension.Length);
								links.Add("<a href=\"javascript:Navigate('"
									+ GetLinkPath(new DirectoryInfo(fullNameWithoutExtension))
									+ "');\">" + nameWithoutExtension + "</a>");
							}
						}
					}
				}
				if (fiBdl.Exists)
				{
					string pathRoot = HttpUtility.JavaScriptStringEncode(GetLinkPath(diArg, false));
					List<string> fileNames = FileBundle.FileBundleManager.GetFileList(fiBdl.FullName);
					fileNames.Sort();
					fileNames.Reverse();
					for (int i = 0; i < fileNames.Count; i++)
					{
						string tempF, latestImgTime;
						DateTime time = Util.GetTimestampFromBundleKey(fileNames[i], out tempF);
						if (string.IsNullOrEmpty(tempF))
							latestImgTime = Util.GetDisplayableTime(time, true);
						else
							latestImgTime = Util.GetDisplayableTime(time, false) + " " + Util.Colorize(tempF);
						string fullPath = pathRoot + HttpUtility.JavaScriptStringEncode(fileNames[i]) + ".jpg";
						links.Add("<a id=\"imglnk" + i + "\" href=\"" + fullPath + "\" onclick=\"Img(" + i + "); return false;\">" + latestImgTime + "</a>");
					}
				}

				sb.Append("<div id=\"navlinks\">");
				sb.Append(string.Join("<br/>", links));
				sb.Append("</div>");
			}

			return sb.ToString();
		}
		private static string GetLinkPath(DirectoryInfo di, bool removeCameraId = true)
		{
			Stack<string> stk = new Stack<string>();
			while (di != null && di.Name.ToLower() != "imgarchive")
			{
				stk.Push(di.Name);
				di = di.Parent;
			}
			if (di == null)
				return "";
			if (stk.Count > 0 && removeCameraId)
				stk.Pop(); // This will be the camera ID
			StringBuilder sb = new StringBuilder();
			while (stk.Count > 0)
			{
				sb.Append(stk.Pop());
				sb.Append('/');
			}
			return sb.ToString();
		}
		public static string GetLatestPath(CameraSpec cs, out string latestImgTime)
		{
			return GetLatestPath(Globals.ImageArchiveDirectoryBase + cs.id, false, out latestImgTime);
		}
		private static string GetLatestPath(string path, bool imgPath, out string latestImgTime)
		{
			DirectoryInfo di = new DirectoryInfo(path);
			if (!di.Exists)
			{
				latestImgTime = "";
				return GetLinkPath(di);
			}
			return GetLatestPath(di, imgPath, out latestImgTime);
		}
		private static string GetLatestPath(DirectoryInfo di, bool imgPath, out string latestImgTime)
		{
			DirectoryInfo[] dis = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
			if (dis != null && dis.Length > 0)
			{
				Array.Sort(dis, new FileSystemInfoComparer());
				// Get the first directory in the list. This is alphabetically the last one.
				DirectoryInfo diLatest = dis[0];
				return GetLatestPath(diLatest, imgPath, out latestImgTime);
			}
			else
			{
				FileInfo[] fis = di.GetFiles("*.bdl", SearchOption.TopDirectoryOnly);
				if (fis != null && fis.Length > 0)
				{
					Array.Sort(fis, new FileSystemInfoComparer());
					// Get the first bundle file in the list. This is alphabetically the last one.
					FileInfo fiLatest = fis[0];
					string dirPath = GetLinkPath(new DirectoryInfo(fiLatest.FullName.Remove(fiLatest.FullName.Length - fiLatest.Extension.Length)));
					latestImgTime = "";
					if (!imgPath)
						return dirPath;
					else
					{
						List<string> fileNames = FileBundle.FileBundleManager.GetFileList(fiLatest.FullName);
						fileNames.Sort();
						if (fileNames.Count == 0)
							return "Empty Bundle";
						else
						{
							latestImgTime = GetImgTimeHtml(fileNames[fileNames.Count - 1]);
							return dirPath + fileNames[fileNames.Count - 1];
						}
					}
				}
			}
			latestImgTime = "";
			return GetLinkPath(di);
		}
		public static string GetImgTimeHtml(string bundleKey)
		{
			string tempF;
			DateTime imgTimeStamp = Util.GetTimestampFromBundleKey(bundleKey, out tempF);
			if (string.IsNullOrEmpty(tempF))
				return Util.GetDisplayableTime(imgTimeStamp, true);
			else
				return Util.GetDisplayableTime(imgTimeStamp, false) + " " + Util.Colorize(tempF);
		}
		public static string GetLatestImagePath(CameraSpec cs, out string latestImgTime)
		{
			return GetLatestPath(Globals.ImageArchiveDirectoryBase + cs.id, true, out latestImgTime);
		}
	}
}
