using Hexa.NET.ImGui;
using System.Numerics;

namespace Staple.Editor;

internal partial class StapleEditor
{
    public static void SetupImGuiStyle()
    {
        // Dark Ruda styleRaikiri from ImThemes
        var style = ImGui.GetStyle();

        style.Alpha = 1.0f;
        style.DisabledAlpha = 0.6000000238418579f;
        style.WindowPadding = new Vector2(8.0f, 8.0f);
        style.WindowRounding = 0.0f;
        style.WindowBorderSize = 1.0f;
        style.WindowMinSize = new Vector2(32.0f, 32.0f);
        style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Left;
        style.ChildRounding = 0.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupRounding = 0.0f;
        style.PopupBorderSize = 1.0f;
        style.FramePadding = new Vector2(4.0f, 3.0f);
        style.FrameRounding = 4.0f;
        style.FrameBorderSize = 0.0f;
        style.ItemSpacing = new Vector2(8.0f, 4.0f);
        style.ItemInnerSpacing = new Vector2(4.0f, 4.0f);
        style.CellPadding = new Vector2(4.0f, 2.0f);
        style.IndentSpacing = 21.0f;
        style.ColumnsMinSpacing = 6.0f;
        style.ScrollbarSize = 14.0f;
        style.ScrollbarRounding = 9.0f;
        style.GrabMinSize = 10.0f;
        style.GrabRounding = 4.0f;
        style.TabRounding = 4.0f;
        style.TabBorderSize = 0.0f;
        style.TabMinWidthForCloseButton = 0.0f;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
        style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

        style.Colors[(int)ImGuiCol.Text] = new Vector4(0.9490196108818054f, 0.95686274766922f, 0.9764705896377563f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.3568627536296844f, 0.4196078479290009f, 0.4666666686534882f, 1.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1098039224743843f, 0.1490196138620377f, 0.168627455830574f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.1490196138620377f, 0.1764705926179886f, 0.2196078449487686f, 1.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.0784313753247261f, 0.0784313753247261f, 0.0784313753247261f, 0.9399999976158142f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.0784313753247261f, 0.09803921729326248f, 0.1176470592617989f, 1.0f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.2000000029802322f, 0.2470588237047195f, 0.2862745225429535f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.1176470592617989f, 0.2000000029802322f, 0.2784313857555389f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.08627451211214066f, 0.1176470592617989f, 0.1372549086809158f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.08627451211214066f, 0.1176470592617989f, 0.1372549086809158f, 0.6499999761581421f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.0784313753247261f, 0.09803921729326248f, 0.1176470592617989f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.0f, 0.0f, 0.0f, 0.5099999904632568f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.1490196138620377f, 0.1764705926179886f, 0.2196078449487686f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.01960784383118153f, 0.01960784383118153f, 0.01960784383118153f, 0.3899999856948853f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.2000000029802322f, 0.2470588237047195f, 0.2862745225429535f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.1764705926179886f, 0.2196078449487686f, 0.2470588237047195f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.08627451211214066f, 0.2078431397676468f, 0.3098039329051971f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.2784313857555389f, 0.5568627715110779f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.2784313857555389f, 0.5568627715110779f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.3686274588108063f, 0.6078431606292725f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.2000000029802322f, 0.2470588237047195f, 0.2862745225429535f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.2784313857555389f, 0.5568627715110779f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.05882352963089943f, 0.529411792755127f, 0.9764705896377563f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.2000000029802322f, 0.2470588237047195f, 0.2862745225429535f, 0.550000011920929f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.800000011920929f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.2000000029802322f, 0.2470588237047195f, 0.2862745225429535f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.09803921729326248f, 0.4000000059604645f, 0.7490196228027344f, 0.7799999713897705f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.09803921729326248f, 0.4000000059604645f, 0.7490196228027344f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.25f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.6700000166893005f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.949999988079071f);
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.1098039224743843f, 0.1490196138620377f, 0.168627455830574f, 1.0f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.800000011920929f);
        style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.2000000029802322f, 0.2470588237047195f, 0.2862745225429535f, 1.0f);
        style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.1098039224743843f, 0.1490196138620377f, 0.168627455830574f, 1.0f);
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.1098039224743843f, 0.1490196138620377f, 0.168627455830574f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.6078431606292725f, 0.6078431606292725f, 0.6078431606292725f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.0f, 0.4274509847164154f, 0.3490196168422699f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.8980392217636108f, 0.6980392336845398f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.0f, 0.6000000238418579f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.3098039329051971f, 0.3098039329051971f, 0.3490196168422699f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.2274509817361832f, 0.2274509817361832f, 0.2470588237047195f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 0.05999999865889549f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 0.3499999940395355f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 0.8999999761581421f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 0.699999988079071f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.2000000029802322f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.3499999940395355f);
    }
}
