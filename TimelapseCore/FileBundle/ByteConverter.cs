using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimelapseCore.FileBundle
{
	public static class ByteConverter
	{
		public static byte[] GetBytes(short data)
		{
			return new byte[]
                {
                    (byte) ((data >> 8) & 0xff),
                    (byte) ((data >> 0) & 0xff),
                };
		}

		public static byte[] GetBytes(int data)
		{
			return new byte[]
                {
                    (byte) ((data >> 24) & 0xff),
                    (byte) ((data >> 16) & 0xff),
                    (byte) ((data >> 8) & 0xff),
                    (byte) ((data >> 0) & 0xff),
                };
		}

		public static byte[] GetBytes(long data)
		{
			return new byte[]
                {
                    (byte) ((data >> 56) & 0xff),
                    (byte) ((data >> 48) & 0xff),
                    (byte) ((data >> 40) & 0xff),
                    (byte) ((data >> 32) & 0xff),
                    (byte) ((data >> 24) & 0xff),
                    (byte) ((data >> 16) & 0xff),
                    (byte) ((data >> 8) & 0xff),
                    (byte) ((data >> 0) & 0xff),
                };
		}

		public static byte[] GetBytes(bool data)
		{
			return new byte[]
                {
                    (byte) (data ? 0x01 : 0x00)
                }; // bool -> {1 byte}
		}

		public static byte[] GetBytes(char data)
		{
			return new byte[]
                {
                    (byte) ((data >> 8) & 0xff),
                    (byte) ((data >> 0) & 0xff),
                };
		}

		public static byte[] GetBytes(char[] data)
		{
			if (data == null)
				return null;
			// ----------
			byte[] byts = new byte[data.Length * 2];
			for (int i = 0; i < data.Length; i++)
				Array.Copy(GetBytes(data[i]), 0, byts, i * 2, 2);
			return byts;
		}

		public static byte[] GetBytes(string data)
		{
			try
			{
				return Encoding.UTF8.GetBytes(data);
			}
			catch (Exception)
			{
				return new byte[0];
			}
		}

		public static short ToInt16(byte[] b, int startIndex)
		{
			if (b == null || b.Length - startIndex < 2)
				return 0;
			// ----------
			return (short)((0xff & b[startIndex]) << 8
					| (0xff & b[startIndex + 1]) << 0);
		}

		public static int ToInt32(byte[] data, int startIndex)
		{
			if (data == null || data.Length - startIndex < 4)
				return 0;
			// ----------
			return (int)( // NOTE: type cast not necessary for int
					(0xff & data[startIndex]) << 24
					| (0xff & data[startIndex + 1]) << 16
					| (0xff & data[startIndex + 2]) << 8
					| (0xff & data[startIndex + 3]) << 0);
		}

		public static long ToInt64(byte[] b, int startIndex)
		{
			if (b == null || b.Length - startIndex < 8)
				return 0;
			// ----------
			return (long)( // (Below) convert to longs before shift because digits
				//         are lost with ints beyond the 32-bit limit
					(long)(0xff & b[startIndex]) << 56
					| (long)(0xff & b[startIndex + 1]) << 48
					| (long)(0xff & b[startIndex + 2]) << 40
					| (long)(0xff & b[startIndex + 3]) << 32
					| (long)(0xff & b[startIndex + 4]) << 24
					| (long)(0xff & b[startIndex + 5]) << 16
					| (long)(0xff & b[startIndex + 6]) << 8
					| (long)(0xff & b[startIndex + 7]) << 0);
		}

		public static bool ToBoolean(byte[] data, int startIndex)
		{
			return (data == null || data.Length - startIndex < 1) ? false : data[startIndex] != 0x00;
		}

		public static char ToChar(byte[] data, int startIndex)
		{
			if (data == null || data.Length - startIndex < 2)
				return (char)0;
			// ----------
			return (char)((0xff & data[0]) << 8
					| (0xff & data[1]) << 0);
		}

		/**
		 * Two bytes == One char
		 * @param data
		 * @param startIndex
		 * @param bytesToConvert
		 * @return
		 */
		public static char[] ToCharArray(byte[] data, int startIndex, int bytesToConvert)
		{
			if (data == null || bytesToConvert % 2 != 0 || (data.Length - startIndex) < bytesToConvert)
				return null;
			// ----------
			char[] chrs = new char[bytesToConvert / 2];
			for (int i = 0; i < data.Length; i++)
			{
				chrs[i] = ToChar(new byte[]
                    {
                        data[startIndex],
                        data[startIndex + 1],
                    }, 0);
				startIndex += 2;
			}
			return chrs;
		}

		public static string ToString(byte[] message, int startIndex, int bytesToConvert)
		{
			try
			{
				return Encoding.UTF8.GetString(message, startIndex, bytesToConvert);
			}
			catch (Exception)
			{
				return "";
			}
		}

		private static string ByteArrayDump(byte[] message)
		{
			if (message == null)
				return "null";
			else if (message.Length == 0)
				return "empty";
			return string.Join(",", message);
		}
	}
}
