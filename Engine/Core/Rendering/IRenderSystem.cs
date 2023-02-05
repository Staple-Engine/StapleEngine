using System;

namespace Staple
{
    internal interface IRenderSystem
    {
        Type RelatedComponent();

        void Preprocess(Entity entity, Transform transform, IComponent renderer);

        void Process(Entity entity, Transform transform, IComponent renderer, ushort viewId);

        void Destroy();
    }
}
