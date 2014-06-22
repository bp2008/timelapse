using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimelapseCore.Configuration
{
	[EditorName("Global Options")]
	public class TimelapseGlobalOptions : FieldSettable
	{
		[EditorName("Public Service Enabled")]
		[EditorHint("If unchecked, requests for the /All page, Camera.html, image navigation data, and imagery will return a \"503 Service Unavailable\" response.  The web server will continue running, and the admin pages will function normally.")]
		public bool enabled = true;

		[EditorName("System Name")]
		[EditorHint("<br/>This name is made available to html pages as %SYSTEM_NAME%")]
		public string systemName = "";

		[EditorName("All.html page heading")]
		[EditorHint("<br/>This text is made available to the /all.html page as %ALL_PAGE_HEADER%")]
		public string allPageHeading = "";

		[EditorName("URL Redirections")]
		[EditorHint("Here, you can specify relative URLs that should redirect to other URLs.  URLs entered here are case insensitive.  Format:<br><br>"
			+ "RelativeUrlUserEntered RelativeUrlRedirectedTo<br/>"
			+ "RelativeUrlUserEntered RelativeUrlRedirectedTo<br/>"
			+ "RelativeUrlUserEntered RelativeUrlRedirectedTo<br/>... And so on ...")]
		[EditorUseTextArea("95%", "20")]
		public string UrlRedirectList = "/ All";

		public TimelapseGlobalOptions()
		{
		}
	}
}
