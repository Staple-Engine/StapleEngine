using Staple.Internal;
using System;

namespace Staple.Tooling;

public interface IMeshImporter
{
    bool HandlesExtension(string extension);

    SerializableMeshAsset ImportMesh(MeshImporterContext context);
}
