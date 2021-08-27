using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil.MVC;
using Timelapse.Configuration;

namespace Timelapse.API.Handlers
{
	public class Image : TimelapseController
	{
		public ActionResult Index(params string[] args)
		{
			if (args.Length == 0)
				return Error("Bad Request", "400 Bad Request");

			CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec(args[0]);
			if (cs == null || !cs.enabled)
				return Error("Camera Not Found", "404 Not Found");

			if (!IpWhitelist.IsWhitelisted(Context.httpProcessor.RemoteIPAddressStr, cs.ipWhitelist))
				return Error("Forbidden", "403 Forbidden");

			string latestImagePathPart;
			try
			{
				latestImagePathPart = Navigation.GetLatestImagePath(cs, out string latestImgTime, out DateTime dateTime);
			}
			catch (NavigationException)
			{
				return Error("Service Unavailable", "503 Service Unavailable");
			}

			string path = cs.id + "/" + latestImagePathPart;

			if (path == Context.httpProcessor.GetHeaderValue("if-none-match"))
				return new StatusCodeResult("304 Not Modified") { ContentType = "image/jpeg" };
			byte[] data = WebServerUtil.GetImageData(path);
			JpegImageResult result = new JpegImageResult(data);

			List<KeyValuePair<string, string>> headers = WebServerUtil.GetCacheEtagHeaders(TimeSpan.Zero, path);
			FileInfo imgFile = new FileInfo(TimelapseGlobals.ImageArchiveDirectoryBase + path);
			headers.Add(new KeyValuePair<string, string>("Content-Disposition", "inline; filename=\"" + cs.name + " " + imgFile.Name.Substring(0, imgFile.Name.Length - imgFile.Extension.Length) + ".jpg\""));
			headers.ForEach(h => result.AddOrUpdateHeader(h.Key, h.Value));

			return result;
		}
		public ActionResult Test(int p1, bool p2)
		{
			return PlainText(p1 + " - " + p2);
		}
	}
}
