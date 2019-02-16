using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Timelapse
{
	public class FileSystemInfoComparer : IComparer<System.IO.FileSystemInfo>
	{
		public int Compare(System.IO.FileSystemInfo fsi1, System.IO.FileSystemInfo fsi2)
		{
			return fsi2.Name.CompareTo(fsi1.Name);
		}
	}
}
