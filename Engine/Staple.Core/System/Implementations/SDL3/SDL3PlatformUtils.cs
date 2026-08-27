using SDL;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal static class SDL3PlatformUtils
{
    public static SDL_MessageBoxFlags GetMessageBoxFlags(MessageBoxType type)
    {
        return type switch
        {
            MessageBoxType.Warning => SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
            MessageBoxType.Error => SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
            _ => SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
        };
    }

    public static void ShowMessageBox(MessageBoxType type, string title,
        string message, string okTitle, string cancelTitle = null, Action onOK = null,
        Action onCancel = null)
    {
        var buttonData = new SDL_MessageBoxButtonData[string.IsNullOrEmpty(cancelTitle) ? 1 : 2];

        var titleBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(title) ? "" : title);
        var messageBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(message) ? "" : message);
        var okBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(okTitle) ? "OK" : okTitle);
        var cancelBytes = string.IsNullOrEmpty(cancelTitle) ? null : Encoding.UTF8.GetBytes(cancelTitle);

        buttonData[0] = new()
        {
            flags = SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT,
        };

        if (buttonData.Length == 2)
        {
            buttonData[1] = new()
            {
                flags = SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT,
            };

            buttonData[0].buttonID = 1;
            buttonData[0].flags = SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT;
        }

        var messageBoxData = new SDL_MessageBoxData()
        {
            flags = GetMessageBoxFlags(type),
            numbuttons = buttonData.Length,
        };

        unsafe
        {
            fixed (byte* titlePtr = titleBytes)
            {
                fixed (byte* messagePtr = messageBytes)
                {
                    fixed (byte* okPtr = okBytes)
                    {
                        fixed (SDL_MessageBoxButtonData* buttons = buttonData)
                        {
                            messageBoxData.buttons = buttons;
                            messageBoxData.title = titlePtr;
                            messageBoxData.message = messagePtr;

                            int buttonID;

                            if (buttonData.Length == 2)
                            {
                                fixed (byte* cancelPtr = cancelBytes)
                                {
                                    buttonData[0].text = cancelPtr;
                                    buttonData[1].text = okPtr;

                                    if (SDL3.SDL_ShowMessageBox(&messageBoxData, &buttonID))
                                    {
                                        if (buttonID == 0)
                                        {
                                            onOK?.Invoke();
                                        }
                                        else
                                        {
                                            onCancel?.Invoke();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                buttonData[0].text = okPtr;

                                if (SDL3.SDL_ShowMessageBox(&messageBoxData, &buttonID))
                                {
                                    if (buttonID == 0)
                                    {
                                        onOK?.Invoke();
                                    }
                                    else
                                    {
                                        onCancel?.Invoke();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
