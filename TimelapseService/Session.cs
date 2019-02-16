using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Timelapse
{
	public class Session
	{
		public string sid;
		private string username;
		public int permission;
		public DateTime expire;
		public int sessionLengthMinutes;

		public Session()
		{
			sessionLengthMinutes = permission = 0;
			expire = DateTime.Now.AddMinutes(-1);
		}

		public Session(string username, int permission, int sessionLengthMinutes)
		{
			if (sessionLengthMinutes < 0)
				sessionLengthMinutes = 0;
			this.username = username;
			this.permission = permission;
			this.sessionLengthMinutes = sessionLengthMinutes;
			expire = DateTime.Now.AddMinutes(sessionLengthMinutes);
			sid = GenerateSid();
		}

		public static string GenerateSid()
		{
			StringBuilder sb = new StringBuilder(16);
			while (sb.Length < 16)
				sb.Append(Util.GetRandomAlphaNumericChar());
			return sb.ToString();
		}
	}
}
