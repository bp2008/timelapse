using BPUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timelapse.Configuration;

namespace Timelapse
{
	public static class ExportTest
	{
		//private static int n = 0;
		private static int fileNumber = 0;
		private static HashSet<int> corruptFrames = new HashSet<int>();
		public static void Export1()
		{
			Thread thr = new Thread(() =>
			{
				try
				{
					Directory.CreateDirectory("ExportTest");
					// Create an X264Net instance. Be sure to dispose it when finished, either by calling Dispose() on it, or by creating it in a using block.
					CameraSpec cs = TimelapseWrapper.cfg.GetCameraSpec("buffalo");
					//corruptFrames = new HashSet<int>(new int[] { 852 });
					ExportDate(cs, "2019/07/01");
					ExportDate(cs, "2019/07/02");
					ExportDate(cs, "2019/07/03");
					ExportDate(cs, "2019/07/04");
					ExportDate(cs, "2019/07/05");
					ExportDate(cs, "2019/07/06");
					ExportDate(cs, "2019/07/07");
					ExportDate(cs, "2019/07/08");
					ExportDate(cs, "2019/07/09");
					ExportDate(cs, "2019/07/10");
					ExportDate(cs, "2019/07/11");
					ExportDate(cs, "2019/07/12");
					ExportDate(cs, "2019/07/13");
					ExportDate(cs, "2019/07/14");
					ExportDate(cs, "2019/07/15");
					ExportDate(cs, "2019/07/16");
					ExportDate(cs, "2019/07/17");
					ExportDate(cs, "2019/07/18");
					ExportDate(cs, "2019/07/19");
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
				}
				finally
				{
					Console.WriteLine("Finished exporting images");
				}
			});
			thr.IsBackground = true;
			thr.Name = "ExportTest";
			thr.Start();
			// ffmpeg -r 15 -i "C:\Users\brpea\Source\Repos\timelapse\TimelapseService\bin\Debug\ExportTest\%06d.jpg" -vcodec libx264 -crf 18 -preset medium WeatherTimelapse.mp4
		}

		public static void ExportDate(CameraSpec cs, string date)
		{
			string[] fileListURLs = Navigation.GetFileListUrls(cs, date).Split('\n');
			foreach (string url in fileListURLs)
			{
				//if (corruptFrames.Contains(fileNumber))
				//{
				//	corruptFrames.Remove(fileNumber);
				//	continue;
				//}
				byte[] jpeg_data = WebServerUtil.GetImageData(url);
				if (jpeg_data.Length == 0)
					continue;
				//n++;
				//if (n <= 114)
				//	continue;
				File.WriteAllBytes("ExportTest/" + (fileNumber++).ToString().PadLeft(6, '0') + ".jpg", jpeg_data);
			}
		}
	}
}
