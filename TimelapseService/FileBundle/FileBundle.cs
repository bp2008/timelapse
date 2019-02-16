using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Timelapse.FileBundle
{
	/// <summary>
	/// This class IS thread safe.
	/// </summary>
	internal class FileBundle
	{
		internal string Path;
		private const int totalReadLocks = 5;
		private SemaphoreSlim readLock = new SemaphoreSlim(totalReadLocks, totalReadLocks);
		private SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);

		internal FileBundle(string path)
		{
			this.Path = path;
		}

		/// <summary>
		/// Adds the specified files to the bundle.  Files with names that are null or empty will be ignored.  This function will not add files with names that already exist in the bundle.  Returns a list of file names that already existed, and were therefore not written.
		/// </summary>
		/// <param name="files"></param>
		internal IList<string> AddFiles(IDictionary<string, byte[]> files)
		{
			if (!writeLock.Wait(-1))
				throw new Exception("Failed to obtain write lock for FileBundle " + Path);

			int readLocksObtained = 0;
			try
			{
				while(readLocksObtained < totalReadLocks)
				{
					if (!readLock.Wait(-1))
						throw new Exception("Failed to obtain read lock for FileBundle " + Path + ".  Already obtained: " + readLocksObtained);
					readLocksObtained++;
				}
				Util.EnsureDirectoryExistsFromFilePath(Path);
				using (FileStream fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
				{
					BundleIndex index = new BundleIndex(fs);
					fs.Seek(0, SeekOrigin.Begin);

					List<string> alreadyExistingFileNames = new List<string>();

					// Seek to the "end of the file" where new files are to be written.  The current index will be overwritten, and then re-written at the new end of the file.
					long position = index.Offset;
					fs.Seek(position, SeekOrigin.Begin);
					foreach (KeyValuePair<string, byte[]> file in files)
					{
						if (string.IsNullOrEmpty(file.Key))
							continue;

						try
						{
							index.AddFile(file.Key, fs.Position);
						}
						catch (Exception)
						{
							alreadyExistingFileNames.Add(file.Key);
							continue;
						}

						WriteItem(fs, file.Key, file.Value);
					}
					index.WriteToStream(fs);
					return alreadyExistingFileNames;
				}
			}
			finally
			{
				if(readLocksObtained > 0)
					readLock.Release(readLocksObtained);
				writeLock.Release();
			}
		}
		internal IDictionary<string, byte[]> GetFiles(params string[] fileNames)
		{
			if (!readLock.Wait(-1))
				throw new Exception("Failed to obtain read lock for FileBundle " + Path);

			try
			{
				using (FileStream fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
				{
					BundleIndex index = new BundleIndex(fs);
					fs.Seek(0, SeekOrigin.Begin);

					SortedList<string, byte[]> files = new SortedList<string, byte[]>(fileNames.Length);
					foreach (string fileName in fileNames)
					{
						if (fileName == null)
							continue;

						BundledFileInfo bfi = index.GetFileInfo(fileName);
						if (bfi == null)
							continue;
						fs.Seek(bfi.Position, SeekOrigin.Begin);

						string name;
						byte[] data;
						ReadItem(fs, out name, out data);

						if (name != fileName)
							throw new Exception("Bundle is corrupt. Item name at item location (" + name + ") does not match item name from index (" + fileName + ").");

						files.Add(name, data);
					}
					return files;
				}
			}
			finally
			{
				readLock.Release();
			}
		}
		internal void DeleteFiles(params string[] fileNames)
		{
			RepairAndDelete(fileNames);
		}
		internal void Repair()
		{
			RepairAndDelete(null);
		}
		/// <summary>
		/// Iterate through the entire file, rebuild the index, optionally delete some of the contained items, and consolidate the data into a contiguous block.
		/// </summary>
		/// <param name="fileNamesToDelete"></param>
		private void RepairAndDelete(string[] fileNamesToDelete)
		{
			if (fileNamesToDelete == null)
				fileNamesToDelete = new string[0];
			HashSet<string> validFileNamesToDelete = new HashSet<string>();
			foreach (string fileName in fileNamesToDelete)
				if (!string.IsNullOrEmpty(fileName))
					validFileNamesToDelete.Add(fileName);
			
			if (!writeLock.Wait(-1))
				throw new Exception("Failed to obtain write lock for FileBundle " + Path);

			int readLocksObtained = 0;
			try
			{
				while(readLocksObtained < totalReadLocks)
				{
					if (!readLock.Wait(-1))
						throw new Exception("Failed to obtain read lock for FileBundle " + Path + ".  Already obtained: " + readLocksObtained);
					readLocksObtained++;
				}
				using (FileStream fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
				{
					BundleIndex index = BundleIndex.Rebuild(fs);
					fs.Seek(0, SeekOrigin.Begin);
					List<long> positionsToKeep = index.GetSortedGoodDataBlocks(validFileNamesToDelete);
					// Read the data blocks defined by offsetsToKeep and write them back to the file in a single contiguous block.
					long writePosition = 0;
					bool hasMovedAnItem = false;
					foreach (long itemPosition in positionsToKeep)
					{
						if (itemPosition == writePosition)
						{
							// The current item is already in the place we need it to be, so skip past it as quickly as possible.
							if (hasMovedAnItem)
								throw new Exception("Case 1 Logic error.  This code block should not be reachable if we have already moved an item in the bundle repair process.  Unable to continue.");

							if (fs.Position != itemPosition)
								throw new Exception("Case 1 Logic error.  FileStream position is " + fs.Position + " though we expected it to be at " + itemPosition + ".  Unable to continue.");

							// Read the name length and seek past the name
							short nameLength = ByteConverter.ToInt16(Util.ReadNBytes(fs, 2), 0);
							if (nameLength < 1)
								throw new Exception("Case 1 Unexpected name length (" + nameLength + ") encountered when repairing bundle.  Unable to continue.");
							if (fs.Length <= fs.Position + nameLength)
								throw new Exception("Case 1 File is not long enough to contain specified item name of length " + nameLength + ".  Unable to continue.");
							fs.Seek(nameLength, SeekOrigin.Current);

							// Read the item length and seek past the item
							int length = ByteConverter.ToInt32(Util.ReadNBytes(fs, 4), 0);
							if (length < 0)
								throw new Exception("Case 1 Invalid item length (" + length + ") found when repairing bundle.  Unable to continue.");
							if (fs.Length <= fs.Position + length)
								throw new Exception("Case 1 File is not long enough to contain specified item of length " + length + ".  Unable to continue.");
							fs.Seek(length, System.IO.SeekOrigin.Current);

							writePosition = fs.Position;
						}
						else
						{
							// Read the item
							fs.Seek(itemPosition, SeekOrigin.Begin);

							string name;
							byte[] data;
							ReadItem(fs, out name, out data);

							// Write the item
							fs.Seek(writePosition, SeekOrigin.Begin);

							WriteItem(fs, name, data);

							// Modify the index position for this block to match the new location on disk.
							index.Reposition(name, writePosition);

							// Update the stored write position
							writePosition = fs.Position;

							hasMovedAnItem = true;
						}
					}
					index.WriteToStream(fs);
				}
			}
			finally
			{
				if(readLocksObtained > 0)
					readLock.Release(readLocksObtained);
				writeLock.Release();
			}
		}

		internal List<string> GetFileList()
		{
			if (!readLock.Wait(-1))
				throw new Exception("Failed to obtain read lock for FileBundle " + Path);

			try
			{
				using (FileStream fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
				{
					BundleIndex index = new BundleIndex(fs);
					return index.GetFileNames();
				}
			}
			finally
			{
				readLock.Release();
			}
		}

		#region Helpers
		private void ReadItem(FileStream fs, out string name, out byte[] data)
		{
			short nameLength = ByteConverter.ToInt16(Util.ReadNBytes(fs, 2), 0);
			if (nameLength < 1)
				throw new Exception("Unexpected name length (" + nameLength + ") encountered.");
			if (fs.Length <= fs.Position + nameLength)
				throw new Exception("File is not long enough to contain specified item name of length " + nameLength + ".");
			name = ByteConverter.ToString(Util.ReadNBytes(fs, nameLength), 0, nameLength);

			int length = ByteConverter.ToInt32(Util.ReadNBytes(fs, 4), 0);
			if (length < 0)
				throw new Exception("Invalid item length (" + length + ").");
			if (fs.Length <= fs.Position + length)
				throw new Exception("File is not long enough to contain specified item of length " + length + ".");
			data = Util.ReadNBytes(fs, length);
		}

		private void WriteItem(FileStream fs, string name, byte[] data)
		{
			byte[] fileNameBytes = ByteConverter.GetBytes(name);

			fs.Write(ByteConverter.GetBytes((short)fileNameBytes.Length), 0, 2);
			fs.Write(fileNameBytes, 0, fileNameBytes.Length);

			fs.Write(ByteConverter.GetBytes((int)data.Length), 0, 4);
			fs.Write(data, 0, data.Length);
		}
		#endregion
	}
}