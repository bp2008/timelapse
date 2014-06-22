using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using TimelapseCore.Configuration;

namespace TimelapseCore
{
	public class TimelapseWrapper
	{
		public static volatile bool ApplicationExiting = false;

		TimelapseServer httpServer;
		public static DateTime startTime = DateTime.MinValue;
		public static TimelapseConfig cfg;

		public TimelapseWrapper()
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			System.Net.ServicePointManager.Expect100Continue = false;
			System.Net.ServicePointManager.DefaultConnectionLimit = 640;

			cfg = new TimelapseConfig();
			if (File.Exists(Globals.ConfigFilePath))
				cfg.Load(Globals.ConfigFilePath);
			else
			{
				if (cfg.users.Count == 0)
					cfg.users.Add(new User("admin", "admin", 100));
				cfg.Save(Globals.ConfigFilePath);
			}
			SimpleHttp.SimpleHttpLogger.RegisterLogger(Logger.httpLogger);
		}
		#region Start / Stop
		/// <summary>
		/// Don't call this if the current application is running in ASP.NET
		/// </summary>
		public void Start()
		{
			Stop();

			startTime = DateTime.Now;

			httpServer = new TimelapseServer(cfg.webport, cfg.webport_https);
			httpServer.Start();
		}
		public void Stop()
		{
			if (httpServer != null)
			{
				httpServer.Stop();
				httpServer.Join(1000);
			}
		}
		#endregion

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject == null)
			{
				Logger.Debug("UNHANDLED EXCEPTION - null exception");
			}
			else
			{
				try
				{
					Logger.Debug((Exception)e.ExceptionObject, "UNHANDLED EXCEPTION");
				}
				catch (Exception ex)
				{
					Logger.Debug(ex, "UNHANDLED EXCEPTION - Unable to report exception of type " + e.ExceptionObject.GetType().ToString());
				}
			}
		}
	}
}
