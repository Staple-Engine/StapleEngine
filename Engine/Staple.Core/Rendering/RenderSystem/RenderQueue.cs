using System.Collections.Generic;

namespace Staple.Internal;

internal class RenderQueue
{
    public readonly List<RenderQueueItem> items = [];
    public RenderState state;
    public StapleShaderUniform[] vertexUniformData;
    public StapleShaderUniform[] fragmentUniformData;
}
