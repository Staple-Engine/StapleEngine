using System.Numerics;

namespace Staple;

public interface IRenderData
{
    void Clear();

    void Sort(int renderIndex, Vector3 origin, RenderableSortMode sortMode);

    void Submit(int renderIndex);
}
