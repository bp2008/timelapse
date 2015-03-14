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

		[EditorName("Upload Throttling")]
		[EditorHint("bytes per second, shared by all users (0 to disable)")]
		public int uploadBytesPerSecond = 0;
		[EditorName("Download Throttling")]
		[EditorHint("bytes per second, shared by all users (0 to disable)<br/>Please note: Under normal circumstances, almost all bandwidth used by Timelapse is <b>upload</b>, not download.<br/>Also note: Throttling has no effect when Timelapse is run in ASP.NET mode.<br/>Only IP addresses outside the server's class C address ranges are throttled.")]
		public int downloadBytesPerSecond = 0;
		[EditorName("Throttling Granularity")]
		[EditorHint("number of milliseconds to wait after each transmission.  A larger number here means data is sent less frequently in larger chunks.")]
		public int throttlingGranularity = 50;

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
		protected override string validateFieldValues()
		{
			if (uploadBytesPerSecond < 0)
				return "0Upload Throttling must not be negative.";
			if (downloadBytesPerSecond < 0)
				return "0Download Throttling must not be negative";
			if (throttlingGranularity < 1 || throttlingGranularity > 1000)
				return "0Throttling Granularity must be between 1 and 1000, inclusively.";
			return "1";
		}
	}
}
