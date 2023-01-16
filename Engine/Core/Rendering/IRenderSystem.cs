using System;

namespace Staple
{
    internal interface IRenderSystem
    {
        Type RelatedComponent();

        void Preprocess(Entity entity, Transform transform, Component renderer);

        void Process(Entity entity, Transform transform, Component renderer, ushort viewId);

        void Destroy();
    }
}
