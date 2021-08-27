using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using BPUtil;
using BPUtil.Forms;
using Timelapse;

namespace TimelapseService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			if (Environment.UserInteractive)
			{
				if (Debugger.IsAttached || (args.Length == 1 && args[0] == "cmd"))
				{
					BPUtil.NativeWin.WinConsole.Initialize();
					TimelapseWrapper server = new TimelapseWrapper(false);
					server.SocketBound += Server_SocketBound;
					server.Start();

					do
					{
						Console.WriteLine("Type \"exit\" to close.");
					}
					while (Console.ReadLine().ToLower() != "exit");

					server.Stop();
					return;
				}
				string Title = "Timelapse " + TimelapseGlobals.Version + " Service Manager";
				string ServiceName = "Timelapse";
				ButtonDefinition btnCmd = new ButtonDefinition("Run Command Line", btnCmd_Click);

				Application.Run(new ServiceManager(Title, ServiceName, new ButtonDefinition[] { btnCmd }));
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
		private static void Server_SocketBound(object sender, string e)
		{
			Console.WriteLine(e);
		}
		static void btnCmd_Click(object sender, EventArgs e)
		{
			Process.Start(Application.ExecutablePath, "cmd");
		}
	}
}
