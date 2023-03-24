namespace Staple
{
    public class MeshRenderer : Renderable
    {
        public Mesh mesh;
        public Material material;

        internal void OnDestroy()
        {
            material?.Destroy();
        }
    }
}