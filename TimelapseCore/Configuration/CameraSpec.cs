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
		public bool enabled = false;
		[EditorName("Name")]
		[EditorHint("This is the name users will see.")]
		public string name = "New Timelapse Camera";
		[EditorName("ID")]
		[EditorHint("Must be unique, alphanumeric, no spaces.")]
		public string id = "";
		[EditorName("Camera Type")]
		public CameraType type = CameraType.FTP;
		[EditorName("Timestamp Type")]
		[EditorHint("<br/>* File_Created: The image timestamp will be derived from the file's Creation date on the local filesystem."
			 + "<br/>* File_Modified: The image timestamp will be derived from the file's Last Modified date on the local filesystem."
			 + "<br/>* Regular_Expression: Use this if you understand \"regular expressions\" well, and you want to preserve the timestamp that was written into the image file name."
			 + "<br/>* DateTime_FromBinary: (Special Case) Use this if the uploaded files were named via: DateTime.ToBinary() + \".jpg\"")]
		public TimestampType timestampType = TimestampType.File_Created;
		[EditorName("Time Zone ID")]
		[EditorHint("<br/>* Note: This affects new images only.<br/>Modify this field if the server is using the wrong time zone for the image archive links.<br/>CaSe SeNsiTive!<br/><a href=\"../TimeZoneList\" target=\"_blank\">Click here</a> to open (in a new window) a list of Time Zone IDs that you can copy from.")]
		public string timezone = TimeZoneInfo.Local.Id;

		[EditorCategory("FTP Settings")]
		[EditorCondition_FieldMustBe("type", CameraType.FTP)]
		[EditorName("Image dump root directory")]
		[EditorHint("<br/>The relative path from the root of the application to the directory where new images from the camera will appear.  You are responsible for setting up your own FTP server to ensure images are sent here.")]
		public string path_imgdump = "SetMe";

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
					if (string.IsNullOrEmpty(path_imgdump))
						return "0You must provide an Image dump root directory when using the FTP camera type.";
					string full_path_imgdump = Globals.ApplicationDirectoryBase + path_imgdump;
					FileInfo fi = new FileInfo(full_path_imgdump);
				}
				catch (Exception)
				{
					return "0Invalid Image dump root directory.";
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
		FTP
	}
	public enum TimestampType
	{
		File_Created,
		File_Modified,
		Regular_Expression,
		DateTime_FromBinary
	}
	public enum PtzType
	{
		None, LoftekCheap, Dahua,
		WanscamCheap, TrendnetIP672,
		IPS_EYE01, TrendnetTVIP400,
		CustomPTZProfile, Dev
	}
}
