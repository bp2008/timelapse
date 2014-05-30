using System;
using System.Web;

namespace Timelapse
{
	public class EverythingHandler : IHttpHandler
	{
		/// <summary>
		/// You will need to configure this handler in the web.config file of your 
		/// web and register it with IIS before being able to use it. For more information
		/// see the following link: http://go.microsoft.com/?linkid=8101007
		/// </summary>
		#region IHttpHandler Members

		public bool IsReusable
		{
			// Return false in case your Managed Handler cannot be reused for another request.
			// Usually this would be false in case you have some state information preserved per request.
			get { return true; }
		}


		TimelapseCore.TimelapseServer server = new TimelapseCore.TimelapseServer(-1, -1);
		public void ProcessRequest(HttpContext context)
		{
			SimpleHttp.HttpProcessor processor = new DummyHttpProcessor(context, server);
			if (context.Request.HttpMethod == "GET")
				server.handleGETRequest(processor);
			else if (context.Request.HttpMethod == "POST")
				server.handlePOSTRequest(processor, null);
			try
			{
				if (!processor.responseWritten)
					processor.writeFailure();
				processor.outputStream.Flush();
				processor.rawOutputStream.Flush();
			}
			catch (Exception) { }
		}

		#endregion
	}
}
