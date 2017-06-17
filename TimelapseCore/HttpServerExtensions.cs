using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using BPUtil;
using BPUtil.SimpleHttp;

namespace TimelapseCore.Extensions
{
	public static class HttpServerExtensions
	{
		public static void UpdateHttpCookieCollection(this Cookies thisCookies, HttpCookieCollection httpCookieCollection)
		{
			SortedList<string, Cookie> cookieCollection = PrivateAccessor.GetFieldValue<SortedList<string, Cookie>>(thisCookies, "cookieCollection");
			foreach (Cookie cookie in PrivateAccessor.GetFieldValue<Cookies>(thisCookies, "cookieCollection"))
			{
				HttpCookie newCookie = new HttpCookie(cookie.name, cookie.value);
				newCookie.Expires = DateTime.UtcNow + cookie.expire;
				httpCookieCollection.Add(newCookie);
				//Logger.Debug("Response cookie: " + cookie.name + ":" + cookie.value);
			}
		}

		public static Cookies FromHttpCookieCollection(HttpCookieCollection httpCookieCollection)
		{
			Cookies cookies = new Cookies();
			foreach (string key in httpCookieCollection.AllKeys)
			{
				HttpCookie cookie = httpCookieCollection[key];
				cookies.Add(Uri.UnescapeDataString(cookie.Name), Uri.UnescapeDataString(cookie.Value));
				//Logger.Debug("Request cookie: " + Uri.UnescapeDataString(cookie.Name) + ":" + Uri.UnescapeDataString(cookie.Value));
			}
			return cookies;
		}
	}
}
