using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Threading;

namespace TimelapseCore.Configuration
{
	[EditorName("Camera Configuration")]
	public class CameraSpec : FieldSettable
	{
		[EditorName("Enabled")]
		[EditorHint("Cameras cannot currently be disabled, so this has no meaning.")]
		public bool enabled = false;
		[EditorName("Name")]
		[EditorHint("This is the name users will see.")]
		public string name = "New Timelapse Camera";
		[EditorName("ID")]
		[EditorHint("Must be unique, alphanumeric, no spaces.")]
		public string id = "";
		[EditorName("Camera Type")]
		public CameraType type = CameraType.FTP;

		[EditorCategory("FTP Settings")]
		[EditorCondition_FieldMustBe("type", CameraType.FTP)]
		[EditorName("Image dump root directory")]
		[EditorHint("<br/>The relative path from the root of the application to the directory where new images from the camera will appear.  You are responsible for setting up your own FTP server to ensure images are sent here.")]
		public string path_imgdump = "SetMe";
		[EditorName("Timestamp Type")]
		[EditorHint("<br/>* File_Created: The image timestamp will be derived from the file's Creation date on the local filesystem."
			 + "<br/>* File_Modified: The image timestamp will be derived from the file's Last Modified date on the local filesystem."
			 + "<br/>* Regular_Expression: Use this if you understand \"regular expressions\" well, and you want to preserve the timestamp that was written into the image file name."
			 + "<br/>* DateTime_FromBinary: (Special Case) Use this if the uploaded files were named via: DateTime.ToBinary() + \".jpg\""
			 + "<br/>* DateTime_FromBinary_With_Temp_F: (Special Case) Use this if the uploaded files were named via: DateTime.ToBinary() + \" \" + temperatureF + \".jpg\"")]
		public TimestampType timestampType = TimestampType.File_Created;
		[EditorName("Time Zone ID")]
		[EditorHint("<br/>* Note: This affects new images only.<br/>Modify this field if the server is using the wrong time zone for the image archive links.<br/>CaSe SeNsiTive!<br/><a href=\"../TimeZoneList\" target=\"_blank\">Click here</a> to open (in a new window) a list of Time Zone IDs that you can copy from.")]
		public string timezone = TimeZoneInfo.Local.Id;

		[EditorCategory("Third Party Hosted Settings")]
		[EditorCondition_FieldMustBe("type", CameraType.ThirdPartyHosted)]
		[EditorName("Image path")]
		[EditorHint("<br/>The absolute URL to the live/updating image on a 3rd party server.  The image will not be stored on this server, and therefore the primary purpose of the Third Party Hosted camera type is to allow 3rd party cameras to be added to the <a href=\"../all\">all</a> page.<br/><br/>If you include the text <b>%TIME%</b> it will be replaced by a value derived from the current system time where appropriate to prevent caching of the image.")]
		public string path_3rdpartyimg = "";
		[EditorName("Camera Name Link")]
		[EditorHint("<br/>The URL to load if the camera name is clicked on the all page.")]
		public string path_3rdpartynamelink = "";
		[EditorName("Camera Image Link")]
		[EditorHint("<br/>The URL to load if the camera image is clicked on the all page.")]
		public string path_3rdpartyimglink = "";
		[EditorName("Zipped full-resolution jpeg remote URL")]
		[EditorHint("<br/>This field exists to handle a special case in which the full resolution version of an image is stored as a *.jpg file inside a zip file on the 3rd party server.  If you specify a url here, this server will try to request and unzip the image and send it to you when you load the url '/ID/latest.jpg'.")]
		public string path_3rdpartyimgzippedURL = "";

		[EditorCategory("Regular Expression Timestamp Type Settings")]
		[EditorCondition_FieldMustBe("timestampType", TimestampType.Regular_Expression)]
		[EditorName("Input Regex")]
		[EditorHint("<br/>The regular expression to use to parse the timestamp from the file name.  It should include capture groups for each field of the timestamp.<br/>If any of the Capture Groups are not specified (i.e. -1), the value will be copied from the filesystem Created timestamp.")]
		public string timestamp_regex_input = "^(\\d\\d\\d\\d)(\\d\\d)(\\d\\d)(\\d\\d)(\\d\\d)(\\d\\d)\\.jpg$";
		[EditorName("Capture Group: Year")]
		[EditorHint("The number of the capture group that contains the year.")]
		public int timestamp_regex_capture_year = 1;
		[EditorName("Capture Group: Month")]
		[EditorHint("The number of the capture group that contains the month.")]
		public int timestamp_regex_capture_month = 2;
		[EditorName("Capture Group: Day")]
		[EditorHint("The number of the capture group that contains the day.")]
		public int timestamp_regex_capture_day = 3;
		[EditorName("Capture Group: Hour")]
		[EditorHint("The number of the capture group that contains the hour.")]
		public int timestamp_regex_capture_hour = 4;
		[EditorName("Capture Group: Minute")]
		[EditorHint("The number of the capture group that contains the minute.")]
		public int timestamp_regex_capture_minute = 5;
		[EditorName("Capture Group: Second")]
		[EditorHint("The number of the capture group that contains the second.")]
		public int timestamp_regex_capture_second = 6;

		[EditorCategory("Page Customization (all fields optional)")]
		[EditorCondition_FieldMustBe("type", CameraType.FTP)]
		[EditorName("Camera Name Text Color")]
		[EditorHint("<br/>A css color string (e.g. 'Red' or '#FF0000')")]
		public string cameraNameColor = "#999999";
		[EditorName("Camera Name Background Color")]
		public string cameraNameBackgroundColor = "rgba(0,0,0,0.5)";
		[EditorName("Camera Name Font Size")]
		[EditorHint(" (in points)")]
		public int cameraNameFontSizePts = 20;
		[EditorName("Navigation Menu Font Size")]
		[EditorHint(" (in points)")]
		public int cameraNavigationFontSizePts = 16;
		[EditorName("Camera Name Image Link")]
		[EditorHint("<br/>A relative or absolute URL pointing at the image to use as the logo for this camera.  Replaces the camera name.")]
		public string cameraNameImageUrl = "";
		[EditorName("Top Bar HTML")]
		[EditorHint("<br/>Html content to appear above the image frame, to the right of the image timestamp.")]
		public string topMenuHtml = "";
		[EditorName("Image frame gradient")]
		[EditorHint("Apply a dark gradient background to the image frame.")]
		public bool imgBackgroundGradient = true;
		[EditorName("Background Image Link")]
		[EditorHint("<br/>A relative or absolute URL pointing at the image to use as the background for this camera's main page. Aligned to the top left.")]
		public string backgroundImageUrl = "";
		[EditorName("CSS Body Background Color")]
		public string backgroundColor = "#333333";
		[EditorName("CSS Text Color")]
		public string textColor = "#BBBBBB";
		[EditorName("CSS Text Background Color")]
		public string textBackgroundColor = "rgba(0,0,0,0.5)";
		[EditorName("CSS Link Color")]
		public string linkColor = "#2288DD";
		[EditorName("CSS Link Background Color")]
		public string linkBackgroundColor = "rgba(0,0,0,0.5)";

		[EditorCategory("Page Linking Options")]
		[EditorName("Show on \"All\" page")]
		[EditorHint("If checked, the camera will be shown on the <a href=\"../all\">all</a> page.")]
		public bool showOnAllPage = true;
		[EditorName("\"All\" page overlay message")]
		[EditorHint("<br/>A message that will be overlayed on the camera image on the <a href=\"../all\">all</a> page if the image hasn't updated in longer than 12 hours.  You might enter \"Offline\" here, or another descriptive message.")]
		public string allPageOverlayMessage = "";

		public int order = -1;

		private SemaphoreSlim maintenanceLock = new SemaphoreSlim(1, 1);
		public SemaphoreSlim MaintenanceLock
		{
			get
			{
				return maintenanceLock;
			}
		}

		protected override string validateFieldValues()
		{
			id = id.ToLower();
			if (id == "imgarchive")
				return "0Camera ID cannot be 'imgarchive'";
			if (string.IsNullOrWhiteSpace(name))
				return "0Camera name must not contain only whitespace.";
			if (!Util.IsAlphaNumeric(name, true))
				return "0Camera name must be alphanumeric, but may contain spaces.";
			if (!Util.IsAlphaNumeric(id, false))
				return "0Camera ID must be alphanumeric and not contain any spaces.";
			if (type == CameraType.FTP)
			{
				try
				{
					if (string.IsNullOrWhiteSpace(path_imgdump))
						return "0You must provide an Image dump root directory when using the FTP camera type.";
					string full_path_imgdump = Globals.ApplicationDirectoryBase + path_imgdump;
					FileInfo fi = new FileInfo(full_path_imgdump);
				}
				catch (Exception)
				{
					return "0Invalid Image dump root directory.";
				}
			}
			else if (type == CameraType.ThirdPartyHosted)
			{
				try
				{
					if (string.IsNullOrWhiteSpace(path_3rdpartyimg))
						return "0You must provide a URL for the 3rd party camera image.";
					Uri uri = new Uri(path_3rdpartyimg);
				}
				catch (Exception)
				{
					return "0Invalid URL for the 3rd party camera image.";
				}
			}
			if (timestampType == TimestampType.Regular_Expression)
			{
				if (string.IsNullOrEmpty(timestamp_regex_input))
					return "0You must provide an input regular expression when using the Regular_Expression timestamp type.";
				Regex rxTimestampInput;
				try
				{
					rxTimestampInput = new Regex(timestamp_regex_input);
				}
				catch (Exception) { return "0The timestamp input regular expression is invalid"; }
				string captureGroupsMustBe = "Capture group numbers must be positive, nonzero numbers, or -1 to indicate the capture group is not used.";
				if (timestamp_regex_capture_year < -1 || timestamp_regex_capture_year == 0)
					return "0The capture group for the Year is not valid.  " + captureGroupsMustBe;
				if (timestamp_regex_capture_month < -1 || timestamp_regex_capture_month == 0)
					return "0The capture group for the Month is not valid.  " + captureGroupsMustBe;
				if (timestamp_regex_capture_day < -1 || timestamp_regex_capture_day == 0)
					return "0The capture group for the Day is not valid.  " + captureGroupsMustBe;
				if (timestamp_regex_capture_hour < -1 || timestamp_regex_capture_hour == 0)
					return "0The capture group for the Hour is not valid.  " + captureGroupsMustBe;
				if (timestamp_regex_capture_minute < -1 || timestamp_regex_capture_minute == 0)
					return "0The capture group for the Minute is not valid.  " + captureGroupsMustBe;
				if (timestamp_regex_capture_second < -1 || timestamp_regex_capture_second == 0)
					return "0The capture group for the Second is not valid.  " + captureGroupsMustBe;
				int[] validCaptureGroupNumbers = rxTimestampInput.GetGroupNumbers();
				if (timestamp_regex_capture_year != -1 && !validCaptureGroupNumbers.Contains(timestamp_regex_capture_year))
					return "0The capture group for the Year was not found in the input Regular Expression";
				if (timestamp_regex_capture_month != -1 && !validCaptureGroupNumbers.Contains(timestamp_regex_capture_month))
					return "0The capture group for the Month was not found in the input Regular Expression";
				if (timestamp_regex_capture_day != -1 && !validCaptureGroupNumbers.Contains(timestamp_regex_capture_day))
					return "0The capture group for the Day was not found in the input Regular Expression";
				if (timestamp_regex_capture_hour != -1 && !validCaptureGroupNumbers.Contains(timestamp_regex_capture_hour))
					return "0The capture group for the Hour was not found in the input Regular Expression";
				if (timestamp_regex_capture_minute != -1 && !validCaptureGroupNumbers.Contains(timestamp_regex_capture_minute))
					return "0The capture group for the Minute was not found in the input Regular Expression";
				if (timestamp_regex_capture_second != -1 && !validCaptureGroupNumbers.Contains(timestamp_regex_capture_second))
					return "0The capture group for the Second was not found in the input Regular Expression";
			}
			TimeZoneInfo tzi = Util.GetTimeZoneInfo(timezone, null);
			if (tzi == null)
				return "0Invalid Time Zone ID '" + timezone + "'";
			return "1";
		}

		public override string ToString()
		{
			return id;
		}
	}

	public enum CameraType
	{
		FTP,
		ThirdPartyHosted
	}
	public enum TimestampType
	{
		File_Created,
		File_Modified,
		Regular_Expression,
		DateTime_FromBinary,
		DateTime_FromBinary_With_Temp_F
	}
}
