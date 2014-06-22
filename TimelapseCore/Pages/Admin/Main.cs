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
			return @"<div id=""maindiv""><p>This is the Timelapse admin landing page.</p><p>Choose an option from the menu to the left, or you can <a href=""edititem?itemtype=globaloptions"">Edit Global Options</a>.</p></div>";
		}
	}
}
