﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using BPUtil.SimpleHttp;
using Timelapse.Configuration;

namespace Timelapse.Pages.Admin
{
	class Cameras : AdminBase
	{
		protected override string GetPageHtml(HttpProcessor p, Session s)
		{
			ItemTable<CameraSpec> tbl = new ItemTable<CameraSpec>("Cameras", "camera", "id", TimelapseWrapper.cfg.cameras, TimelapseWrapper.cfg, ItemTableMode.Add, new ItemTableColumnDefinition<CameraSpec>[]
			{
				//new ItemTableColumnDefinition<CameraSpec>(" ", c => { return "<a href=\"../image/" + c.id + ".cam\"><img src=\"../image/" + c.id + ".jpg?maxwidth=40&maxheight=40&nocache=" + DateTime.Now.ToBinary().ToString() + "\" alt=\"[img]\" /></a>"; }),
				new ItemTableColumnDefinition<CameraSpec>("Link", c => { return c.type == CameraType.ThirdPartyHosted
					? (c.showOnAllPage ? "<a href=\"../all\">All</a>" : "<span style=\"color:Red\" title=\"This camera is not hosted by the Timelapse application and is not configured to appear on the all page.\">N/A</span>") 
					: ("<a href=\"../../" + c.id + "\">Link</a>"); }),
				new ItemTableColumnDefinition<CameraSpec>("Name", c => { return "<a href=\"javascript:EditItem('" + c.id + "')\">" + HttpUtility.HtmlEncode(c.name) + "</a>"; }),
				new ItemTableColumnDefinition<CameraSpec>("ID", c => { return c.id; }),
				new ItemTableColumnDefinition<CameraSpec>("Enabled", c => { return c.enabled ? ("<span style=\"color:Green;\">Enabled</span>") : "<span style=\"color:Red;\">Disabled</span>"; }),
				new ItemTableColumnDefinition<CameraSpec>("<span title=\"Appears on 'all' page\">All</span>", c => { return c.showOnAllPage ? ("<span style=\"color:Green;\">Yes</span>") : "<span style=\"color:Red;\">No</span>"; }),
				new ItemTableColumnDefinition<CameraSpec>("Type", c => { return c.type.ToString(); }),
				new ItemTableColumnDefinition<CameraSpec>("FTP Directory", c => { return c.type == CameraType.FTP ? c.path_imgdump : "N/A"; }),
				new ItemTableColumnDefinition<CameraSpec>("Order", c => { return "<a href=\"javascript:void(0)\" onclick=\"$.post('reordercam', { dir: 'up', id: '" + c.id + "' }).done(function(){location.href=location.href;});\">Up</a><br/><a href=\"javascript:void(0)\" onclick=\"$.post('reordercam', { dir: 'down', id: '" + c.id + "' }).done(function(){location.href=location.href;});\">Down</a>"; })
			});
			return tbl.GetSectionHtml();
		}
	}
}
