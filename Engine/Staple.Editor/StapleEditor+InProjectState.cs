using Hexa.NET.ImGui;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private void HandleInProjectState(ImGuiIOPtr io, ImGuiViewportPtr viewport)
    {
        ImGui.SetNextWindowViewport(viewport.ID);

        Dockspace();
        Viewport(io, viewport);
        Entities(io);
        Inspector(io);
        BottomPanel(io);
    }
}
