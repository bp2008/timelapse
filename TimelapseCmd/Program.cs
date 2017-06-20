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
			server.SocketBound += Server_SocketBound;
			server.Start();
			
			do
			{
				Console.WriteLine("Type \"exit\" to close.");
			}
			while (Console.ReadLine().ToLower() != "exit");

			server.Stop();
		}

		private static void Server_SocketBound(object sender, string e)
		{
			Console.WriteLine(e);
		}
	}
}
