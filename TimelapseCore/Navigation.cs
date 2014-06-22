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
						links.Add("<a id=\"imglnk" + i + "\" href=\"" + fullPath + "\" onclick=\"Img(" + i + ", '" + fullPath + "'); return false;\">" + latestImgTime + "</a>");
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
							string tempF;
							DateTime imgTimeStamp = Util.GetTimestampFromBundleKey(fileNames[fileNames.Count - 1], out tempF);
							if (string.IsNullOrEmpty(tempF))
								latestImgTime = Util.GetDisplayableTime(imgTimeStamp, true);
							else
								latestImgTime = Util.GetDisplayableTime(imgTimeStamp, false) + " " + Util.Colorize(tempF);
							return dirPath + fileNames[fileNames.Count - 1];
						}
					}
				}
			}
			latestImgTime = "";
			return GetLinkPath(di);
		}

		public static string GetLatestImagePath(CameraSpec cs, out string latestImgTime)
		{
			return GetLatestPath(Globals.ImageArchiveDirectoryBase + cs.id, true, out latestImgTime);
		}
	}
}
