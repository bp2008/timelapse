using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using BPUtil.MVC;
using Timelapse.Configuration;

namespace Timelapse.API.Handlers
{
	public class AllCameras : TimelapseController
	{
		public ActionResult Index()
		{
			IEnumerable<AllPageCameraDef> cams = TimelapseWrapper.cfg.EnabledCameras
				.Where(cam => cam.showOnAllPage)
				.Where(cam => IpWhitelist.IsWhitelisted(Context.httpProcessor.RemoteIPAddressStr, cam.ipWhitelist))
				.Select(cam => cam.GetAllPageCameraDef());
			return Json(cams);
		}
	}
}
