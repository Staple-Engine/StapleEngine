using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Editor;

internal class ImGuiProxy
{
    public ImGuiContextPtr ImGuiContext;
    public ImNodesContextPtr ImNodesContext;
    public ImPlotContextPtr ImPlotContext;
    public Shader program;
    public VertexLayout layout;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public ImDrawVert[] vertices = [];
    public ushort[] indices = [];
    public readonly Dictionary<int, (Texture, byte[], int)> textures = [];
    public readonly Dictionary<int, Texture> registeredTextures = [];
    public readonly Dictionary<Texture, int> registeredTexturesInverse = [];
    private int textureCounter = 1;

    private readonly Texture[] emptyTexture = [];
    private readonly Texture[] singleTexture = new Texture[1];

    public ImFontPtr editorFont;

    public ImFontPtr headerFont;

    private byte[] editor;
    private byte[] header;
    private bool frameBegun = false;
    private bool destroyed = false;

    private readonly KeyCode[] keyCodes = Enum.GetValues<KeyCode>();

    public static readonly ImGuiProxy instance = new();

    private static GCHandle pinnedClipboardHandle;

    private static unsafe void *GetClipboardText(void *context)
    {
        if(pinnedClipboardHandle.IsAllocated)
        {
            pinnedClipboardHandle.Free();
        }

        var clipboard = Encoding.UTF8.GetBytes(Platform.ClipboardText ?? "");

        pinnedClipboardHandle = GCHandle.Alloc(clipboard, GCHandleType.Pinned);

        return (void *)pinnedClipboardHandle.AddrOfPinnedObject();
    }

    private static unsafe void SetClipboardText(void* context, byte *text)
    {
        var counter = 0;

        byte* i = text;

        while(*i != 0)
        {
            counter++;
            i++;
        }

        var textSpan = new Span<byte>(text, counter);

        var str = Encoding.UTF8.GetString(textSpan);

        Platform.SetClipboardText(str);
    }

    public bool Initialize()
    {
        ImGuiContext = ImGui.CreateContext();

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

        var io = ImGui.GetIO();
        var platformIO = ImGui.GetPlatformIO();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset |
            ImGuiBackendFlags.RendererHasTextures;

        io.ConfigWindowsMoveFromTitleBarOnly = true;

        unsafe
        {
            var setPtr = Marshal.GetFunctionPointerForDelegate(SetClipboardText);
            var getPtr = Marshal.GetFunctionPointerForDelegate(GetClipboardText);

            platformIO.RendererTextureMaxWidth = platformIO.RendererTextureMaxHeight = 8192;
            platformIO.PlatformSetClipboardTextFn = (void*)setPtr;
            platformIO.PlatformGetClipboardTextFn = (void*)getPtr;
            platformIO.PlatformClipboardUserData = (void *)nint.Zero;
        }

        editor = Convert.FromBase64String(FontData.IntelOneMonoMedium);
        header = Convert.FromBase64String(FontData.IntelOneMonoBold);

        unsafe
        {
            var editorPtr = new Span<byte>(editor);
            var headerPtr = new Span<byte>(header);

            fixed(byte *ptr = editorPtr)
            {
                editorFont = io.Fonts.AddFontFromMemoryTTF(ptr, editor.Length, 20);
            }

            fixed(byte *ptr = headerPtr)
            {
                headerFont = io.Fonts.AddFontFromMemoryTTF(ptr, header.Length, 22);
            }

            io.FontDefault = editorFont;
        }

        var programPath = $"Hidden/Shaders/UI/imgui.{AssetSerialization.ShaderExtension}";

        program = ResourceManager.instance.LoadShader(programPath);

        ResourceManager.instance.LockAsset(programPath);

        if (program == null)
        {
            Log.Error("Failed to load imgui shaders");

            return false;
        }

        layout = VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float2)
            .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float2)
            .Add(VertexAttribute.Color0, VertexAttributeType.UInt)
            .Build();

        return true;
    }

    public void Destroy()
    {
        if(destroyed)
        {
            return;
        }

        destroyed = true;

        program?.Destroy();

        foreach(var texture in textures)
        {
            texture.Value.Item1.Destroy();
        }

        textures.Clear();

        if(pinnedClipboardHandle.IsAllocated)
        {
            pinnedClipboardHandle.Free();
        }
    }

    public static ImGuiKey GetKey(KeyCode key)
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
        if(destroyed)
        {
            return;
        }

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

        io.AddKeyEvent(ImGuiKey.ModCtrl, io.KeyCtrl);
        io.AddKeyEvent(ImGuiKey.ModAlt, io.KeyAlt);
        io.AddKeyEvent(ImGuiKey.ModShift, io.KeyShift);
        io.AddKeyEvent(ImGuiKey.ModSuper, io.KeySuper);

        if(Input.Character != 0)
        {
            io.AddInputCharacter(Input.Character);
        }

        ImGui.NewFrame();

        ImGuizmo.BeginFrame();

        frameBegun = true;
    }

    public void EndFrame()
    {
        if (destroyed)
        {
            return;
        }

        if (frameBegun)
        {
            frameBegun = false;

            ImGui.Render();

            Render(ImGui.GetDrawData());
        }
    }

    private void Render(ImDrawDataPtr drawData)
    {
        if (destroyed)
        {
            return;
        }

        unsafe
        {
            var fbWidth = drawData.DisplaySize.X * drawData.FramebufferScale.X;
            var fbHeight = drawData.DisplaySize.Y * drawData.FramebufferScale.Y;

            if (fbWidth < 0 || fbHeight < 0)
            {
                return;
            }

            if(drawData.Textures.Size != 0)
            {
                var t = new Span<ImTextureDataPtr>(drawData.Textures.Data, drawData.Textures.Size);

                foreach(var texture in t)
                {
                    switch(texture.Status)
                    {
                        case ImTextureStatus.WantCreate:

                            {
                                var bytesPerPixel = texture.BytesPerPixel;

                                var pixels = new byte[texture.Width * texture.Height * bytesPerPixel];

                                fixed (void* ptr = pixels)
                                {
                                    Buffer.MemoryCopy(texture.Pixels, ptr, pixels.Length, pixels.Length);
                                }

                                var outTexture = Texture.CreatePixels($"ImGui {texture.UniqueID}", pixels,
                                    (ushort)texture.Width, (ushort)texture.Height,
                                    new TextureMetadata()
                                    {
                                        useMipmaps = false,
                                    }, texture.Format switch
                                    {
                                        ImTextureFormat.Rgba32 => TextureFormat.RGBA8,
                                        ImTextureFormat.Alpha8 => TextureFormat.A8,
                                        _ => TextureFormat.RGBA8,
                                    });

                                if (outTexture != null)
                                {
                                    ResourceManager.instance.LockAsset($"ImGui {texture.UniqueID}");

                                    textures.Add(textureCounter, (outTexture, pixels, bytesPerPixel));

                                    texture.SetTexID(new ImTextureID(textureCounter));

                                    textureCounter++;
                                }
                                else
                                {
                                    texture.SetTexID(ImTextureID.Null);
                                }

                                texture.SetStatus(ImTextureStatus.Ok);
                            }

                            break;

                        case ImTextureStatus.WantUpdates:

                            {
                                var index = (int)texture.TexID.Handle;

                                if (textures.TryGetValue(index, out var item))
                                {
                                    var (target, pixels, bytesPerPixel) = item;

                                    var updates = texture.Updates;

                                    fixed(byte *ptr = pixels)
                                    {
                                        for (var i = 0; i < updates.Size; i++)
                                        {
                                            var update = updates.Data[i];

                                            for(var y = 0; y < update.H; y++)
                                            {
                                                Buffer.MemoryCopy(texture.GetPixelsAt(update.X, update.Y + y),
                                                    ptr + update.X * bytesPerPixel + ((update.Y + y) * texture.Width * bytesPerPixel),
                                                    update.W * bytesPerPixel, update.W * bytesPerPixel);
                                            }
                                        }
                                    }

                                    target?.Destroy();

                                    target = Texture.CreatePixels($"ImGui {texture.UniqueID}", pixels,
                                        (ushort)texture.Width, (ushort)texture.Height,
                                        new TextureMetadata()
                                        {
                                            useMipmaps = false,
                                        }, texture.Format switch
                                        {
                                            ImTextureFormat.Rgba32 => TextureFormat.RGBA8,
                                            ImTextureFormat.Alpha8 => TextureFormat.A8,
                                            _ => TextureFormat.RGBA8,
                                        });

                                    textures[index] = (target, pixels, bytesPerPixel);

                                    if (target != null)
                                    {
                                        texture.SetTexID(new ImTextureID(index));
                                    }
                                    else
                                    {
                                        texture.SetTexID(ImTextureID.Null);
                                    }

                                    texture.SetStatus(ImTextureStatus.Ok);
                                }
                            }

                            break;

                        case ImTextureStatus.WantDestroy:

                            if(texture.UnusedFrames > 0)
                            {
                                var index = (int)texture.TexID.Handle;

                                if (textures.TryGetValue(index, out var item))
                                {
                                    item.Item1?.Destroy();

                                    textures.Remove(index);

                                    break;
                                }

                                texture.SetStatus(ImTextureStatus.Destroyed);
                            }

                            break;
                    }
                }
            }

            var rect = new RectFloat(drawData.DisplayPos, drawData.DisplaySize);

            var ortho = Matrix4x4.CreateOrthographicOffCenter(rect.Position.X, rect.Size.X, rect.Size.Y, rect.Position.Y, -1, 1);

            var clipPos = drawData.DisplayPos;
            var clipScale = drawData.FramebufferScale;

            if (vertices.Length < drawData.TotalVtxCount)
            {
                Array.Resize(ref vertices, drawData.TotalVtxCount);
            }

            if (indices.Length < drawData.TotalIdxCount)
            {
                Array.Resize(ref indices, drawData.TotalIdxCount);
            }

            var currentVertex = 0;
            var currentIndex = 0;

            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                var cmdList = drawData.CmdLists.Data[i];

                var vertexData = new Span<ImDrawVert>(cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size);
                var targetVertexData = new Span<ImDrawVert>(vertices, currentVertex, cmdList.VtxBuffer.Size);

                vertexData.CopyTo(targetVertexData);

                currentVertex += cmdList.VtxBuffer.Size;

                var indexData = new Span<ushort>(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size);
                var targetIndexData = new Span<ushort>(indices, currentIndex, cmdList.IdxBuffer.Size);

                indexData.CopyTo(targetIndexData);

                currentIndex += cmdList.IdxBuffer.Size;
            }

            var needsUpdate = true;

            if ((vertexBuffer?.Disposed ?? true) == true)
            {
                needsUpdate = false;
            }

            if (needsUpdate == false)
            {
                vertexBuffer = RenderSystem.Backend.CreateVertexBuffer(vertices, layout, RenderBufferFlags.None);
                indexBuffer = RenderSystem.Backend.CreateIndexBuffer(indices, RenderBufferFlags.None);
            }
            else
            {
                vertexBuffer.Update(vertices);
                indexBuffer.Update(indices);
            }

            RenderSystem.Render(null, Scene.current == null ? CameraClearMode.SolidColor : CameraClearMode.None, StapleEditor.ClearColor,
                new(0, 0, 1, 1), Matrix4x4.Identity, ortho,
                () =>
                {
                    currentVertex = 0;
                    currentIndex = 0;

                    for (int i = 0; i < drawData.CmdListsCount; i++)
                    {
                        var cmdList = drawData.CmdLists.Data[i];

                        var numVertices = cmdList.VtxBuffer.Size;
                        var numIndices = cmdList.IdxBuffer.Size;

                        for (var j = 0; j < cmdList.CmdBuffer.Size; j++)
                        {
                            var drawCmd = cmdList.CmdBuffer.Data[j];

                            if (drawCmd.ElemCount == 0 || drawCmd.UserCallback != null)
                            {
                                continue;
                            }

                            var program = this.program.instances.FirstOrDefault().Value.program;

                            Texture[] textures;

                            if (drawCmd.GetTexID().IsNull == false)
                            {
                                var index = (int)drawCmd.GetTexID().Handle;

                                if (this.textures.TryGetValue(index, out var item) && item.Item1 != null)
                                {
                                    singleTexture[0] = item.Item1;
                                }
                                else if (registeredTextures.TryGetValue(index, out var t) && t != null)
                                {
                                    singleTexture[0] = t;
                                }

                                textures = singleTexture;
                            }
                            else
                            {
                                textures = [Material.WhiteTexture];
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

                                var state = new RenderState()
                                {
                                    sourceBlend = BlendMode.SrcAlpha,
                                    destinationBlend = BlendMode.OneMinusSrcAlpha,
                                    cull = CullingMode.None,
                                    depthWrite = false,
                                    enableDepth = false,
                                    primitiveType = MeshTopology.Triangles,
                                    scissor = new(x, (int)clipRect.Z, y, (int)clipRect.W),
                                    indexBuffer = indexBuffer,
                                    vertexBuffer = vertexBuffer,
                                    startVertex = currentVertex + (int)drawCmd.VtxOffset,
                                    startIndex = currentIndex + (int)drawCmd.IdxOffset,
                                    indexCount = (int)drawCmd.ElemCount,
                                    shader = this.program,
                                    shaderVariant = "",
                                    fragmentTextures = textures,
                                    world = Matrix4x4.Identity,
                                };

                                RenderSystem.Submit(state, (int)drawCmd.ElemCount / 3, 1);
                            }
                        }

                        currentVertex += numVertices;
                        currentIndex += numIndices;
                    }
                });
        }
    }

    public int RegisterTexture(Texture texture)
    {
        if(registeredTexturesInverse.TryGetValue(texture, out var ID))
        {
            return ID;
        }

        var index = textureCounter++;

        registeredTextures.Add(index, texture);
        registeredTexturesInverse.Add(texture, index);

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImTextureRef GetImGuiTexture(Texture texture)
    {
        unsafe
        {
            if (texture == null ||
                texture.Disposed ||
                instance == null)
            {
                return new ImTextureRef(texId: ImTextureID.Null);
            }

            return new ImTextureRef(texId: new ImTextureID(instance.RegisterTexture(texture)));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ImGuiRGBA(byte r, byte g, byte b, byte a = 255)
    {
        return ImGui.ColorConvertFloat4ToU32(new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f));
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
