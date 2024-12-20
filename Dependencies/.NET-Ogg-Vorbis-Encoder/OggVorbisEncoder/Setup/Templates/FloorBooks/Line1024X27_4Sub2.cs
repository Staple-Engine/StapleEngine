﻿namespace OggVorbisEncoder.Setup.Templates.FloorBooks;

public class Line1024X27_4Sub2 : IStaticCodeBook
{
    public int Dimensions { get; } = 1;

    public byte[] LengthList { get; } = {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 4, 2, 4, 2, 5, 3, 5, 4, 6, 6, 6, 7, 7, 8,
        7, 8, 7, 8, 7, 9, 8, 9, 8, 9, 8, 10, 8, 11, 9, 12,
        9, 12
    };

    public CodeBookMapType MapType { get; } = CodeBookMapType.None;
    public int QuantMin { get; } = 0;
    public int QuantDelta { get; } = 0;
    public int Quant { get; } = 0;
    public int QuantSequenceP { get; } = 0;
    public int[] QuantList { get; } = null;
}
