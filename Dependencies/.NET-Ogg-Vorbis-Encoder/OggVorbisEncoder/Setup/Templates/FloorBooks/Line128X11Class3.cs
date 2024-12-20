﻿namespace OggVorbisEncoder.Setup.Templates.FloorBooks;

public class Line128X11Class3 : IStaticCodeBook
{
    public int Dimensions { get; } = 1;

    public byte[] LengthList { get; } = {
        7, 6, 9, 17, 7, 6, 8, 17, 12, 9, 11, 16, 16, 16, 16, 16,
        5, 4, 7, 16, 5, 3, 6, 14, 9, 6, 8, 15, 16, 16, 16, 16,
        5, 4, 6, 13, 3, 2, 4, 11, 7, 4, 6, 13, 16, 11, 10, 14,
        12, 12, 12, 16, 9, 7, 10, 15, 12, 9, 11, 16, 16, 15, 15, 16
    };

    public CodeBookMapType MapType { get; } = CodeBookMapType.None;
    public int QuantMin { get; } = 0;
    public int QuantDelta { get; } = 0;
    public int Quant { get; } = 0;
    public int QuantSequenceP { get; } = 0;
    public int[] QuantList { get; } = null;
}
