using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimelapseCore.Pages.Admin
{
	class Main : AdminBase
	{
		protected override string GetPageHtml(SimpleHttp.HttpProcessor p, Session s)
		{
			return @"<div id=""maindiv"">Main Page</div>";
		}
	}
}
