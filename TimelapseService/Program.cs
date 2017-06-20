using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using BPUtil.Forms;
using TimelapseCore;

namespace TimelapseService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			if (Environment.UserInteractive)
			{
				string Title = "Timelapse " + TimelapseGlobals.Version + " Service Manager";
				string ServiceName = "Timelapse";

				Application.Run(new ServiceManager(Title, ServiceName, null));
			}
			else
			{
				ServiceBase[] ServicesToRun;
				ServicesToRun = new ServiceBase[]
				{
					new TimelapseWebService()
				};
				ServiceBase.Run(ServicesToRun);
			}
		}
	}
}
