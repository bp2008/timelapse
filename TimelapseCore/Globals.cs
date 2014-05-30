using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TimelapseCore
{
	public static class Globals
	{
		public static string jQueryPath = "//ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js";
		public static string jQueryUIJsPath = "//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/jquery-ui.min.js";
		public static string jQueryUICssPath = "//ajax.googleapis.com/ajax/libs/jqueryui/1.10.4/themes/smoothness/jquery-ui.css";
		//public static string jQueryPath = "../Scripts/jquery-1.11.1.min.js";
		//public static string jQueryUIJsPath = "../Scripts/jquery-ui.min.js";
		//public static string jQueryUICssPath = "../Styles/jquery-ui.css";

		static Globals()
		{
			Initialize(System.Reflection.Assembly.GetExecutingAssembly().Location);
		}
		public static void Initialize(string exePath)
		{
			executablePath = exePath;
			applicationRoot = new FileInfo(executablePath).Directory.FullName.TrimEnd('\\', '/');
			applicationDirectoryBase = applicationRoot + "/";
			configFilePath = applicationDirectoryBase + "Config.cfg";
			wwwDirectoryBase = applicationRoot + "/www/";
		}
		private static string executablePath;
		private static string applicationRoot;
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Does not have trailing '/'.
		/// </summary>
		public static string ApplicationRoot
		{
			get { return applicationRoot; }
		}
		private static string applicationDirectoryBase;
		/// <summary>
		/// Gets the full path to the root directory where the current executable is located.  Includes trailing '/'.
		/// </summary>
		public static string ApplicationDirectoryBase
		{
			get { return applicationDirectoryBase; }
		}
		private static string configFilePath;
		/// <summary>
		/// Gets the full path to the config file.
		/// </summary>
		public static string ConfigFilePath
		{
			get { return configFilePath; }
		}
		private static string wwwDirectoryBase;
		/// <summary>
		/// Gets the full path to the www directory including the trailing '/'.  Just add page name!
		/// </summary>
		public static string WWWDirectoryBase
		{
			get { return wwwDirectoryBase; }
		}
		public static string Version = "0.1";
	}
}
