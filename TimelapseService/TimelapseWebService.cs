using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Timelapse;

namespace TimelapseService
{
	public partial class TimelapseWebService : ServiceBase
	{
		static TimelapseWrapper server;
		public TimelapseWebService()
		{
			InitializeComponent();
			server = new TimelapseWrapper(false);
		}

		protected override void OnStart(string[] args)
		{
			server.Start();
		}

		protected override void OnStop()
		{
			server.Stop();
		}
	}
}
