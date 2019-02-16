using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using BPUtil;

namespace Timelapse.Pages
{
	public static class TimeZoneList
	{
		public static string GetHtml()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("<html><head><title>Supported Time Zone List</title>");
			sb.Append("<script type=\"text/javascript\" src=\"" + Globals.jQueryPath + "\"></script>");
			sb.Append("<script type=\"text/javascript\" src=\"Scripts/TableSorter.js\"></script>");
			sb.Append("<link href=\"../Styles/TableSorter_Green.css\" rel=\"stylesheet\" type=\"text/css\">");
			sb.Append(@"<script type=""text/javascript"">
		$(document).ready(function ()
		{
			$('#tztable').tablesorter({ widgets: ['zebra'] });
		}); 
	</script>");
			sb.Append("</head>");
			sb.Append("<body><table id=\"tztable\" class=\"tablesorter\"><thead><tr><th>Zone ID</th><th>Also Known As</th><th>Also Known As</th><th>Supports Daylight Savings</th><th>UTC Offset</th></tr></thead><tbody>");
			foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
			{
				sb.Append("<tr>");
				sb.Append("<td>");
				sb.Append(HttpUtility.HtmlEncode(tz.Id));
				sb.Append("</td>");
				sb.Append("<td>");
				sb.Append(HttpUtility.HtmlEncode(tz.DaylightName));
				sb.Append("</td>");
				sb.Append("<td>");
				sb.Append(HttpUtility.HtmlEncode(tz.DisplayName));
				sb.Append("</td>");
				sb.Append("<td>");
				sb.Append(tz.SupportsDaylightSavingTime ? "Yes" : "No");
				sb.Append("</td>");
				sb.Append("<td>");
				sb.Append(tz.BaseUtcOffset.TotalHours);
				sb.Append(" hours</td>");
				sb.Append("</tr>");
			}
			sb.Append("</tbody></table></body></html>");
			return sb.ToString();
		}
	}
}