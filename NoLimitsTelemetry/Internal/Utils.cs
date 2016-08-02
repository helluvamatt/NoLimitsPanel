using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Internal
{
	internal static class Utils
	{
		public static uint GetUInt32(byte[] data, int index)
		{
			byte[] slice = new byte[4];
			Array.Copy(data, index, slice, 0, 4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(slice);
			return BitConverter.ToUInt32(slice, 0);
		}

		public static int GetInt32(byte[] data, int index)
		{
			byte[] slice = new byte[4];
			Array.Copy(data, index, slice, 0, 4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(slice);
			return BitConverter.ToInt32(slice, 0);
		}

		public static ushort GetUInt16(byte[] data, int index)
		{
			byte[] slice = new byte[2];
			Array.Copy(data, index, slice, 0, 2);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(slice);
			return BitConverter.ToUInt16(slice, 0);
		}

		public static short GetInt16(byte[] data, int index)
		{
			byte[] slice = new byte[2];
			Array.Copy(data, index, slice, 0, 2);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(slice);
			return BitConverter.ToInt16(slice, 0);
		}

		public static float GetFloat32(byte[] data, int index)
		{
			byte[] slice = new byte[4];
			Array.Copy(data, index, slice, 0, 4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(slice);
			return BitConverter.ToSingle(slice, 0);
		}

		public static double GetFloat64(byte[] data, int index)
		{
			byte[] slice = new byte[8];
			Array.Copy(data, index, slice, 0, 8);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(slice);
			return BitConverter.ToDouble(slice, 0);
		}

		public static string GetUtf8String(byte[] data, int index, int length)
		{
			return Encoding.UTF8.GetString(data, index, length);
		}

		public static byte[] GetBytes(uint value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(int value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(ushort value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(short value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(float value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(double value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return bytes;
		}

		public static byte[] GetBytes(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
	}
}
