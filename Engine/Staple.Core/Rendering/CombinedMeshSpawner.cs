namespace Staple;

public class CombinedMeshSpawner : LifecycleComponent
{
    public CombinedMeshAsset asset;
    public Transform targetContainer;

    public override void Start()
    {
        base.Start();

        if(asset == null ||
            !(targetContainer?.Entity.IsValid ?? false) ||
            MeshUtilities.CombinedMeshData.FromAsset(asset) is not MeshUtilities.CombinedMeshData meshData)
        {
            return;
        }

        MeshUtilities.InstanceCombinedMesh(meshData, targetContainer);

        Entity.Destroy();
    }
}
