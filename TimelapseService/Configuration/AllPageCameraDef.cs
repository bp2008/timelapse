using System;
using BPUtil;
using Newtonsoft.Json;

namespace Timelapse.Configuration
{
	public class AllPageCameraDef
	{
		public string id;
		/// <summary>
		/// This is the name users will see.
		/// </summary>
		public string name;
		/// <summary>
		/// If not null, a frame can be downloaded from this URL.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string imageUrl;
		/// <summary>
		/// If not null, the URL to load if the camera name is clicked on the all page. 
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string nameLink;
		/// <summary>
		/// If not null, the URL to load if the camera image is clicked on the all page.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string imageLink;
		/// <summary>
		/// A message that will be overlayed on the camera image on the "all" page if the image hasn't updated in longer than 12 hours.  You might enter \"Offline\" here, or another descriptive message.
		/// </summary>
		public string allPageOverlayMessage;
		/// <summary>
		/// Age of the latest image, in milliseconds since the Unix Epoch.
		/// </summary>
		public long date;

		public AllPageCameraDef(CameraSpec cs)
		{
			id = cs.id;
			name = cs.name;
			if (cs.type == CameraType.ThirdPartyHosted)
			{
				if (string.IsNullOrWhiteSpace(cs.path_3rdpartyimgzippedURL))
					imageUrl = cs.path_3rdpartyimg.Replace("%TIME%", TimeUtil.GetTimeInMsSinceEpoch().ToString());
				nameLink = cs.path_3rdpartynamelink;
				imageLink = cs.path_3rdpartyimglink;
			}
			allPageOverlayMessage = cs.allPageOverlayMessage;
			DateTime timeObj;
			try
			{
				Navigation.GetLatestImagePath(cs, out string timeHtml, out timeObj);
			}
			catch (NavigationException)
			{
				timeObj = DateTime.Now;
			}
			date = TimeUtil.GetTimeInMsSinceEpoch(timeObj);
		}
	}
}