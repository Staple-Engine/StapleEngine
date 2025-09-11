namespace Staple.Internal;

public class RenderStats
{
    public int drawCalls;
    public int batchedDrawCalls;
    public int culledDrawCalls;
    public int triangleCount;

    public void Clear()
    {
        drawCalls = 0;
        batchedDrawCalls = 0;
        culledDrawCalls = 0;
        triangleCount = 0;
    }
}
