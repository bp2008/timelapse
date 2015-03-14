﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace Timelapse
{
	public class DummyHttpProcessor : SimpleHttp.HttpProcessor
	{
		HttpContext context;
		public DummyHttpProcessor(System.Web.HttpContext context, SimpleHttp.HttpServer srv)
			: base(context.Request.IsSecureConnection)
		{
			this.context = context;
			this.srv = srv;
			//context.Response.BufferOutput = false;
			//rawOutputStream = new SimpleHttp.GlobalThrottledStream(context.Response.OutputStream, 0);
			rawOutputStream = new System.IO.BufferedStream(context.Response.OutputStream);
			outputStream = new StreamWriter(rawOutputStream);
			http_method = context.Request.HttpMethod.ToUpper();
			request_url = context.Request.Url;
			httpHeaders = new Dictionary<string, string>();
			foreach (string key in context.Request.Headers.Keys)
				httpHeaders[key.ToLower()] = context.Request.Headers[key];

			RawPostParams = new SortedList<string, string>();
			PostParams = new SortedList<string, string>();
			foreach (string key in context.Request.Form.Keys)
			{
				string value = context.Request.Form[key];

				if (RawPostParams.ContainsKey(key))
					RawPostParams[key] += "," + value;
				else
					RawPostParams[key] = value;

				string keyLower = key.ToLower();
				if (PostParams.ContainsKey(keyLower))
					PostParams[keyLower] += "," + value;
				else
					PostParams[keyLower] = value;
			}

			RawQueryString = ParseQueryStringArguments(this.request_url.Query, preserveKeyCharacterCase: true);
			QueryString = ParseQueryStringArguments(this.request_url.Query);

			remoteIPAddress = context.Request.UserHostAddress;

			requestCookies = SimpleHttp.Cookies.FromHttpCookieCollection(context.Request.Cookies);
			responseCookies = new SimpleHttp.Cookies();
		}
		public override void writeSuccess(string contentType = "text/html", long contentLength = -1, string responseCode = "200 OK", List<KeyValuePair<string, string>> additionalHeaders = null)
		{
			responseWritten = true;
			context.Response.ContentType = contentType;
			context.Response.StatusCode = TimelapseCore.Util.ParseIntRobust(responseCode);
			context.Response.Status = responseCode;
			if (additionalHeaders != null)
				foreach (var kvp in additionalHeaders)
					context.Response.AddHeader(kvp.Key, kvp.Value);
			responseCookies.UpdateHttpCookieCollection(context.Response.Cookies);
		}
		public override void writeFailure(string code = "404 Not Found", string description = null)
		{
			responseWritten = true;
			context.Response.StatusCode = TimelapseCore.Util.ParseIntRobust(code);
			context.Response.Status = code;
			if (description != null)
				context.Response.StatusDescription = description;
		}
		public override void writeRedirect(string redirectToUrl)
		{
			responseWritten = true;
			context.Response.Redirect(redirectToUrl, false);
		}
	}
}