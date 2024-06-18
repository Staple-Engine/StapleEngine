namespace Staple;

/// <summary>
/// Noise generator with a lot of settings, proxy for FastNoiseLib
/// </summary>
public class NoiseGenerator
{
    public enum NoiseType
    {
        OpenSimplex2,
        OpenSimplex2S,
        Cellular,
        Perlin,
        ValueCubic,
        Value
    }

    public enum RotationType3D
    {
        None,
        ImproveXYPlanes,
        ImproveXZPlanes
    };

    public enum FractalType
    {
        None,
        FBm,
        Ridged,
        PingPong,
        DomainWarpProgressive,
        DomainWarpIndependent
    };

    public enum CellularDistanceFunction
    {
        Euclidean,
        EuclideanSq,
        Manhattan,
        Hybrid
    };

    public enum CellularReturnType
    {
        CellValue,
        Distance,
        Distance2,
        Distance2Add,
        Distance2Sub,
        Distance2Mul,
        Distance2Div
    };

    public enum DomainWarpType
    {
        OpenSimplex2,
        OpenSimplex2Reduced,
        BasicGrid
    };

    private FastNoiseLite.FastNoiseLite noise = new();

    private int _seed = 1337;
    private float _frequency = 0.01f;
    private NoiseType _noiseType = NoiseType.OpenSimplex2;
    private RotationType3D _rotationType3D = RotationType3D.None;
    private FractalType _fractalType = FractalType.None;
    private int _fractalOctaves = 3;
    private float _fractalLacunarity = 2;
    private float _fractalGain = 0.5f;
    private float _fractalWeightedStrength = 0;
    private float _fractalPingPongStrength = 2;
    private CellularDistanceFunction _cellularDistanceFunction = CellularDistanceFunction.EuclideanSq;
    private CellularReturnType _cellularReturnType = CellularReturnType.Distance;
    private float _cellularJitter = 1;
    private DomainWarpType _domainWarpType = DomainWarpType.OpenSimplex2;
    private float _domainWarpAmp = 1;

    public int seed
    {
        get => _seed;

        set
        {
            _seed = value;

            noise.SetSeed(value);
        }
    }

    public float frequency
    {
        get => _frequency;

        set
        {
            _frequency = value;

            noise.SetFrequency(value);
        }
    }

    public NoiseType noiseType
    {
        get => _noiseType;

        set
        {
            _noiseType = value;

            noise.SetNoiseType(value switch
            {
                NoiseType.OpenSimplex2 => FastNoiseLite.FastNoiseLite.NoiseType.OpenSimplex2,
                NoiseType.OpenSimplex2S => FastNoiseLite.FastNoiseLite.NoiseType.OpenSimplex2S,
                NoiseType.Cellular => FastNoiseLite.FastNoiseLite.NoiseType.Cellular,
                NoiseType.Perlin => FastNoiseLite.FastNoiseLite.NoiseType.Perlin,
                NoiseType.ValueCubic => FastNoiseLite.FastNoiseLite.NoiseType.ValueCubic,
                NoiseType.Value => FastNoiseLite.FastNoiseLite.NoiseType.Value,
                _ => FastNoiseLite.FastNoiseLite.NoiseType.OpenSimplex2,
            });
        }
    }

    public RotationType3D rotationType3D
    {
        get => _rotationType3D;

        set
        {
            _rotationType3D = value;

            noise.SetRotationType3D(value switch
            {
                RotationType3D.ImproveXZPlanes => FastNoiseLite.FastNoiseLite.RotationType3D.ImproveXZPlanes,
                RotationType3D.ImproveXYPlanes => FastNoiseLite.FastNoiseLite.RotationType3D.ImproveXYPlanes,
                _ => FastNoiseLite.FastNoiseLite.RotationType3D.None,
            });
        }
    }

    public FractalType fractalType
    {
        get => _fractalType;

        set
        {
            _fractalType = value;

            noise.SetFractalType(value switch
            {
                FractalType.FBm => FastNoiseLite.FastNoiseLite.FractalType.FBm,
                FractalType.Ridged => FastNoiseLite.FastNoiseLite.FractalType.Ridged,
                FractalType.PingPong => FastNoiseLite.FastNoiseLite.FractalType.PingPong,
                FractalType.DomainWarpProgressive => FastNoiseLite.FastNoiseLite.FractalType.DomainWarpProgressive,
                FractalType.DomainWarpIndependent => FastNoiseLite.FastNoiseLite.FractalType.DomainWarpIndependent,
                _ => FastNoiseLite.FastNoiseLite.FractalType.None,
            });
        }
    }

    public int fractalOctaves
    {
        get => _fractalOctaves;

        set
        {
            _fractalOctaves = value;

            noise.SetFractalOctaves(value);
        }
    }

    public float fractalLacunarity
    {
        get => _fractalLacunarity;

        set
        {
            _fractalLacunarity = value;

            noise.SetFractalLacunarity(value);
        }
    }

    public float fractalGain
    {
        get => _fractalGain;

        set
        {
            _fractalGain = value;

            noise.SetFractalGain(value);
        }
    }

    public float fractalWeightedStrength
    {
        get => _fractalWeightedStrength;

        set
        {
            _fractalWeightedStrength = value;

            noise.SetFractalWeightedStrength(value);
        }
    }

    public float fractalPingPongStrength
    {
        get => _fractalPingPongStrength;

        set
        {
            _fractalPingPongStrength = value;

            noise.SetFractalPingPongStrength(value);
        }
    }

    public CellularDistanceFunction cellularDistanceFunction
    {
        get => _cellularDistanceFunction;

        set
        {
            _cellularDistanceFunction = value;

            noise.SetCellularDistanceFunction(value switch
            {
                CellularDistanceFunction.Euclidean => FastNoiseLite.FastNoiseLite.CellularDistanceFunction.Euclidean,
                CellularDistanceFunction.EuclideanSq => FastNoiseLite.FastNoiseLite.CellularDistanceFunction.EuclideanSq,
                CellularDistanceFunction.Manhattan => FastNoiseLite.FastNoiseLite.CellularDistanceFunction.Manhattan,
                CellularDistanceFunction.Hybrid => FastNoiseLite.FastNoiseLite.CellularDistanceFunction.Hybrid,
                _ => FastNoiseLite.FastNoiseLite.CellularDistanceFunction.EuclideanSq,
            });
        }
    }

    public CellularReturnType cellularReturnType
    {
        get => _cellularReturnType;

        set
        {
            _cellularReturnType = value;

            noise.SetCellularReturnType(value switch
            {
                CellularReturnType.CellValue => FastNoiseLite.FastNoiseLite.CellularReturnType.CellValue,
                CellularReturnType.Distance => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance,
                CellularReturnType.Distance2 => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance2,
                CellularReturnType.Distance2Add => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance2Add,
                CellularReturnType.Distance2Sub => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance2Sub,
                CellularReturnType.Distance2Mul => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance2Mul,
                CellularReturnType.Distance2Div => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance2Div,
                _ => FastNoiseLite.FastNoiseLite.CellularReturnType.Distance,
            });
        }
    }

    public float cellularJitter
    {
        get => _cellularJitter;

        set
        {
            _cellularJitter = value;

            noise.SetCellularJitter(value);
        }
    }

    public DomainWarpType domainWarpType
    {
        get => _domainWarpType;

        set
        {
            _domainWarpType = value;

            noise.SetDomainWarpType(value switch
            {
                DomainWarpType.BasicGrid => FastNoiseLite.FastNoiseLite.DomainWarpType.BasicGrid,
                DomainWarpType.OpenSimplex2 => FastNoiseLite.FastNoiseLite.DomainWarpType.OpenSimplex2,
                DomainWarpType.OpenSimplex2Reduced => FastNoiseLite.FastNoiseLite.DomainWarpType.OpenSimplex2Reduced,
                _ => FastNoiseLite.FastNoiseLite.DomainWarpType.OpenSimplex2,
            });
        }
    }

    public float domainWarpAmp
    {
        get => _domainWarpAmp;

        set
        {
            _domainWarpAmp = value;

            noise.SetDomainWarpAmp(value);
        }
    }

    /// <summary>
    /// Gets a noise value for two coordinates
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    /// <returns>The noise</returns>
    public float GetNoise(float x, float y) => noise.GetNoise(x, y);

    /// <summary>
    /// Gets a noise value for three coordinates
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    /// <param name="z">The z coordinate</param>
    /// <returns>The noise</returns>
    public float GetNoise(float x, float y, float z) => noise.GetNoise(x, y, z);

    /// <summary>
    /// Warps a 2D position. Should be used in a separate NoiseGenerator since the properties it uses are used for regular noise too.
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    public void DomainWarp(ref float x, ref float y) => noise.DomainWarp(ref x, ref y);

    /// <summary>
    /// Warps a 3D position. Should be used in a separate NoiseGenerator since the properties it uses are used for regular noise too.
    /// </summary>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    /// <param name="z">The z coordinate</param>
    public void DomainWarp(ref float x, ref float y, ref float z) => noise.DomainWarp(ref x, ref y, ref z);
}
