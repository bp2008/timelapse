using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace TimelapseCore.FileBundle
{
	public static class FileBundleManager
	{
		private static ConcurrentDictionary<string, FileBundle> bundles = new ConcurrentDictionary<string, FileBundle>();
		private static FileBundle GetFileBundle(string bundlePath)
		{
			return bundles.GetOrAdd(bundlePath.ToLower(), (path) =>
				{
					return new FileBundle(path);
				});
		}
		public static void SaveFiles(string bundlePath, IDictionary<string, byte[]> files)
		{
			GetFileBundle(bundlePath).AddFiles(files);
		}
		public static IDictionary<string, byte[]> GetFiles(string bundlePath, params string[] fileNames)
		{
			return GetFileBundle(bundlePath).GetFiles(fileNames);
		}
		public static void DeleteFiles(string bundlePath, params string[] fileNames)
		{
			GetFileBundle(bundlePath).DeleteFiles(fileNames);
		}

		public static List<string> GetFileList(string bundlePath)
		{
			return GetFileBundle(bundlePath).GetFileList();
		}
	}
}
