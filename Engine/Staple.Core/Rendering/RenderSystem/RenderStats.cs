namespace Staple.Internal;

public class RenderStats
{
    public int drawCalls;
    public int savedDrawCalls;
    public int culledDrawCalls;
    public int triangleCount;

    public void Clear()
    {
        drawCalls = 0;
        savedDrawCalls = 0;
        culledDrawCalls = 0;
        triangleCount = 0;
    }
}
