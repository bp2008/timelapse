using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using Timelapse.Configuration;
using BPUtil;
using BPUtil.SimpleHttp;

namespace Timelapse
{
	public class TimelapseWrapper
	{
		public static volatile bool ApplicationExiting = false;

		TimelapseServer httpServer;
		public static DateTime startTime = DateTime.MinValue;
		public static TimelapseConfig cfg;
		public event EventHandler<string> SocketBound = delegate { };

		public TimelapseWrapper(bool isAspNet)
		{
			if (!isAspNet)
			{
				Logger.logType = Environment.UserInteractive ? (LoggingMode.Console | LoggingMode.File) : LoggingMode.File;
				TimelapseGlobals.Initialize(System.Reflection.Assembly.GetEntryAssembly().Location);
				AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			}

			System.Net.ServicePointManager.Expect100Continue = false;
			if (!isAspNet)
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
			SimpleHttpLogger.RegisterLogger(Logger.httpLogger);

			// The ASP.NET implementation does not currently support request throttling, though output throttling should be more-or-less possible easily.
			GlobalThrottledStream.ThrottlingManager.Initialize(3);
			GlobalThrottledStream.ThrottlingManager.SetBytesPerSecond(0, cfg.options.uploadBytesPerSecond);
			GlobalThrottledStream.ThrottlingManager.SetBytesPerSecond(1, cfg.options.downloadBytesPerSecond);
			GlobalThrottledStream.ThrottlingManager.SetBytesPerSecond(2, -1);
			GlobalThrottledStream.ThrottlingManager.BurstIntervalMs = cfg.options.throttlingGranularity;
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
			httpServer.XForwardedForHeader = true;
			httpServer.SocketBound += HttpServer_SocketBound;
			httpServer.Start();

			Logger.StartLoggingThreads();
		}

		private void HttpServer_SocketBound(object sender, string e)
		{
			SocketBound(this, e);
		}

		public void Stop()
		{
			if (httpServer != null)
			{
				httpServer.Stop();
				httpServer.Join(1000);
			}
			Logger.StopLoggingThreads();
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
