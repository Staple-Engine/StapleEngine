namespace OggVorbisEncoder.Setup.Templates.BookBlocks.Stereo44.Coupled.Chapter9;

public class Page9_1 : IStaticCodeBook
{
    public int Dimensions { get; } = 2;

    public byte[] LengthList { get; } = {
         1, 4, 4, 7, 7, 7, 7, 8, 7, 9, 8, 9, 9,10,10,11,
        11,11,11, 6, 5, 5, 8, 8, 9, 9, 9, 8,10, 9,11,10,
        12,12,13,12,13,13, 5, 5, 5, 8, 8, 9, 9, 9, 9,10,
        10,11,11,12,12,13,12,13,13,17, 8, 8, 9, 9, 9, 9,
         9, 9,10,10,12,11,13,12,13,13,13,13,18, 8, 8, 9,
         9, 9, 9, 9, 9,11,11,12,12,13,13,13,13,13,13,17,
        13,12, 9, 9,10,10,10,10,11,11,12,12,12,13,13,13,
        14,14,18,13,12, 9, 9,10,10,10,10,11,11,12,12,13,
        13,13,14,14,14,17,18,18,10,10,10,10,11,11,11,12,
        12,12,14,13,14,13,13,14,18,18,18,10, 9,10, 9,11,
        11,12,12,12,12,13,13,15,14,14,14,18,18,16,13,14,
        10,11,11,11,12,13,13,13,13,14,13,13,14,14,18,18,
        18,14,12,11, 9,11,10,13,12,13,13,13,14,14,14,13,
        14,18,18,17,18,18,11,12,12,12,13,13,14,13,14,14,
        13,14,14,14,18,18,18,18,17,12,10,12, 9,13,11,13,
        14,14,14,14,14,15,14,18,18,17,17,18,14,15,12,13,
        13,13,14,13,14,14,15,14,15,14,18,17,18,18,18,15,
        15,12,10,14,10,14,14,13,13,14,14,14,14,18,16,18,
        18,18,18,17,14,14,13,14,14,13,13,14,14,14,15,15,
        18,18,18,18,17,17,17,14,14,14,12,14,13,14,14,15,
        14,15,14,18,18,18,18,18,18,18,17,16,13,13,13,14,
        14,14,14,15,16,15,18,18,18,18,18,18,18,17,17,13,
        13,13,13,14,13,14,15,15,15,
    };

    public CodeBookMapType MapType { get; } = (CodeBookMapType)1;
    public int QuantMin { get; } = -518287360;
    public int QuantDelta { get; } = 1622704128;
    public int Quant { get; } = 5;
    public int QuantSequenceP { get; } = 0;

    public int[] QuantList { get; } = {
        9,
        8,
        10,
        7,
        11,
        6,
        12,
        5,
        13,
        4,
        14,
        3,
        15,
        2,
        16,
        1,
        17,
        0,
        18,
    };
}
