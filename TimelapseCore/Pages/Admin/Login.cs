using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BPUtil.SimpleHttp;

namespace TimelapseCore.Pages.Admin
{
	class Login : AdminBase
	{
		protected override string GetPageHtml(HttpProcessor p, Session s)
		{
			p.responseCookies.Add("cps", "", TimeSpan.Zero);
			p.responseCookies.Add("auth", "", TimeSpan.Zero);
			return TimelapseCore.Login.GetLoginScripts("main") + "<div style=\"margin-bottom: 10px;\">Please log in to continue:</div>" + TimelapseCore.Login.GetLoginBody();
		}
	}
}
