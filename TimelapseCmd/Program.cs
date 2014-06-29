using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimelapseCore;

namespace TimelapseCmd
{
	class Program
	{
		static TimelapseWrapper server;
		static void Main(string[] args)
		{
			server = new TimelapseWrapper(false);
			server.Start();

			List<string> portStrings = new List<string>();

			AddPort(portStrings, TimelapseWrapper.cfg.webport, "http");
			AddPort(portStrings, TimelapseWrapper.cfg.webport_https, "https");
			AddPort(portStrings, TimelapseWrapper.cfg.webSocketPort, "Web Socket, ws://");
			AddPort(portStrings, TimelapseWrapper.cfg.webSocketPort_secure, "Secure Web Socket, wss://");

			if (portStrings.Count == 0)
				Console.WriteLine("CameraProxy Server is not configured to listen on any valid ports.");
			else
				Console.WriteLine("CameraProxy Server listening on port " + string.Join(" and ", portStrings));

			Console.ReadLine();

			server.Stop();
		}
		static void AddPort(List<string> portStrings, int port, string description)
		{
			if (port > -1 && port < 65535)
				portStrings.Add(port + " (" + description + ")");
		}
	}
}
