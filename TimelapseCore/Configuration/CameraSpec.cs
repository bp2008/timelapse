using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

		[EditorCategory("FTP Settings")]
		[EditorCondition_FieldMustBe("type", CameraType.FTP)]
		[EditorName("Image dump root directory")]
		[EditorHint("The relative path from the root of the application to the directory where new images from the camera will appear.")]
		public string path_imgdump = "SetMe";

		public int order = -1;

		protected override string validateFieldValues()
		{
			id = id.ToLower();
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
	public enum PtzType
	{
		None, LoftekCheap, Dahua,
		WanscamCheap, TrendnetIP672,
		IPS_EYE01, TrendnetTVIP400,
		CustomPTZProfile, Dev
	}
}
