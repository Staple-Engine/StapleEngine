﻿namespace OggVorbisEncoder.Setup.Templates.FloorBooks;

public class Line2048X27_1Sub1 : IStaticCodeBook
{
    public int Dimensions { get; } = 1;

    public byte[] LengthList { get; } = {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        6, 5, 7, 5, 7, 4, 7, 4, 8, 4, 8, 4, 8, 4, 8, 3,
        8, 4, 9, 4, 9, 4, 9, 4, 9, 4, 9, 5, 9, 5, 9, 6,
        9, 7, 9, 8, 9, 9, 9, 10, 9, 11, 9, 14, 9, 15, 10, 15,
        10, 15, 10, 15, 10, 15, 11, 15, 10, 14, 12, 14, 11, 14, 13, 14,
        13, 15, 15, 15, 12, 15, 15, 15, 13, 15, 13, 15, 13, 15, 15, 15,
        15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 14
    };

    public CodeBookMapType MapType { get; } = CodeBookMapType.None;
    public int QuantMin { get; } = 0;
    public int QuantDelta { get; } = 0;
    public int Quant { get; } = 0;
    public int QuantSequenceP { get; } = 0;
    public int[] QuantList { get; } = null;
}
