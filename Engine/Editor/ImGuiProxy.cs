using Bgfx;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;
using Staple.Internal;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Editor;

internal class ImGuiProxy
{
    public ushort viewID = 255;
    public ImGuiContextPtr ImGuiContext;
    public ImNodesContextPtr ImNodesContext;
    public ImPlotContextPtr ImPlotContext;
    public Shader program;
    public Shader imageProgram;
    public VertexLayout layout;
    public bgfx.UniformHandle textureUniform;
    public Texture fontTexture;
    public bgfx.TextureHandle activeTexture;

    public ImFontPtr editorFont;

    private bool frameBegun = false;

    private readonly KeyCode[] keyCodes = Enum.GetValues<KeyCode>();

    public bool Initialize()
    {
        ImGuiContext = ImGui.CreateContext();

        var io = ImGui.GetIO();

        ImGui.SetCurrentContext(ImGuiContext);

        ImGuizmo.SetImGuiContext(ImGuiContext);

        ImPlot.SetImGuiContext(ImGuiContext);
        ImNodes.SetImGuiContext(ImGuiContext);

        ImNodesContext = ImNodes.CreateContext();
        ImNodes.SetCurrentContext(ImNodesContext);
        ImNodes.StyleColorsDark(ImNodes.GetStyle());

        ImPlotContext = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(ImPlotContext);
        ImPlot.StyleColorsDark(ImPlot.GetStyle());

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        io.Fonts.AddFontDefault();

        var bytes = Convert.FromBase64String(FontData.IntelOneMonoRegular);

        unsafe
        {
            fixed(byte * ptr = bytes)
            {
                editorFont = io.Fonts.AddFontFromMemoryTTF(ptr, bytes.Length, 18);
            }
        }

        program = ResourceManager.instance.LoadShader("Hidden/Shaders/UI/ocornut_imgui.stsh");
        imageProgram = ResourceManager.instance.LoadShader("Hidden/Shaders/UI/imgui_image.stsh");

        if(program == null || imageProgram == null)
        {
            Log.Error("Failed to load imgui shaders");

            return false;
        }

        layout = new VertexLayoutBuilder()
            .Add(bgfx.Attrib.Position, 2, bgfx.AttribType.Float)
            .Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float)
            .Add(bgfx.Attrib.Color0, 4, bgfx.AttribType.Uint8, true)
            .Build();

        textureUniform = bgfx.create_uniform("s_tex", bgfx.UniformType.Sampler, 1);

        unsafe
        {
            byte *data = null;
            int fontWidth = 0;
            int fontHeight = 0;

            io.Fonts.GetTexDataAsRGBA32(&data, ref fontWidth, ref fontHeight);

            byte[] fontData = new byte[fontWidth * fontHeight * 4];

            for(int i = 0; i < fontData.Length; i++)
            {
                fontData[i] = data[i];
            }

            fontTexture = Texture.CreatePixels("FONT", fontData, (ushort)fontWidth, (ushort)fontHeight, new TextureMetadata()
            {
                useMipmaps = false,
            }, bgfx.TextureFormat.BGRA8);
        }

        if(fontTexture == null)
        {
            Log.Error("Failed to load font");

            return false;
        }

        return true;
    }

    public void Destroy()
    {
        if(textureUniform.Valid)
        {
            bgfx.destroy_uniform(textureUniform);
        }

        program?.Destroy();
        imageProgram?.Destroy();
        fontTexture?.Destroy();
    }

    public ImGuiKey GetKey(KeyCode key)
    {
        return key switch
        {
            KeyCode.Space => ImGuiKey.Space,
            KeyCode.Apostrophe => ImGuiKey.Apostrophe,
            KeyCode.Comma => ImGuiKey.Comma,
            KeyCode.Minus => ImGuiKey.Minus,
            KeyCode.Period => ImGuiKey.Period,
            KeyCode.Slash => ImGuiKey.Slash,
            KeyCode.SemiColon => ImGuiKey.Semicolon,
            KeyCode.Equal => ImGuiKey.Equal,
            KeyCode.A => ImGuiKey.A,
            KeyCode.B => ImGuiKey.B,
            KeyCode.C => ImGuiKey.C,
            KeyCode.D => ImGuiKey.D,
            KeyCode.E => ImGuiKey.E,
            KeyCode.F => ImGuiKey.F,
            KeyCode.G => ImGuiKey.G,
            KeyCode.H => ImGuiKey.H,
            KeyCode.I => ImGuiKey.I,
            KeyCode.J => ImGuiKey.J,
            KeyCode.K => ImGuiKey.K,
            KeyCode.L => ImGuiKey.L,
            KeyCode.M => ImGuiKey.M,
            KeyCode.N => ImGuiKey.N,
            KeyCode.O => ImGuiKey.O,
            KeyCode.P => ImGuiKey.P,
            KeyCode.Q => ImGuiKey.Q,
            KeyCode.R => ImGuiKey.R,
            KeyCode.S => ImGuiKey.S,
            KeyCode.T => ImGuiKey.T,
            KeyCode.U => ImGuiKey.U,
            KeyCode.V => ImGuiKey.V,
            KeyCode.W => ImGuiKey.W,
            KeyCode.X => ImGuiKey.X,
            KeyCode.Y => ImGuiKey.Y,
            KeyCode.Z => ImGuiKey.Z,
            KeyCode.LeftBracket => ImGuiKey.LeftBracket,
            KeyCode.Backslash => ImGuiKey.Backslash,
            KeyCode.RightBracket => ImGuiKey.RightBracket,
            KeyCode.GraveAccent => ImGuiKey.GraveAccent,
            KeyCode.Escape => ImGuiKey.Escape,
            KeyCode.Enter => ImGuiKey.Enter,
            KeyCode.Tab => ImGuiKey.Tab,
            KeyCode.Backspace => ImGuiKey.Backspace,
            KeyCode.Insert => ImGuiKey.Insert,
            KeyCode.Delete => ImGuiKey.Delete,
            KeyCode.Right => ImGuiKey.RightArrow,
            KeyCode.Left => ImGuiKey.LeftArrow,
            KeyCode.Down => ImGuiKey.DownArrow,
            KeyCode.Up => ImGuiKey.UpArrow,
            KeyCode.PageUp => ImGuiKey.PageUp,
            KeyCode.PageDown => ImGuiKey.PageDown,
            KeyCode.Home => ImGuiKey.Home,
            KeyCode.End => ImGuiKey.End,
            KeyCode.CapsLock => ImGuiKey.CapsLock,
            KeyCode.ScrollLock => ImGuiKey.ScrollLock,
            KeyCode.NumLock => ImGuiKey.NumLock,
            KeyCode.PrintScreen => ImGuiKey.PrintScreen,
            KeyCode.Pause => ImGuiKey.Pause,
            KeyCode.F1 => ImGuiKey.F1,
            KeyCode.F2 => ImGuiKey.F2,
            KeyCode.F3 => ImGuiKey.F3,
            KeyCode.F4 => ImGuiKey.F4,
            KeyCode.F5 => ImGuiKey.F5,
            KeyCode.F6 => ImGuiKey.F6,
            KeyCode.F7 => ImGuiKey.F7,
            KeyCode.F8 => ImGuiKey.F8,
            KeyCode.F9 => ImGuiKey.F9,
            KeyCode.F10 => ImGuiKey.F10,
            KeyCode.F11 => ImGuiKey.F11,
            KeyCode.F12 => ImGuiKey.F12,
            KeyCode.Numpad0 => ImGuiKey.Keypad0,
            KeyCode.Numpad1 => ImGuiKey.Keypad1,
            KeyCode.Numpad2 => ImGuiKey.Keypad2,
            KeyCode.Numpad3 => ImGuiKey.Keypad3,
            KeyCode.Numpad4 => ImGuiKey.Keypad4,
            KeyCode.Numpad5 => ImGuiKey.Keypad5,
            KeyCode.Numpad6 => ImGuiKey.Keypad6,
            KeyCode.Numpad7 => ImGuiKey.Keypad7,
            KeyCode.Numpad8 => ImGuiKey.Keypad8,
            KeyCode.Numpad9 => ImGuiKey.Keypad9,
            KeyCode.NumpadDecimal => ImGuiKey.KeypadDecimal,
            KeyCode.NumpadDivide => ImGuiKey.KeypadDivide,
            KeyCode.NumpadMultiply => ImGuiKey.KeypadMultiply,
            KeyCode.NumpadSubtract => ImGuiKey.KeypadSubtract,
            KeyCode.NumpadAdd => ImGuiKey.KeypadAdd,
            KeyCode.NumpadEnter => ImGuiKey.KeypadEnter,
            KeyCode.NumpadEqual => ImGuiKey.KeypadEqual,
            KeyCode.LeftShift => ImGuiKey.LeftShift,
            KeyCode.LeftControl => ImGuiKey.LeftCtrl,
            KeyCode.LeftAlt => ImGuiKey.LeftAlt,
            KeyCode.LeftSuper => ImGuiKey.LeftSuper,
            KeyCode.RightShift => ImGuiKey.RightShift,
            KeyCode.RightControl => ImGuiKey.RightCtrl,
            KeyCode.RightAlt => ImGuiKey.RightAlt,
            KeyCode.RightSuper => ImGuiKey.RightSuper,
            KeyCode.Menu => ImGuiKey.Menu,
            _ => ImGuiKey.None,
        };
    }

    public void BeginFrame()
    {
        var io = ImGui.GetIO();

        io.DeltaTime = Time.unscaledDeltaTime;
        io.MouseWheel = Input.MouseDelta.Y;
        io.MouseWheelH = Input.MouseDelta.X;

        io.MouseDown[0] = Input.GetMouseButton(MouseButton.Left);
        io.MouseDown[1] = Input.GetMouseButton(MouseButton.Right);
        io.MouseDown[2] = Input.GetMouseButton(MouseButton.Middle);

        io.MousePos = Input.MousePosition;

        foreach(var key in keyCodes)
        {
            if(key == KeyCode.Unknown)
            {
                continue;
            }

            var nativeKey = GetKey(key);

            if(nativeKey == (ImGuiKey)(-1))
            {
                continue;
            }

            io.AddKeyEvent(nativeKey, Input.GetKey(key));
        }

        io.KeyCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        io.KeyAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        io.KeyShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        io.KeySuper = Input.GetKey(KeyCode.LeftSuper) || Input.GetKey(KeyCode.RightSuper);

        if(Input.Character != 0)
        {
            io.AddInputCharacter(Input.Character);
        }

        ImGui.NewFrame();

        ImGui.PushFont(editorFont);

        ImGuizmo.BeginFrame();

        frameBegun = true;
    }

    public void EndFrame()
    {
        if (frameBegun)
        {
            frameBegun = false;

            ImGui.PopFont();

            ImGui.Render();

            Render(ImGui.GetDrawData());
        }
    }

    private void Render(ImDrawDataPtr drawData)
    {
        unsafe
        {
            var fbWidth = drawData.DisplaySize.X * drawData.FramebufferScale.X;
            var fbHeight = drawData.DisplaySize.Y * drawData.FramebufferScale.Y;

            if (fbWidth < 0 || fbHeight < 0)
            {
                return;
            }

            bgfx.set_view_name(viewID, "ImGui");
            bgfx.set_view_mode(viewID, bgfx.ViewMode.Sequential);

            var ortho = Matrix4x4.CreateOrthographicOffCenter(drawData.DisplayPos.X, drawData.DisplayPos.X + drawData.DisplaySize.X,
                drawData.DisplayPos.Y + drawData.DisplaySize.Y, drawData.DisplayPos.Y, 0, 1000);

            bgfx.set_view_transform(viewID, null, &ortho);
            bgfx.set_view_rect(viewID, 0, 0, (ushort)drawData.DisplaySize.X, (ushort)drawData.DisplaySize.Y);

            var clipPos = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                bgfx.TransientVertexBuffer tvb;
                bgfx.TransientIndexBuffer tib;

                var cmdList = drawData.CmdLists.Data[i];

                var numVertices = cmdList->VtxBuffer.Size;
                var numIndices = cmdList->IdxBuffer.Size;

                if (RenderSystem.CheckAvailableTransientBuffers((uint)numVertices, layout.layout, (uint)numIndices) == false)
                {
                    break;
                }

                fixed(bgfx.VertexLayout *layout = &this.layout.layout)
                {
                    bgfx.alloc_transient_vertex_buffer(&tvb, (uint)numVertices, layout);
                }

                bgfx.alloc_transient_index_buffer(&tib, (uint)numIndices, false);

                var size = numVertices * sizeof(ImDrawVert);

                Buffer.MemoryCopy((void *)cmdList->VtxBuffer.Data, tvb.data, size, size);

                size = numIndices * sizeof(ushort);

                Buffer.MemoryCopy((void *)cmdList->IdxBuffer.Data, tib.data, size, size);

                for (var j = 0; j < cmdList->CmdBuffer.Size; j++)
                {
                    var drawCmd = cmdList->CmdBuffer.Data[j];

                    if (drawCmd.ElemCount == 0 || drawCmd.UserCallback != null)
                    {
                        continue;
                    }

                    var state = (ulong)(bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA) |
                        RenderSystem.BlendFunction(bgfx.StateFlags.BlendSrcAlpha, bgfx.StateFlags.BlendInvSrcAlpha);

                    bgfx.ProgramHandle program = this.program.instances.First().Value.program;

                    if (drawCmd.TextureId != IntPtr.Zero)
                    {
                        program = imageProgram.instances.First().Value.program;

                        var index = (ushort)drawCmd.TextureId.Handle;

                        activeTexture.idx = index;
                    }
                    else
                    {
                        activeTexture.idx = fontTexture.handle.idx;
                    }

                    var clipRect = new Vector4((drawCmd.ClipRect.X - clipPos.X) * clipScale.X,
                        (drawCmd.ClipRect.Y - clipPos.Y) * clipScale.Y,
                        (drawCmd.ClipRect.Z - clipPos.X) * clipScale.X,
                        (drawCmd.ClipRect.W - clipPos.Y) * clipScale.Y);

                    if (clipRect.X < fbWidth && clipRect.Y < fbHeight &&
                        clipRect.Z >= 0 && clipRect.W >= 0)
                    {
                        var x = (ushort)Math.Max(clipRect.X, 0);
                        var y = (ushort)Math.Max(clipRect.Y, 0);

                        bgfx.set_texture(0, textureUniform, activeTexture, uint.MaxValue);

                        bgfx.set_scissor(x, y,
                            (ushort)(Math.Min(clipRect.Z, 65535.0f) - x),
                            (ushort)(Math.Min(clipRect.W, 65535.0f) - y));

                        bgfx.set_state(state, 0);

                        bgfx.set_transient_vertex_buffer(0, &tvb, drawCmd.VtxOffset, (uint)numVertices);
                        bgfx.set_transient_index_buffer(&tib, drawCmd.IdxOffset, drawCmd.ElemCount);

                        bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr GetImGuiTexture(Texture texture)
    {
        if(texture == null || texture.handle.Valid == false || texture.Disposed)
        {
            return IntPtr.Zero;
        }

        return new IntPtr(texture.handle.idx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ImGuiRGBA(byte r, byte g, byte b, byte a = 255)
    {
        return r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ImGuiRGBA(Color32 color)
    {
        return ImGuiRGBA(color.r, color.g, color.b, color.a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MouseOverArea()
    {
        return ImGui.IsAnyItemActive() || ImGui.IsAnyItemHovered() || ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
    }
}
