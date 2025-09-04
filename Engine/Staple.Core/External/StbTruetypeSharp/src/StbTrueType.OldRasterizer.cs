using Hebron.Runtime;
using System.Runtime.InteropServices;

namespace StbTrueTypeSharp
{
	unsafe partial class StbTrueType
	{
		public static stbtt__active_edge_old_rasterizer* stbtt__new_active_old_rasterizer(stbtt__hheap* hh, stbtt__edge* e, int off_x, float start_point, void* userdata)
		{
			stbtt__active_edge_old_rasterizer* z = (stbtt__active_edge_old_rasterizer*)(stbtt__hheap_alloc(hh, (ulong)(sizeof(stbtt__active_edge_old_rasterizer)), userdata));
			float dxdy = (e->x1 - e->x0) / (e->y1 - e->y0);
			if (z == null)
				return z;
			if ((dxdy) < (0))
				z->dx = (int)(-((int)(CRuntime.floor((double)((1 << 10) * -dxdy)))));
			else
				z->dx = ((int)(CRuntime.floor((double)((1 << 10) * dxdy))));

			z->x = ((int)(CRuntime.floor((double)((1 << 10) * e->x0 + z->dx * (start_point - e->y0)))));
			z->x -= (int)(off_x * (1 << 10));
			z->ey = (float)(e->y1);
			z->next = null;
			z->direction = (int)((e->invert) != 0 ? 1 : -1);
			return z;
		}

		public static void stbtt__fill_active_edges_old_rasterizer(byte* scanline, int len, stbtt__active_edge_old_rasterizer* e, int max_weight)
		{
			int x0 = 0; 
			int w = 0;
			while ((e) != null)
			{
				if ((w) == (0))
				{
					x0 = (int)(e->x);
					w += (int)(e->direction);
				}
				else
				{
					int x1 = e->x;
					w += (int)(e->direction);
					if ((w) == (0))
					{
						int i = x0 >> 10;
						int j = x1 >> 10;
						if (((i) < (len)) && ((j) >= (0)))
						{
							if ((i) == (j))
							{
								scanline[i] = (byte)(scanline[i] + (byte)((x1 - x0) * max_weight >> 10));
							}
							else
							{
								if ((i) >= (0))
									scanline[i] = (byte)(scanline[i] + (byte)((((1 << 10) - (x0 & ((1 << 10) - 1))) * max_weight) >> 10));
								else
									i = (int)(-1);
								if ((j) < (len))
									scanline[j] = (byte)(scanline[j] + (byte)(((x1 & ((1 << 10) - 1)) * max_weight) >> 10));
								else
									j = (int)(len);
								for (++i; (i) < (j); ++i)
								{
									scanline[i] = (byte)(scanline[i] + (byte)(max_weight));
								}
							}
						}
					}
				}

				e = e->next;
			}
		}

		public static void stbtt__rasterize_sorted_edges_old_rasterizer(stbtt__bitmap* result, stbtt__edge* e, int n, int vsubsample, int off_x, int off_y, void* userdata)
		{
			usedOldRasterizer = true;

			var hh = new stbtt__hheap();
			stbtt__active_edge_old_rasterizer* active = null;
			int y = 0; int j = 0;
			int max_weight = (255 / vsubsample);
			int s = 0;
			byte* scanline_data = stackalloc byte[512];
			byte* scanline;

			if ((result->w) > (512))
				scanline = (byte*)(CRuntime.malloc((ulong)(result->w)));
			else
				scanline = scanline_data;
			y = (int)(off_y * vsubsample);
			e[n].y0 = (float)((off_y + result->h) * (float)(vsubsample) + 1);
			while ((j) < (result->h))
			{
				CRuntime.memset(scanline, (int)(0), (ulong)(result->w));
				for (s = (int)(0); (s) < (vsubsample); ++s)
				{
					float scan_y = y + 0.5f;
					stbtt__active_edge_old_rasterizer** step = &active;
					while ((*step) != null)
					{
						stbtt__active_edge_old_rasterizer* z = *step;
						if ((z->ey) <= (scan_y))
						{
							*step = z->next;
							z->direction = (int)(0);
							stbtt__hheap_free(&hh, z);
						}
						else
						{
							z->x += (int)(z->dx);
							step = &((*step)->next);
						}
					}

					for (; ; )
					{
						int changed = 0;
						step = &active;
						while (((*step) != null) && (((*step)->next) != null))
						{
							if (((*step)->x) > ((*step)->next->x))
							{
								stbtt__active_edge_old_rasterizer* t = *step;
								stbtt__active_edge_old_rasterizer* q = t->next;
								t->next = q->next;
								q->next = t;
								*step = q;
								changed = (int)(1);
							}

							step = &(*step)->next;
						}

						if (changed == 0)
							break;
					}

					while ((e->y0) <= (scan_y))
					{
						if ((e->y1) > (scan_y))
						{
							stbtt__active_edge_old_rasterizer* z = stbtt__new_active_old_rasterizer(&hh, e, (int)(off_x), (float)(scan_y), userdata);
							if (z != null)
							{
								if ((active) == (null))
									active = z;
								else if ((z->x) < (active->x))
								{
									z->next = active;
									active = z;
								}
								else
								{
									stbtt__active_edge_old_rasterizer* p = active;
									while (((p->next) != null) && ((p->next->x) < (z->x)))
									{
										p = p->next;
									}

									z->next = p->next;
									p->next = z;
								}
							}
						}

						++e;
					}

					if ((active) != null)
						stbtt__fill_active_edges_old_rasterizer(scanline, (int)(result->w), active, (int)(max_weight));
					++y;
				}

				CRuntime.memcpy(result->pixels + j * result->stride, scanline, (ulong)(result->w));
				++j;
			}

			stbtt__hheap_cleanup(&hh, userdata);
			if (scanline != scanline_data)
				CRuntime.free(scanline);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbtt__active_edge_old_rasterizer
		{
			public stbtt__active_edge_old_rasterizer* next;
			public int x;
			public int dx;
			public float ey;
			public int direction;
		}
	}
}