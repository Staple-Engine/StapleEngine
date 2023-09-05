using Bgfx;
using ImGuiNET;
using Staple.Internal;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Staple.Editor
{
    internal class ImGuiProxy
    {
        public ushort viewID = 255;
        public IntPtr ImGuiContext;
        public Shader program;
        public Shader imageProgram;
        public VertexLayout layout;
        public bgfx.UniformHandle textureUniform;
        public Texture fontTexture;
        public bgfx.TextureHandle activeTexture;

        public ImFontPtr editorFont;

        private bool frameBegun = false;

        public bool Initialize()
        {
            ImGuiContext = ImGui.CreateContext();

            var ImGuiIO = ImGui.GetIO();

            ImGui.SetCurrentContext(ImGuiContext);

            ImGuiIO.Fonts.AddFontDefault();

            var bytes = Convert.FromBase64String(FontData.IntelOneMonoRegular);

            unsafe
            {
                fixed(byte * ptr = bytes)
                {
                    editorFont = ImGuiIO.Fonts.AddFontFromMemoryTTF((IntPtr)ptr, bytes.Length, 18);
                }
            }

            SetupImGuiKeyMaps(ImGuiIO);

            program = ResourceManager.instance.LoadShader("Shaders/UI/ocornut_imgui.stsh");
            imageProgram = ResourceManager.instance.LoadShader("Shaders/UI/imgui_image.stsh");

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
                ImGuiIO.Fonts.GetTexDataAsRGBA32(out byte* data, out var fontWidth, out var fontHeight);

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

        public void BeginFrame()
        {
            var io = ImGui.GetIO();

            io.DeltaTime = Time.deltaTime;
            io.MouseWheel = Input.MouseDelta.Y;
            io.MouseWheelH = Input.MouseDelta.X;

            io.MouseDown[0] = Input.GetMouseButton(MouseButton.Left);
            io.MouseDown[1] = Input.GetMouseButton(MouseButton.Right);
            io.MouseDown[2] = Input.GetMouseButton(MouseButton.Middle);

            io.MousePos = Input.MousePosition;

            foreach(KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if(key == KeyCode.Unknown)
                {
                    continue;
                }

                io.KeysDown[(int)key] = Input.GetKey(key);
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

                    var cmdList = drawData.CmdListsRange[i];

                    var numVertices = cmdList.VtxBuffer.Size;
                    var numIndices = cmdList.IdxBuffer.Size;

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

                    byte[] data = new byte[size];

                    Marshal.Copy(cmdList.VtxBuffer.Data, data, 0, data.Length);

                    for (var j = 0; j < size; j++)
                    {
                        tvb.data[j] = data[j];
                    }

                    size = numIndices * sizeof(ushort);

                    data = new byte[size];

                    Marshal.Copy(cmdList.IdxBuffer.Data, data, 0, data.Length);

                    for (var j = 0; j < size; j++)
                    {
                        tib.data[j] = data[j];
                    }

                    for (var j = 0; j < cmdList.CmdBuffer.Size; j++)
                    {
                        var drawCmd = cmdList.CmdBuffer[j];

                        if (drawCmd.ElemCount == 0 || drawCmd.UserCallback != IntPtr.Zero)
                        {
                            continue;
                        }

                        var state = (ulong)(bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA) |
                            RenderSystem.BlendFunction(bgfx.StateFlags.BlendSrcAlpha, bgfx.StateFlags.BlendInvSrcAlpha);

                        bgfx.ProgramHandle program = this.program.program;

                        if (drawCmd.TextureId != IntPtr.Zero)
                        {
                            program = imageProgram.program;

                            var index = (ushort)drawCmd.TextureId.ToInt64();

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
            if(texture == null || texture.handle.Valid == false)
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
        public static bool MouseOverArea()
        {
            return ImGui.IsAnyItemActive() || ImGui.IsAnyItemHovered() || ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
        }

        private static void SetupImGuiKeyMaps(ImGuiIOPtr io)
        {
            io.KeyMap[(int)ImGuiKey.Space] = (int)KeyCode.Space;
            io.KeyMap[(int)ImGuiKey.Apostrophe] = (int)KeyCode.Apostrophe;
            io.KeyMap[(int)ImGuiKey.Comma] = (int)KeyCode.Comma;
            io.KeyMap[(int)ImGuiKey.Minus] = (int)KeyCode.Minus;
            io.KeyMap[(int)ImGuiKey.Period] = (int)KeyCode.Period;
            io.KeyMap[(int)ImGuiKey.Slash] = (int)KeyCode.Slash;
            io.KeyMap[(int)ImGuiKey.Semicolon] = (int)KeyCode.SemiColon;
            io.KeyMap[(int)ImGuiKey.Equal] = (int)KeyCode.Equal;
            io.KeyMap[(int)ImGuiKey.A] = (int)KeyCode.A;
            io.KeyMap[(int)ImGuiKey.B] = (int)KeyCode.B;
            io.KeyMap[(int)ImGuiKey.C] = (int)KeyCode.C;
            io.KeyMap[(int)ImGuiKey.D] = (int)KeyCode.D;
            io.KeyMap[(int)ImGuiKey.E] = (int)KeyCode.E;
            io.KeyMap[(int)ImGuiKey.F] = (int)KeyCode.F;
            io.KeyMap[(int)ImGuiKey.G] = (int)KeyCode.G;
            io.KeyMap[(int)ImGuiKey.H] = (int)KeyCode.H;
            io.KeyMap[(int)ImGuiKey.I] = (int)KeyCode.I;
            io.KeyMap[(int)ImGuiKey.J] = (int)KeyCode.J;
            io.KeyMap[(int)ImGuiKey.K] = (int)KeyCode.K;
            io.KeyMap[(int)ImGuiKey.L] = (int)KeyCode.L;
            io.KeyMap[(int)ImGuiKey.M] = (int)KeyCode.M;
            io.KeyMap[(int)ImGuiKey.N] = (int)KeyCode.N;
            io.KeyMap[(int)ImGuiKey.O] = (int)KeyCode.O;
            io.KeyMap[(int)ImGuiKey.P] = (int)KeyCode.P;
            io.KeyMap[(int)ImGuiKey.Q] = (int)KeyCode.Q;
            io.KeyMap[(int)ImGuiKey.R] = (int)KeyCode.R;
            io.KeyMap[(int)ImGuiKey.S] = (int)KeyCode.S;
            io.KeyMap[(int)ImGuiKey.T] = (int)KeyCode.T;
            io.KeyMap[(int)ImGuiKey.U] = (int)KeyCode.U;
            io.KeyMap[(int)ImGuiKey.V] = (int)KeyCode.V;
            io.KeyMap[(int)ImGuiKey.W] = (int)KeyCode.W;
            io.KeyMap[(int)ImGuiKey.X] = (int)KeyCode.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)KeyCode.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)KeyCode.Z;
            io.KeyMap[(int)ImGuiKey.LeftBracket] = (int)KeyCode.LeftBracket;
            io.KeyMap[(int)ImGuiKey.Backslash] = (int)KeyCode.Backslash;
            io.KeyMap[(int)ImGuiKey.RightBracket] = (int)KeyCode.RightBracket;
            io.KeyMap[(int)ImGuiKey.GraveAccent] = (int)KeyCode.GraveAccent;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)KeyCode.Escape;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)KeyCode.Enter;
            io.KeyMap[(int)ImGuiKey.Tab] = (int)KeyCode.Tab;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)KeyCode.Backspace;
            io.KeyMap[(int)ImGuiKey.Insert] = (int)KeyCode.Insert;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)KeyCode.Delete;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)KeyCode.Right;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)KeyCode.Left;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)KeyCode.Down;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)KeyCode.Up;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)KeyCode.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)KeyCode.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)KeyCode.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)KeyCode.End;
            io.KeyMap[(int)ImGuiKey.CapsLock] = (int)KeyCode.CapsLock;
            io.KeyMap[(int)ImGuiKey.ScrollLock] = (int)KeyCode.ScrollLock;
            io.KeyMap[(int)ImGuiKey.NumLock] = (int)KeyCode.NumLock;
            io.KeyMap[(int)ImGuiKey.PrintScreen] = (int)KeyCode.PrintScreen;
            io.KeyMap[(int)ImGuiKey.Pause] = (int)KeyCode.Pause;
            io.KeyMap[(int)ImGuiKey.F1] = (int)KeyCode.F1;
            io.KeyMap[(int)ImGuiKey.F2] = (int)KeyCode.F2;
            io.KeyMap[(int)ImGuiKey.F3] = (int)KeyCode.F3;
            io.KeyMap[(int)ImGuiKey.F4] = (int)KeyCode.F4;
            io.KeyMap[(int)ImGuiKey.F5] = (int)KeyCode.F5;
            io.KeyMap[(int)ImGuiKey.F6] = (int)KeyCode.F6;
            io.KeyMap[(int)ImGuiKey.F7] = (int)KeyCode.F7;
            io.KeyMap[(int)ImGuiKey.F8] = (int)KeyCode.F8;
            io.KeyMap[(int)ImGuiKey.F9] = (int)KeyCode.F9;
            io.KeyMap[(int)ImGuiKey.F10] = (int)KeyCode.F10;
            io.KeyMap[(int)ImGuiKey.F11] = (int)KeyCode.F11;
            io.KeyMap[(int)ImGuiKey.F12] = (int)KeyCode.F12;
            io.KeyMap[(int)ImGuiKey.Keypad0] = (int)KeyCode.Numpad0;
            io.KeyMap[(int)ImGuiKey.Keypad1] = (int)KeyCode.Numpad1;
            io.KeyMap[(int)ImGuiKey.Keypad2] = (int)KeyCode.Numpad2;
            io.KeyMap[(int)ImGuiKey.Keypad3] = (int)KeyCode.Numpad3;
            io.KeyMap[(int)ImGuiKey.Keypad4] = (int)KeyCode.Numpad4;
            io.KeyMap[(int)ImGuiKey.Keypad5] = (int)KeyCode.Numpad5;
            io.KeyMap[(int)ImGuiKey.Keypad6] = (int)KeyCode.Numpad6;
            io.KeyMap[(int)ImGuiKey.Keypad7] = (int)KeyCode.Numpad7;
            io.KeyMap[(int)ImGuiKey.Keypad8] = (int)KeyCode.Numpad8;
            io.KeyMap[(int)ImGuiKey.Keypad9] = (int)KeyCode.Numpad9;
            io.KeyMap[(int)ImGuiKey.KeypadDecimal] = (int)KeyCode.NumpadDecimal;
            io.KeyMap[(int)ImGuiKey.KeypadDivide] = (int)KeyCode.NumpadDivide;
            io.KeyMap[(int)ImGuiKey.KeypadMultiply] = (int)KeyCode.NumpadMultiply;
            io.KeyMap[(int)ImGuiKey.KeypadSubtract] = (int)KeyCode.NumpadSubtract;
            io.KeyMap[(int)ImGuiKey.KeypadAdd] = (int)KeyCode.NumpadAdd;
            io.KeyMap[(int)ImGuiKey.KeypadEnter] = (int)KeyCode.NumpadEnter;
            io.KeyMap[(int)ImGuiKey.KeypadEqual] = (int)KeyCode.NumpadEqual;
            io.KeyMap[(int)ImGuiKey.LeftShift] = (int)KeyCode.LeftShift;
            io.KeyMap[(int)ImGuiKey.LeftCtrl] = (int)KeyCode.LeftControl;
            io.KeyMap[(int)ImGuiKey.LeftAlt] = (int)KeyCode.LeftAlt;
            io.KeyMap[(int)ImGuiKey.LeftSuper] = (int)KeyCode.LeftSuper;
            io.KeyMap[(int)ImGuiKey.RightShift] = (int)KeyCode.RightShift;
            io.KeyMap[(int)ImGuiKey.RightCtrl] = (int)KeyCode.RightControl;
            io.KeyMap[(int)ImGuiKey.RightAlt] = (int)KeyCode.RightAlt;
            io.KeyMap[(int)ImGuiKey.RightSuper] = (int)KeyCode.RightSuper;
            io.KeyMap[(int)ImGuiKey.Menu] = (int)KeyCode.Menu;
        }
    }
}
