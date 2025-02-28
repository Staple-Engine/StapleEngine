using Staple.Internal;
using System;

namespace Staple;

/// <summary>
/// Contains settings for making a noise generator. Check <see cref="NoiseGenerator"/> for details.
/// </summary>
public class NoiseGeneratorSettings : IStapleAsset, IGuidAsset
{
    public int seed = 1337;
    public float frequency = 0.01f;
    public NoiseGenerator.NoiseType noiseType = NoiseGenerator.NoiseType.OpenSimplex2;
    public NoiseGenerator.RotationType3D rotationType3D = NoiseGenerator.RotationType3D.None;
    public NoiseGenerator.FractalType fractalType = NoiseGenerator.FractalType.None;
    public int fractalOctaves = 3;
    public float fractalLacunarity = 2;
    public float fractalGain = 0.5f;
    public float fractalWeightedStrength = 0;
    public float fractalPingPongStrength = 2;
    public NoiseGenerator.CellularDistanceFunction cellularDistanceFunction = NoiseGenerator.CellularDistanceFunction.EuclideanSq;
    public NoiseGenerator.CellularReturnType cellularReturnType = NoiseGenerator.CellularReturnType.Distance;
    public float cellularJitter = 1;
    public NoiseGenerator.DomainWarpType domainWarpType = NoiseGenerator.DomainWarpType.OpenSimplex2;
    public float domainWarpAmp = 1;

    public int GuidHash { get; set; }

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadAsset<NoiseGeneratorSettings>(guid);
    }

    /// <summary>
    /// Creates a noise generator instance
    /// </summary>
    /// <returns>The generator instance</returns>
    public NoiseGenerator MakeGenerator()
    {
        return new()
        {
            cellularDistanceFunction = cellularDistanceFunction,
            cellularJitter = cellularJitter,
            cellularReturnType = cellularReturnType,
            domainWarpType = domainWarpType,
            domainWarpAmp = domainWarpAmp,
            fractalGain = fractalGain,
            fractalLacunarity = fractalLacunarity,
            fractalOctaves = fractalOctaves,
            fractalPingPongStrength = fractalPingPongStrength,
            fractalType = fractalType,
            fractalWeightedStrength = fractalWeightedStrength,
            frequency = frequency,
            noiseType = noiseType,
            rotationType3D = rotationType3D,
            seed = seed,
        };
    }
}
