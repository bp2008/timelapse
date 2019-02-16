using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BPUtil;

namespace TimelapseCore
{
	public static partial class TimelapseGlobals
	{
		/// <summary>
		/// Call this to initialize global static variables.
		/// </summary>
		/// <param name="exePath">Pass in the path to the exe in the root directory of the application.  The directory must exist, but the exe name can just be a descriptive exe file name like "My Application.exe" and does not need to exist.</param>
		public static void Initialize(string exePath)
		{
			Globals.Initialize(exePath, "Images/writabledir/");
			PrivateAccessor.SetStaticFieldValue(typeof(Globals), "errorFilePath", Globals.WritableDirectoryBase + "TimelapseErrors.txt");
		}

		private static string wwwDirectoryBase = null;
		/// <summary>
		/// Gets the full path to the www directory including the trailing '/'.  Just add page name!
		/// </summary>
		public static string WWWDirectoryBase
		{
			get
			{
				if (wwwDirectoryBase == null)
					wwwDirectoryBase = Globals.ApplicationRoot + "/www/";
				return wwwDirectoryBase;
			}
		}

		private static string imageArchiveDirectoryBase = null;
		/// <summary>
		/// Gets the full path to the images directory including the trailing '/'.  Just add page name!
		/// </summary>
		public static string ImageArchiveDirectoryBase
		{
			get
			{
				if (imageArchiveDirectoryBase == null)
					imageArchiveDirectoryBase = Globals.ApplicationRoot + "/Images/imgarchive/";
				return imageArchiveDirectoryBase;
			}
		}
		public static string Version = "0.5";
	}
}
