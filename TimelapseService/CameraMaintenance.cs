using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BPUtil;
using BPUtil.SimpleHttp;
using Timelapse.Configuration;

namespace Timelapse
{
	public static class CameraMaintenance
	{
		public static bool MaintainCamera(CameraSpec cs)
		{
			if (cs == null)
				return false;

			// This maintenance lock ensures that only one request tries to maintain the files at once.  Additional concurrent requests simply skip the maintenance step.
			if (cs.MaintenanceLock.Wait(0))
			{
				try
				{
					// A long-neglected camera will take a long time to maintain.
					// We don't want this request to time out because that could lead to file corruption.
					// So we will try not to spend longer than 25 seconds total, but with a minimum of 10 seconds of actual file copying.

					Stopwatch watchTotal = new Stopwatch();
					watchTotal.Start();

					DirectoryInfo diRoot = new DirectoryInfo(Globals.ApplicationDirectoryBase + cs.path_imgdump);
					if (!diRoot.Exists)
						diRoot.Create();


					FileInfo[] fis = diRoot.GetFiles("*.jpg", SearchOption.TopDirectoryOnly);
					Array.Sort(fis, new FileSystemInfoComparer());


					if (fis.Length > 0)
					{

						Regex rxDateTime = null;
						if (cs.timestampType == TimestampType.Regular_Expression)
							rxDateTime = new Regex(cs.timestamp_regex_input);

						Dictionary<string, byte[]> filesToSave = new Dictionary<string, byte[]>();

						Stopwatch watchCopying = new Stopwatch();
						watchCopying.Start();

						foreach (FileInfo fi in fis)
						{
							if (watchCopying.ElapsedMilliseconds > 10000 && watchTotal.ElapsedMilliseconds > 25000)
								break;
							if (!fi.Name.EndsWith(".jpg"))
								continue;
							// We try to store the images using the camera's local time zone so that
							// the public categorization matches the file system categorization.
							string tempF;
							DateTime fileTimeStamp = GetImageTimestamp(cs, rxDateTime, fi, out tempF);

							string fileKey = Util.GetBundleKeyForTimestamp(fileTimeStamp, tempF);
							string bundleFilePath = Util.GetBundleFilePathForTimestamp(cs, fileTimeStamp);

							int tries = 0;
							int tryLimit = 5;
							while (tries < tryLimit)
							{
								try
								{
									byte[] data = File.ReadAllBytes(fi.FullName);

									filesToSave.Clear();
									filesToSave.Add(fileKey, data);
									FileBundle.FileBundleManager.SaveFiles(bundleFilePath, filesToSave);

									// Delete the original file
									fi.Delete();

									break;
								}
								catch (Exception ex)
								{
									if (++tries < tryLimit)
									{
										Thread.Sleep(1000 * (tries));
										continue;
									}
									Logger.Debug(ex, "Tries: " + tries);
									break;
								}
							}
						}
					}
				}
				finally
				{
					cs.MaintenanceLock.Release();
				}
			}

			return true;
		}

		private static DateTime GetImageTimestamp(CameraSpec cs, Regex rxDateTime, FileInfo fi, out string tempF)
		{
			tempF = "";
			DateTime fileTimeStamp = DateTime.Now;
			if (cs.timestampType == TimestampType.File_Created)
			{
				fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.File_Modified)
			{
				fileTimeStamp = Util.FromUTC(fi.LastWriteTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.DateTime_FromBinary || cs.timestampType == TimestampType.DateTime_FromBinary_With_Temp_F)
			{
				string name = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
				if (cs.timestampType == TimestampType.DateTime_FromBinary_With_Temp_F)
				{
					string[] parts = name.Split(' ');
					if (parts.Length >= 2)
					{
						name = parts[0];
						tempF = parts[1];
					}
				}
				long binaryForm;
				bool success = false;
				if (long.TryParse(name, out binaryForm))
				{
					try
					{
						fileTimeStamp = DateTime.FromBinary(binaryForm); // We are honoring the sender's timestamp without modifying the time zone.
						fileTimeStamp = TimeZoneInfo.ConvertTimeToUtc(fileTimeStamp, TimeZoneInfo.Local);
						fileTimeStamp = Util.FromUTC(fileTimeStamp, cs.timezone);
						success = true;
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
				if (!success)
					fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			else if (cs.timestampType == TimestampType.Regular_Expression)
			{
				Match m = rxDateTime.Match(fi.Name);
				if (m.Success)
				{
					fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);

					try
					{
						int year = cs.timestamp_regex_capture_year == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_year].Value);
						int month = cs.timestamp_regex_capture_month == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_month].Value);
						int day = cs.timestamp_regex_capture_day == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_day].Value);
						int hour = cs.timestamp_regex_capture_hour == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_hour].Value);
						int minute = cs.timestamp_regex_capture_minute == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_minute].Value);
						int second = cs.timestamp_regex_capture_second == -1 ? -1 : int.Parse(m.Groups[cs.timestamp_regex_capture_second].Value);

						fileTimeStamp = new DateTime(year != -1 ? year : fileTimeStamp.Year,
													month != -1 ? month : fileTimeStamp.Month,
													day != -1 ? day : fileTimeStamp.Day,
													hour != -1 ? hour : fileTimeStamp.Hour,
													minute != -1 ? minute : fileTimeStamp.Minute,
													second != -1 ? second : fileTimeStamp.Second,
													DateTimeKind.Unspecified); // We are honoring the sender's timestamp without modifying the time zone.
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
				}
				else
				{
					Logger.Debug("Regular expression match failed for camera '" + cs.id + "' file named '" + fi.Name + "'. Regex string was \"" + rxDateTime.ToString() + "\"");
					fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
				}
			}
			else
			{
				Logger.Debug("Unknown timestamp type set for camera " + cs.id);
				fileTimeStamp = Util.FromUTC(fi.CreationTimeUtc, cs.timezone);
			}
			return fileTimeStamp;
		}
	}
}
