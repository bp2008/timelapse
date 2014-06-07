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
					sb.Append("<a class=\"linkup\" href=\"");
					sb.Append("javascript:Navigate('");
					sb.Append(HttpUtility.JavaScriptStringEncode(GetLinkPath(diArg.Parent)));
					sb.Append("')\">UP</a>&nbsp; ");
					sb.Append("<span class=\"directorypath\">");
					sb.Append(HttpUtility.JavaScriptStringEncode(GetLinkPath(diArg)));
					sb.Append("</span>");
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
					for(int i = 0; i < fileNames.Count; i++)
					{
						DateTime time = Util.GetTimestampFromBundleKey(fileNames[i]);
						string fullPath = pathRoot + HttpUtility.JavaScriptStringEncode(fileNames[i]) + ".jpg";
						links.Add("<a id=\"imglnk" + i + "\" href=\"" + fullPath + "\" onclick=\"Img(" + i + ", '" + fullPath + "'); return false;\">" + Util.GetDisplayableTime(time, true) + "</a>");
					}
				}

				sb.Append("<div class=\"navlinks\">");
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
		public static string GetLatestPath(CameraSpec cs)
		{
			return GetLatestPath(Globals.ImageArchiveDirectoryBase + cs.id, false);
		}
		private static string GetLatestPath(string path, bool imgPath)
		{
			DirectoryInfo di = new DirectoryInfo(path);
			if (!di.Exists)
				return GetLinkPath(di);
			return GetLatestPath(di, imgPath);
		}
		private static string GetLatestPath(DirectoryInfo di, bool imgPath)
		{
			DirectoryInfo[] dis = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
			if (dis != null && dis.Length > 0)
			{
				Array.Sort(dis, new FileSystemInfoComparer());
				// Get the first directory in the list. This is alphabetically the last one.
				DirectoryInfo diLatest = dis[0];
				return GetLatestPath(diLatest, imgPath);
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
					if (!imgPath)
						return dirPath;
					else
					{
						List<string> fileNames = FileBundle.FileBundleManager.GetFileList(fiLatest.FullName);
						fileNames.Sort();
						if (fileNames.Count == 0)
							return "Empty Bundle";
						else
							return dirPath + fileNames[fileNames.Count - 1];
					}
				}
			}
			return GetLinkPath(di);
		}

		public static string GetLatestImagePath(CameraSpec cs)
		{
			return GetLatestPath(Globals.ImageArchiveDirectoryBase + cs.id, true);
		}
	}
}
