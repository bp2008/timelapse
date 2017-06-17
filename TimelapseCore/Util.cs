using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using TimelapseCore.Configuration;
using System.Threading;
using BPUtil;

namespace TimelapseCore
{
	public static class Util
	{
		private static int seed = Environment.TickCount;
		private static readonly ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
		private static ConcurrentDictionary<string, TimeZoneInfo> timeZones;
		static Util()
		{
			timeZones = new ConcurrentDictionary<string, TimeZoneInfo>();
			foreach (TimeZoneInfo tzi in TimeZoneInfo.GetSystemTimeZones())
				timeZones[tzi.Id] = tzi;
		}
		public static char GetRandomAlphaNumericChar()
		{
			int i;
			lock (rand.Value)
			{
				i = rand.Value.Next(62);
			}
			if (i < 10)
				return (char)(48 + i);
			if (i < 36)
				return (char)(65 + (i - 10));
			return (char)(97 + (i - 36));
		}
		public static bool ParseBool(string str, bool defaultValueIfUnspecified = false)
		{
			if (string.IsNullOrEmpty(str))
				return defaultValueIfUnspecified;
			if (str == "1")
				return true;
			string strLower = str.ToLower();
			if (strLower == "true" || strLower.StartsWith("y"))
				return true;
			return false;
		}
		public static string ToCookieTime(this DateTime time)
		{
			return time.ToString("dd MMM yyyy hh:mm:ss GMT");
		}
		public static bool EnsureDirectoryExistsFromFilePath(string path)
		{
			FileInfo fi = new FileInfo(path);
			if (fi.Directory.Exists)
				return true;
			else
				return Directory.CreateDirectory(fi.Directory.FullName).Exists;
		}
		public static bool EnsureDirectoryExists(string path)
		{
			if (!Directory.Exists(path))
				return Directory.CreateDirectory(path).Exists;
			return true;
		}
		public static Image ImageFromBytes(byte[] imageData)
		{
			try
			{
				using (MemoryStream ms = new MemoryStream(imageData))
				{
					return Image.FromStream(ms);
				}
			}
			catch (Exception) { }
			return null;
		}
		public static byte[] GetJpegBytes(System.Drawing.Image img, long quality = 80)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				SaveJpeg(ms, img, quality);
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Saves the image into the specified stream using the specified jpeg quality level.
		/// </summary>
		/// <param name="stream">The stream to save the image in.</param>
		/// <param name="img">The image to save.</param>
		/// <param name="quality">(Optional) Jpeg compression quality, from 0 (low quality) to 100 (very high quality).</param>
		public static void SaveJpeg(System.IO.Stream stream, System.Drawing.Image img, long quality = 80)
		{
			if (quality < 0)
				quality = 0;
			if (quality > 100)
				quality = 100;
			EncoderParameters encoderParams = new EncoderParameters(1);
			EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
			ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
			if (jpegCodec == null)
				return;

			encoderParams.Param[0] = qualityParam;
			img.Save(stream, jpegCodec, encoderParams);
		}
		private static ImageCodecInfo GetEncoderInfo(string mimeType)
		{
			ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();

			for (int i = 0; i < encoders.Length; i++)
				if (encoders[i].MimeType == mimeType)
					return encoders[i];
			return null;
		}
		public static bool IsAlphaNumeric(string str, bool spacesAndTabsIncluded = false)
		{
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (spacesAndTabsIncluded && (c == ' ' || c == '\t'))))
					return false;
			}
			return true;
		}

		public static string HttpPost(string URI, string data, string ContentType = "application/x-www-form-urlencoded; charset=utf-8", CookieContainer cookieContainer = null, NetworkCredential credentials = null)
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)System.Net.WebRequest.Create(URI);
				req.Proxy = null;
				if (credentials != null)
					req.Credentials = credentials;
				req.ContentType = ContentType;
				req.Method = "POST";
				req.UserAgent = "Timelapse " + TimelapseGlobals.Version;
				if (cookieContainer == null)
					cookieContainer = new CookieContainer();
				req.CookieContainer = cookieContainer;

				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
				req.ContentLength = bytes.Length;

				using (Stream os = req.GetRequestStream())
				{
					os.Write(bytes, 0, bytes.Length);
				}

				WebResponse resp = req.GetResponse();
				if (resp == null)
					return null;

				using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
				{
					return sr.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				return ex.ToString();
			}
		}

		public static float Clamp(float num, float min, float max)
		{
			if (num < min)
				return min;
			if (num > max)
				return max;
			return num;
		}

		/// <summary>
		/// Throws away any whitespace at the start of the string, then parses the first int it finds and throws away the remainder of the string.
		/// </summary>
		/// <param name="str">The string to parse an int from.</param>
		/// <param name="defaultValue">The value to return if an int isn't found at the start of the trimmed string.</param>
		/// <returns></returns>
		public static int ParseIntRobust(string str, int defaultValue = 0)
		{
			str = str.TrimStart(' ', '\r', '\n', '\t');
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				if ((i == 0 && str[i] == '-') || (str[i] >= '0' && str[i] <= '9'))
					sb.Append(str[i]);
				else
					break;
			}
			int outInt;
			if (int.TryParse(sb.ToString(), out outInt))
				return outInt;
			return defaultValue;
		}
		public static byte[] ReadNBytes(Stream s, int N)
		{
			byte[] buffer = new byte[N];
			int read = 0;
			int justRead = -1;
			while (read < N && justRead != 0)
			{
				justRead = s.Read(buffer, read, N - read);
				read += justRead;
			}
			return buffer;
		}
		/// <summary>
		/// Returns the TimeZoneInfo that matches the specified ID.  If none matches, the defaultValue is returned.
		/// </summary>
		/// <param name="id">The ID string of the TimeZoneInfo you want.</param>
		/// <param name="defaultValue">The value to return in case your TimeZoneInfo cannot be found.</param>
		/// <returns></returns>
		public static TimeZoneInfo GetTimeZoneInfo(string id, TimeZoneInfo defaultValue)
		{
			if (!string.IsNullOrEmpty(id))
			{
				TimeZoneInfo tzi;
				if (timeZones.TryGetValue(id, out tzi))
					return tzi;
			}
			return defaultValue;
		}
		public static DateTime ToUTC(DateTime dt, string timezoneid_ifunspecified)
		{
			if (dt.Kind == DateTimeKind.Local)
				return TimeZoneInfo.ConvertTimeToUtc(dt, TimeZoneInfo.Local);
			else if (dt.Kind == DateTimeKind.Unspecified)
				return TimeZoneInfo.ConvertTimeToUtc(dt, Util.GetTimeZoneInfo(timezoneid_ifunspecified, TimeZoneInfo.Local));
			else
				return dt;
		}
		public static DateTime FromUTC(DateTime dt, string timezoneid_target)
		{
			if (dt.Kind == DateTimeKind.Local)
				return dt;
			else
				return TimeZoneInfo.ConvertTimeFromUtc(dt, Util.GetTimeZoneInfo(timezoneid_target, TimeZoneInfo.Local));
		}

		public static string GetBundleKeyForTimestamp(DateTime t, string tempF)
		{
			return t.Year.ToString().PadLeft(4, '0')
				+ t.Month.ToString().PadLeft(2, '0')
				+ t.Day.ToString().PadLeft(2, '0')
				+ t.Hour.ToString().PadLeft(2, '0')
				+ t.Minute.ToString().PadLeft(2, '0')
				+ t.Second.ToString().PadLeft(2, '0')
				+ (string.IsNullOrWhiteSpace(tempF) ? "" : (" " + tempF));
		}

		public static string GetBundleFilePathForTimestamp(CameraSpec cs, DateTime t)
		{
			return TimelapseGlobals.ImageArchiveDirectoryBase + cs.id + "/" + t.Year.ToString().PadLeft(4, '0') + "/" + t.Month.ToString().PadLeft(2, '0') + "/" + t.Day.ToString().PadLeft(2, '0') + ".bdl";
		}

		public static DateTime GetTimestampFromBundleKey(string fileName, out string tempF)
		{
			int year, month, day, hour, minute, second;
			if (fileName.Length >= 14
				&& int.TryParse(fileName.Substring(0, 4), out year)
				&& int.TryParse(fileName.Substring(4, 2), out month)
				&& int.TryParse(fileName.Substring(6, 2), out day)
				&& int.TryParse(fileName.Substring(8, 2), out hour)
				&& int.TryParse(fileName.Substring(10, 2), out minute)
				&& int.TryParse(fileName.Substring(12, 2), out second))
			{
				if (fileName.Length > 14)
					tempF = fileName.Substring(15);
				else
					tempF = "";
				return new DateTime(year, month, day, hour, minute, second);
			}
			else
			{
				tempF = "";
				return DateTime.MinValue;
			}

		}
		public static string GetDisplayableTime(DateTime t, bool includeSeconds)
		{
			return t.ToString(includeSeconds ? "T" : "t");
		}
		public static string Colorize(string tempf, string degrees = "&deg;")
		{
			try
			{
				float dataF = float.Parse(tempf);
				string color;
				if (dataF <= 0)
					color = "Blue";
				else if (dataF <= 32)
					color = "#0050A0";
				else if (dataF <= 55)
					color = "#006850";
				else if (dataF <= 75)
					color = "Green";
				else if (dataF <= 90)
					color = "#884400";
				else
					color = "Red";
				return "<span style=\"color: " + color + ";\">" + tempf + degrees + "</span>";
			}
			catch (Exception) { }
			return tempf + degrees;
		}
	}
	public static class DateTimeJavaScript
	{
		public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly long EpochTicks = UnixEpoch.Ticks;
		private static readonly long TicksPerMillisecond = TimeSpan.TicksPerSecond / 1000;

		public static long ToJavaScriptMilliseconds(this DateTime dt)
		{
			return (long)((dt.ToUniversalTime().Ticks - EpochTicks) / TicksPerMillisecond);
		}
	}
}
