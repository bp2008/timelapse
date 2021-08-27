using NetTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Timelapse
{
	public static class IpWhitelist
	{
		/// <summary>
		/// Returns true if the ip is in the whitelist.
		/// </summary>
		/// <param name="ipStr"></param>
		/// <param name="whitelist"></param>
		/// <returns></returns>
		public static bool IsWhitelisted(string ipStr, string whitelist)
		{
			if (string.IsNullOrWhiteSpace(whitelist))
				return true;
			IPAddress ip = IPAddress.Parse(ipStr);
			string[] parts = whitelist.Split(',');
			foreach (string rangeStr in parts)
			{
				try
				{
					IPAddressRange range = IPAddressRange.Parse(rangeStr);
					if (range.Contains(ip))
						return true;
				}
				catch
				{
				}
			}
			return parts.Length == 0;
		}
	}
}
