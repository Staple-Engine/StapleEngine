#include <vector>
#include <memory>
#include <ft2build.h>
#include "common.h"
#include FT_FREETYPE_H
#include FT_GLYPH_H
#include FT_OUTLINE_H
#include FT_BITMAP_H
#include FT_STROKER_H

#ifdef __ANDROID__
#include <android/log.h>
#endif

struct Color
{
	float r, g, b, a;
};

struct Span
{
	Span() : x(0), y(0), width(0), coverage(0) { }
	Span(int x, int y, int width, int coverage) : x(x), y(y), width(width), coverage(coverage) {}

	int x, y, width, coverage;
};

struct Glyph
{
	uint8_t* bitmap;
	uint32_t xOffset;
	uint32_t yOffset;
	uint32_t width;
	uint32_t height;
	uint32_t xAdvance;

	Glyph() : bitmap(nullptr), xOffset(0), yOffset(0), width(0), height(0), xAdvance(0) {}
};

struct FontData
{
	FT_Library library;
	FT_Face face;
	uint8_t* buffer;
};

void RasterCallback(const int32_t y, const int32_t count, const FT_Span* const spans, void* const user)
{
	std::vector<Span>* sptr = (std::vector<Span> *)user;

	for (int32_t i = 0; i < count; i++)
	{
		sptr->push_back(Span(spans[i].x, y, spans[i].len, spans[i].coverage));
	}
}

CEXPORT FontData * FreeTypeLoadFont(const void* ptr, int byteSize)
{
#ifdef __ANDROID__
	__android_log_print(ANDROID_LOG_DEBUG, "StapleEngine", "Loading FreeType Font with ptr %p and byte size %i", ptr, byteSize);
#endif

	FontData* outValue = new FontData();

	if (FT_Init_FreeType(&outValue->library) != 0)
	{
		delete outValue;

		return nullptr;
	}

	outValue->buffer = new uint8_t[byteSize];

	memcpy(outValue->buffer, ptr, byteSize);

	if (FT_New_Memory_Face(outValue->library, (const FT_Byte *)outValue->buffer, byteSize, 0, &outValue->face) != 0)
	{
		FT_Done_FreeType(outValue->library);

		delete outValue;

		return nullptr;
	}

	if (FT_Select_Charmap(outValue->face, FT_ENCODING_UNICODE) != 0)
	{
		FT_Done_Face(outValue->face);
		FT_Done_FreeType(outValue->library);

		delete outValue;

		return nullptr;
	}

	return outValue;
}

CEXPORT void FreeTypeSetSize(FontData* ptr, uint32_t fontSize)
{
	if (ptr == nullptr || ptr->face == nullptr)
	{
		return;
	}

	FT_Set_Pixel_Sizes(ptr->face, 0, fontSize);
}

CEXPORT int FreeTypeLineSpacing(FontData* ptr, uint32_t fontSize)
{
	if (ptr == nullptr || ptr->face == nullptr)
	{
		return 0;
	}

	FreeTypeSetSize(ptr, fontSize);

	return ptr->face->size->metrics.height >> 6;
}

CEXPORT int FreeTypeKerning(FontData *ptr, uint32_t from, uint32_t to, int fontSize)
{
	if (ptr == nullptr || ptr->face == nullptr || !FT_HAS_KERNING(ptr->face))
	{
		return 0;
	}

	FreeTypeSetSize(ptr, fontSize);

	FT_Vector kerning;
	FT_UInt fromIndex = FT_Get_Char_Index(ptr->face, from);
	FT_UInt toIndex = FT_Get_Char_Index(ptr->face, to);

	if (FT_Get_Kerning(ptr->face, fromIndex, toIndex, FT_KERNING_DEFAULT, &kerning) != FT_Err_Ok)
	{
		return 0;
	}

	return kerning.x >> 6;
}

CEXPORT Glyph *FreeTypeLoadGlyph(FontData* ptr, uint32_t character, uint32_t fontSize, Color textColor, Color secondaryTextColor,
	int borderSize, Color borderColor)
{
	if (ptr == nullptr || ptr->face == nullptr)
	{
		return nullptr;
	}

	FreeTypeSetSize(ptr, fontSize);

	FT_Glyph glyphDescriptor;

	if (FT_Load_Char(ptr->face, character, FT_LOAD_TARGET_NORMAL | FT_LOAD_FORCE_AUTOHINT) != FT_Err_Ok)
	{
		return nullptr;
	}

	if (FT_Get_Glyph(ptr->face->glyph, &glyphDescriptor) != 0)
	{
		return nullptr;
	}

	std::vector<Span> spans, outlineSpans;

	if (borderSize > 0 && glyphDescriptor->format == FT_GLYPH_FORMAT_OUTLINE)
	{
		FT_Raster_Params rasterParams;

		memset(&rasterParams, 0, sizeof(rasterParams));

		rasterParams.flags = FT_RASTER_FLAG_AA | FT_RASTER_FLAG_DIRECT;
		rasterParams.gray_spans = RasterCallback;
		rasterParams.user = &spans;

		FT_Outline_Render(ptr->library, &ptr->face->glyph->outline, &rasterParams);

		FT_Stroker stroker;

		FT_Stroker_New(ptr->library, &stroker);
		FT_Stroker_Set(stroker, (int32_t)(borderSize * 64), FT_STROKER_LINECAP_ROUND, FT_STROKER_LINEJOIN_ROUND, 0);

		FT_Glyph_StrokeBorder(&glyphDescriptor, stroker, 0, 1);

		FT_Outline* o = &reinterpret_cast<FT_OutlineGlyph>(glyphDescriptor)->outline;

		rasterParams.user = &outlineSpans;

		FT_Outline_Render(ptr->library, o, &rasterParams);

		FT_Stroker_Done(stroker);
	}

	FT_Glyph_To_Bitmap(&glyphDescriptor, FT_RENDER_MODE_NORMAL, 0, 1);

	FT_BitmapGlyph bitmapGlyph = (FT_BitmapGlyph)glyphDescriptor;
	FT_Bitmap& bitmap = bitmapGlyph->bitmap;

	Glyph* outValue = new Glyph();

	outValue->xAdvance = glyphDescriptor->advance.x >> 16;

	uint32_t width = (uint32_t)bitmap.width;
	uint32_t height = (uint32_t)bitmap.rows;

	if (width <= 0 || height <= 0)
	{
		FT_Done_Glyph(glyphDescriptor);

		return outValue;
	}

	outValue->xOffset = bitmapGlyph->left;
	outValue->yOffset = bitmapGlyph->top;
	outValue->width = width;
	outValue->height = height;

	uint8_t * pixelBuffer = new uint8_t[width * height * 4];

	memset(pixelBuffer, 0, width * height * 4);

	const uint8_t* pixels = bitmap.buffer;

	if (bitmap.pixel_mode == FT_PIXEL_MODE_MONO)
	{
		for (uint32_t y = 0; y < height; y++)
		{
			for (uint32_t x = 0; x < width; x++)
			{
				uint32_t index = (x + y * width) * 4 + 3;

				pixelBuffer[index] = ((pixels[x / 8]) & (1 << (7 - (x % 8)))) ? 255 : 0;
			}

			pixels += bitmap.pitch;
		}
	}
	else
	{
		for (uint32_t y = 0; y < height; y++)
		{
			for (uint32_t x = 0; x < width; x++)
			{
				uint32_t index = (x + y * width) * 4 + 3;

				pixelBuffer[index] = pixels[x];
			}

			pixels += bitmap.pitch;
		}
	}

	Color byteColor = textColor;
	Color secondaryByteColor = secondaryTextColor;

#define NORMALIZE(c) \
	c.r = CLAMP(c.r, 0, 1) * 255; \
	c.g = CLAMP(c.g, 0, 1) * 255; \
	c.b = CLAMP(c.b, 0, 1) * 255; \
	c.a = CLAMP(c.a, 0, 1) * 255;

	NORMALIZE(byteColor);
	NORMALIZE(secondaryByteColor);

	float diffR = (secondaryByteColor.r - byteColor.r);
	float diffG = (secondaryByteColor.g - byteColor.g);
	float diffB = (secondaryByteColor.b - byteColor.b);

	bool sameColor = (textColor.r == secondaryTextColor.r) &&
		(textColor.g == secondaryTextColor.g) &&
		(textColor.b == secondaryTextColor.b) &&
		(textColor.a == secondaryTextColor.a);

	for (uint32_t y = 0; y < height; y++)
	{
		for (uint32_t x = 0; x < width; x++)
		{
			float percent = y / (float)(height - 1);

			uint32_t index = (x + y * width) * 4;

			if (sameColor)
			{
				pixelBuffer[index] = (uint8_t)byteColor.r;
				pixelBuffer[index + 1] = (uint8_t)byteColor.g;
				pixelBuffer[index + 2] = (uint8_t)byteColor.b;
			}
			else
			{
				pixelBuffer[index] = (uint8_t)(byteColor.r + diffR * percent);
				pixelBuffer[index + 1] = (uint8_t)(byteColor.g + diffG * percent);
				pixelBuffer[index + 2] = (uint8_t)(byteColor.b + diffB * percent);
			}
		}
	}

	if (borderSize > 0)
	{
		float minX, minY, maxX, maxY;

		for (uint32_t i = 0; i < spans.size(); i++)
		{
			if (i == 0)
			{
				minX = (float)spans[i].x;
				minY = (float)spans[i].y;
				maxX = (float)spans[i].x;
				maxY = (float)spans[i].y;
			}

			if (spans[i].x < minX)
				minX = (float)spans[i].x;

			if (spans[i].x > maxX)
				maxX = (float)spans[i].x;

			if (spans[i].y < minY)
				minY = (float)spans[i].y;

			if (spans[i].y > maxY)
				maxY = (float)spans[i].y;

			if (spans[i].x + spans[i].width - 1 < minX)
				minX = (float)(spans[i].x + spans[i].width - 1);

			if (spans[i].x + spans[i].width - 1 > maxX)
				maxX = (float)(spans[i].x + spans[i].width - 1);
		}

		for (uint32_t i = 0; i < outlineSpans.size(); i++)
		{
			if (outlineSpans[i].x < minX)
				minX = (float)outlineSpans[i].x;

			if (outlineSpans[i].x > maxX)
				maxX = (float)outlineSpans[i].x;

			if (outlineSpans[i].y < minY)
				minY = (float)outlineSpans[i].y;

			if (outlineSpans[i].y > maxY)
				maxY = (float)outlineSpans[i].y;

			if (outlineSpans[i].x + outlineSpans[i].width - 1 < minX)
				minX = (float)(outlineSpans[i].x + outlineSpans[i].width - 1);

			if (outlineSpans[i].x + outlineSpans[i].width - 1 > maxX)
				maxX = (float)(outlineSpans[i].x + outlineSpans[i].width - 1);
		}

		Color byteBorderColor = borderColor;

		NORMALIZE(byteBorderColor);

		for (uint32_t i = 0; i < outlineSpans.size(); i++)
		{
			for (int32_t w = 0; w < outlineSpans[i].width; w++)
			{
				uint32_t index = (uint32_t)((height - 1 - (outlineSpans[i].y - minY)) * width + (outlineSpans[i].x - minX) + w) * 4;

				pixelBuffer[index] = (uint8_t)byteBorderColor.r;
				pixelBuffer[index + 1] = (uint8_t)byteBorderColor.g;
				pixelBuffer[index + 2] = (uint8_t)byteBorderColor.b;
				pixelBuffer[index + 3] = (uint8_t)std::min(255, outlineSpans[i].coverage);
			}
		}

		for (uint32_t i = 0; i < spans.size(); i++)
		{
			for (int32_t w = 0; w < spans[i].width; w++)
			{
				uint32_t y = (uint32_t)(height - 1 - (spans[i].y - minY));
				float percent = y / (float)(height - 1);

				uint32_t index = (uint32_t)((height - 1 - (spans[i].y - minY)) * width + (spans[i].x - minX) + w) * 4;

				pixelBuffer[index] = (uint8_t)(pixelBuffer[index] + (((byteColor.r + (int32_t)(diffR * percent)) - pixelBuffer[index]) * spans[i].coverage / 255.f));
				pixelBuffer[index + 1] = (uint8_t)(pixelBuffer[index + 1] + (((byteColor.g + (int32_t)(diffG * percent)) - pixelBuffer[index + 1]) * spans[i].coverage / 255.f));
				pixelBuffer[index + 2] = (uint8_t)(pixelBuffer[index + 2] + (((byteColor.b + (int32_t)(diffB * percent)) - pixelBuffer[index + 2]) * spans[i].coverage / 255.f));
				pixelBuffer[index + 3] = (uint8_t)std::min(255, (int32_t)(pixelBuffer[index + 3]) + spans[i].coverage);
			}
		}
	}

	FT_Done_Glyph(glyphDescriptor);

	outValue->bitmap = pixelBuffer;

	return outValue;
}

CEXPORT void FreeTypeFreeGlyph(Glyph* ptr)
{
	if (ptr == nullptr)
	{
		return;
	}

	delete[] ptr->bitmap;

	delete ptr;
}

CEXPORT void FreeTypeFreeFont(FontData* ptr)
{
	if (ptr == nullptr)
	{
		return;
	}

	FT_Done_Face(ptr->face);
	FT_Done_FreeType(ptr->library);

	delete[] ptr->buffer;

	delete ptr;
}
