using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil.MVC;
using BPUtil.SimpleHttp;
using Newtonsoft.Json;

namespace Timelapse.API
{
	public abstract class TimelapseController : Controller
	{
		protected override JsonResult Json(string json)
		{
			return new JsonResult(JsonConvert.SerializeObject(json));
		}
		protected JsonResult Json(object obj)
		{
			return new JsonResult(JsonConvert.SerializeObject(obj));
		}
	}
}
