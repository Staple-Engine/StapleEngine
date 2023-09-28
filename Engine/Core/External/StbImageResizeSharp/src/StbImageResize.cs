using System.Runtime.InteropServices;

namespace StbImageResizeSharp
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	static unsafe partial class StbImageResize
	{
		public delegate float stbir__kernel_fn(float x, float scale);

		public delegate float stbir__support_fn(float scale);

		public enum stbir__resize_flag
		{
			None,
			AlphaPremultiplied = 1 << 0,
			AlphaUsesColorspace = 1 << 1
		}

		public class stbir__filter_info
		{
			public stbir__kernel_fn kernel;
			public stbir__support_fn support;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct stbir__FP32
		{
			[FieldOffset(0)] public uint u;
			[FieldOffset(0)] public float f;
		}

		public static int stbir_resize_uint8(byte[] input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			byte[] output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels)
		{
			fixed (byte* inputPtr = input_pixels)
			fixed (byte* outputPtr = output_pixels)
			{
				return stbir_resize_uint8(inputPtr, input_w, input_h, input_stride_in_bytes, outputPtr, output_w, output_h, output_stride_in_bytes, num_channels);
			}
		}

		public static int stbir_resize_float(float[] input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			float[] output_pixels, int output_w, int output_h, int output_stride_in_bytes, int num_channels)
		{
			fixed (float* inputPtr = input_pixels)
			fixed (float* outputPtr = output_pixels)
			{
				return stbir_resize_float(inputPtr, input_w, input_h, input_stride_in_bytes, outputPtr, output_w, output_h, output_stride_in_bytes, num_channels);
			}
		}

		public static int stbir_resize_uint8_srgb(byte[] input_pixels, int input_w, int input_h,
			int input_stride_in_bytes, byte[] output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			int num_channels, int alpha_channel, stbir__resize_flag flags)
		{
			fixed (byte* inputPtr = input_pixels)
			fixed (byte* outputPtr = output_pixels)
			{
				return stbir_resize_uint8_srgb(inputPtr, input_w, input_h, input_stride_in_bytes, outputPtr, output_w, output_h, output_stride_in_bytes,
					num_channels, alpha_channel, (int)flags);
			}
		}

		public static int stbir_resize_uint8_srgb_edgemode(byte[] input_pixels, int input_w, int input_h,
			int input_stride_in_bytes, byte[] output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			int num_channels, int alpha_channel, stbir__resize_flag flags, stbir_edge edge_wrap_mode)
		{
			fixed (byte* inputPtr = input_pixels)
			fixed (byte* outputPtr = output_pixels)
			{
				return stbir_resize_uint8_srgb_edgemode(inputPtr, input_w, input_h, input_stride_in_bytes,
					outputPtr, output_w, output_h, output_stride_in_bytes,
					num_channels, alpha_channel, (int)flags, (int)edge_wrap_mode);
			}
		}

		public static int stbir_resize_uint8_generic(byte[] input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			byte[] output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			int num_channels, int alpha_channel, stbir__resize_flag flags, stbir_edge edge_wrap_mode, stbir_filter filter, stbir_colorspace space)
		{
			fixed (byte* inputPtr = input_pixels)
			fixed (byte* outputPtr = output_pixels)
			{
				return stbir_resize_uint8_generic(inputPtr, input_w, input_h, input_stride_in_bytes,
					outputPtr, output_w, output_h, output_stride_in_bytes,
					num_channels, alpha_channel, (int)flags, (int)edge_wrap_mode, (int)filter, (int)space);
			}
		}

		public static int stbir_resize_uint16_generic(ushort[] input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			ushort[] output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			int num_channels, int alpha_channel, stbir__resize_flag flags, stbir_edge edge_wrap_mode, stbir_filter filter, stbir_colorspace space)
		{
			fixed (ushort* inputPtr = input_pixels)
			fixed (ushort* outputPtr = output_pixels)
			{
				return stbir_resize_uint16_generic(inputPtr, input_w, input_h, input_stride_in_bytes,
					outputPtr, output_w, output_h, output_stride_in_bytes,
					num_channels, alpha_channel, (int)flags, (int)edge_wrap_mode, (int)filter, (int)space);
			}
		}

		public static int stbir_resize_float_generic(float[] input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			float[] output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			int num_channels, int alpha_channel, stbir__resize_flag flags, stbir_edge edge_wrap_mode, stbir_filter filter, stbir_colorspace space)
		{
			fixed (float* inputPtr = input_pixels)
			fixed (float* outputPtr = output_pixels)
			{
				return stbir_resize_float_generic(inputPtr, input_w, input_h, input_stride_in_bytes,
					outputPtr, output_w, output_h, output_stride_in_bytes,
					num_channels, alpha_channel, (int)flags, (int)edge_wrap_mode, (int)filter, (int)space);
			}
		}

		public static int stbir_resize(void* input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			void* output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			stbir_datatype datatype, int num_channels, int alpha_channel, stbir__resize_flag flags,
			stbir_edge edge_mode_horizontal, stbir_edge edge_mode_vertical,
			stbir_filter filter_horizontal, stbir_filter filter_vertical, stbir_colorspace space)
		{
			return stbir_resize(input_pixels, input_w, input_h, input_stride_in_bytes,
				output_pixels, output_w, output_h, output_stride_in_bytes,
				(int)datatype, num_channels, alpha_channel, (int)flags,
				(int)edge_mode_horizontal, (int)edge_mode_vertical, (int)filter_horizontal, (int)filter_vertical, (int)space);
		}

		public static int stbir_resize_subpixel(void* input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			void* output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			stbir_datatype datatype, int num_channels, int alpha_channel, stbir__resize_flag flags,
			stbir_edge edge_mode_horizontal, stbir_edge edge_mode_vertical,
			stbir_filter filter_horizontal, stbir_filter filter_vertical, stbir_colorspace space,
			float x_scale, float y_scale, float x_offset, float y_offset)
		{
			return stbir_resize_subpixel(input_pixels, input_w, input_h, input_stride_in_bytes,
				output_pixels, output_w, output_h, output_stride_in_bytes,
				(int)datatype, num_channels, alpha_channel, (int)flags,
				(int)edge_mode_horizontal, (int)edge_mode_vertical, (int)filter_horizontal, (int)filter_vertical, (int)space,
				x_scale, y_scale, x_offset, y_offset);
		}

		public static int stbir_resize_region(void* input_pixels, int input_w, int input_h, int input_stride_in_bytes,
			void* output_pixels, int output_w, int output_h, int output_stride_in_bytes,
			stbir_datatype datatype, int num_channels, int alpha_channel, stbir__resize_flag flags,
			stbir_edge edge_mode_horizontal, stbir_edge edge_mode_vertical,
			stbir_filter filter_horizontal, stbir_filter filter_vertical, stbir_colorspace space,
			float s0, float t0, float s1, float t1)
		{
			return stbir_resize_region(input_pixels, input_w, input_h, input_stride_in_bytes,
				output_pixels, output_w, output_h, output_stride_in_bytes,
				(int)datatype, num_channels, alpha_channel, (int)flags,
				(int)edge_mode_horizontal, (int)edge_mode_vertical, (int)filter_horizontal, (int)filter_vertical, (int)space,
				s0, t0, s1, t1);
		}
	}
}