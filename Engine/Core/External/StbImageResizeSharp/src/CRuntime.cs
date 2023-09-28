using System;
using System.Runtime.InteropServices;

namespace StbImageResizeSharp
{
	internal static unsafe class CRuntime
	{
		public static void* malloc(ulong size)
		{
			return malloc((long)size);
		}

		public static void* malloc(long size)
		{
			var ptr = Marshal.AllocHGlobal((int)size);

			MemoryStats.Allocated();

			return ptr.ToPointer();
		}

		public static void memcpy(void* a, void* b, long size)
		{
			var ap = (byte*)a;
			var bp = (byte*)b;
			for (long i = 0; i < size; ++i)
				*ap++ = *bp++;
		}

		public static void memcpy(void* a, void* b, ulong size)
		{
			memcpy(a, b, (long)size);
		}

		public static void free(void* a)
		{
			if (a == null)
				return;

			var ptr = new IntPtr(a);
			Marshal.FreeHGlobal(ptr);
			MemoryStats.Freed();
		}

		public static void memset(void* ptr, int value, long size)
		{
			var bptr = (byte*)ptr;
			var bval = (byte)value;
			for (long i = 0; i < size; ++i)
				*bptr++ = bval;
		}

		public static void memset(void* ptr, int value, ulong size)
		{
			memset(ptr, value, (long)size);
		}

		public static float fabs(double v)
		{
			return (float)Math.Abs(v);
		}

		public static double pow(double a, double b)
		{
			return Math.Pow(a, b);
		}

		public static double ceil(double a)
		{
			return Math.Ceiling(a);
		}

		public static double floor(double a)
		{
			return Math.Floor(a);
		}
	}
}