using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace TimelapseCore.FileBundle
{
	internal class BundledFileInfo
	{
		public string Name;
		public long Position;
		public BundledFileInfo(string Name, long Position)
		{
			this.Name = Name;
			this.Position = Position;
		}
	}
	/// <summary>
	/// This class is NOT thread safe.
	/// </summary>
	internal class BundleIndex
	{
		public long Offset;
		private SortedList<string, BundledFileInfo> myIndex = new SortedList<string, BundledFileInfo>();
		private const short magicShortValue = -12345;

		public BundleIndex()
		{
			Offset = 0;
		}
		public BundleIndex(System.IO.FileStream fs)
		{
			if (fs.Length < 8)
				Offset = 0;
			else
			{
				fs.Seek(-8, System.IO.SeekOrigin.End);
				long EndPos = fs.Position;
				Offset = ByteConverter.ToInt64(Util.ReadNBytes(fs, 8), 0);
				fs.Seek(Offset, System.IO.SeekOrigin.Begin);
				short isThisTheMagicValue = ByteConverter.ToInt16(Util.ReadNBytes(fs, 2), 0);
				if (isThisTheMagicValue != magicShortValue)
					throw new Exception("Bundle Index is corrupt.  Index did not start with expected value.  Started with: " + isThisTheMagicValue + ", expected: " + magicShortValue);
				while (fs.Position < EndPos)
				{
					short nameLength = ByteConverter.ToInt16(Util.ReadNBytes(fs, 2), 0);
					if (nameLength < 1)
						throw new Exception("Bundle Index is corrupt. Item name length is less than 1.");
					string name = ByteConverter.ToString(Util.ReadNBytes(fs, nameLength), 0, nameLength);
					long position = ByteConverter.ToInt64(Util.ReadNBytes(fs, 8), 0);
					myIndex.Add(name, new BundledFileInfo(name, position));
				}
			}
		}


		/// <summary>
		/// Writes the index to the file starting at the FileStream's current Position.  The file length will be shortened if necessary to ensure that the end of the index is the end of the file.
		/// </summary>
		/// <param name="fs"></param>
		internal void WriteToStream(System.IO.FileStream fs)
		{
			Offset = fs.Position;
			fs.Write(ByteConverter.GetBytes(magicShortValue), 0, 2);
			foreach (KeyValuePair<string, BundledFileInfo> kvp in myIndex)
			{
				byte[] fileNameBytes = ByteConverter.GetBytes(kvp.Value.Name);

				fs.Write(ByteConverter.GetBytes((short)fileNameBytes.Length), 0, 2);
				fs.Write(fileNameBytes, 0, fileNameBytes.Length);

				fs.Write(ByteConverter.GetBytes(kvp.Value.Position), 0, 8);
			}
			fs.Write(ByteConverter.GetBytes(Offset), 0, 8);
			long position = fs.Position;
			if (position != fs.Length)
				fs.SetLength(position);
			fs.Flush(true);
		}

		/// <summary>
		/// Adds a file to the index.  If a file already exists with this name, an exception is thrown and the index is not modified.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="position"></param>
		internal void AddFile(string name, long position)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Name must not be null or empty");
			if (myIndex.ContainsKey(name))
				throw new Exception("The specified file already exists in the bundle.");
			myIndex.Add(name, new BundledFileInfo(name, position));
		}

		internal BundledFileInfo GetFileInfo(string name)
		{
			BundledFileInfo bfi;
			if (!myIndex.TryGetValue(name, out bfi))
				bfi = null;
			return bfi;
		}

		/// <summary>
		/// Builds a BundleIndex by reading from the beginning of the file, and not reading the stored index.
		/// </summary>
		/// <param name="fs"></param>
		/// <returns></returns>
		internal static BundleIndex Rebuild(System.IO.FileStream fs)
		{
			BundleIndex bi = new BundleIndex();
			fs.Seek(0, System.IO.SeekOrigin.Begin);
			while (fs.Position < fs.Length)
			{
				try
				{
					long position = fs.Position;
					short nameLength = ByteConverter.ToInt16(Util.ReadNBytes(fs, 2), 0);
					if (nameLength < 1)
						return bi; // This is either the magic short value (which is negative) or it is some other data that we weren't expecting.
					string name = ByteConverter.ToString(Util.ReadNBytes(fs, nameLength), 0, nameLength);

					int length = ByteConverter.ToInt32(Util.ReadNBytes(fs, 4), 0);
					if (length < 0 || fs.Length <= fs.Position + length)
						return bi; // File is not long enough to contain what we think it must contain, or length is negative.
					fs.Seek(length, System.IO.SeekOrigin.Current);

					bi.myIndex.Add(name, new BundledFileInfo(name, position));
					bi.Offset = fs.Position;
				}
				catch (Exception ex)
				{
					Logger.Debug(ex);
					return bi;
				}
			}
			return bi;
		}

		internal List<long> GetSortedGoodDataBlocks(HashSet<string> validFileNamesToDelete)
		{
			List<long> positionsToKeep = new List<long>();
			foreach (BundledFileInfo bfi in myIndex.Values)
			{
				if (!validFileNamesToDelete.Contains(bfi.Name))
					positionsToKeep.Add(bfi.Position);
			}
			positionsToKeep.Sort();
			return positionsToKeep;
		}

		internal void Reposition(string name, long newPosition)
		{
			BundledFileInfo bfi = GetFileInfo(name);
			if (bfi != null)
				bfi.Position = newPosition;
		}

		internal List<string> GetFileNames()
		{
			List<string> fileNames = new List<string>();
			foreach (BundledFileInfo bfi in myIndex.Values)
				fileNames.Add(bfi.Name);
			return fileNames;
		}
	}
}
