using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using TimelapseCore;
using System.Threading;

namespace Timelapse
{
	public class Global : System.Web.HttpApplication
	{
		static TimelapseWrapper server;
		protected void Application_Start(object sender, EventArgs e)
		{
			Globals.Initialize(Context.Request.PhysicalApplicationPath + "NoExist.exe");
			server = new TimelapseWrapper();
		}

		protected void Session_Start(object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{

		}

		protected void Application_Error(object sender, EventArgs e)
		{
			try
			{
				Exception ex = Server.GetLastError();
				if (ex is ThreadAbortException)
					return;
				Logger.Debug(ex, "UNHANDLED EXCEPTION");
			}
			catch (Exception) { }
		}

		protected void Session_End(object sender, EventArgs e)
		{

		}

		protected void Application_End(object sender, EventArgs e)
		{

		}
	}
}