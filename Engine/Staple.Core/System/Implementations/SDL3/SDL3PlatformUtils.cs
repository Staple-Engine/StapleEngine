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
    private const string LogTag = "SDL3PlatformUtils";

    private class Container<T>
    {
        public T instance;
    }

    private class FileOpenData
    {
        public SDL_DialogFileFilter[] filterData;
        public GCHandle filterDataHandle;
        public byte[][] filterNames;
        public byte[][] filterExtensions;
        public GCHandle[] filterNameHandles;
        public GCHandle[] filterExtensionHandles;
        public SDL_PropertiesID props;

        public Action<Span<string>> success;
        public Action failure;
    }

    private class FolderOpenData
    {
        public SDL_PropertiesID props;

        public Action<Span<string>> success;
        public Action failure;
    }

    private static readonly Dictionary<int, FileOpenData> fileOpenCallbacks = [];
    private static readonly Dictionary<int, FileOpenData> fileSaveCallbacks = [];
    private static readonly Dictionary<int, FolderOpenData> folderOpenCallbacks = [];
    private static int fileOpenCounter;
    private static int fileSaveCounter;
    private static int folderOpenCounter;

    private static unsafe void FileCallback(nint userData, byte** fileList, int filter, Dictionary<int, FileOpenData> container)
    {
        if (!container.TryGetValue((int)userData, out var fileData))
        {
            Log.Error($"File Callback with invalid user data: {userData}. Pending open callbacks: {container.Count}");

            return;
        }

        container.Remove((int)userData);

        fileData.filterDataHandle.Free();

        foreach (var handle in fileData.filterNameHandles)
        {
            handle.Free();
        }

        foreach (var handle in fileData.filterExtensionHandles)
        {
            handle.Free();
        }

        SDL3.SDL_DestroyProperties(fileData.props);

        if (fileList == null)
        {
            ThreadHelper.Dispatch(() =>
            {
                fileData.failure?.Invoke();
            });

            return;
        }

        var files = new List<string>();

        var counter = 0;

        while (fileList[counter] != null)
        {
            var s = fileList[counter];

            var length = 0;

            while (s[length] != '\0')
            {
                length++;
            }

            var path = Encoding.UTF8.GetString(s, length);

            files.Add(path);

            counter++;
        }

        if(files.Count == 0)
        {
            ThreadHelper.Dispatch(() =>
            {
                fileData.failure?.Invoke();
            });

            return;
        }

        ThreadHelper.Dispatch(() =>
        {
            fileData.success?.Invoke(CollectionsMarshal.AsSpan(files));
        });
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private static unsafe void FolderCallback(nint userData, byte** fileList, int filter)
    {
        if (!folderOpenCallbacks.TryGetValue((int)userData, out var folderData))
        {
            Log.Error($"Folder Callback with invalid user data: {userData}. Pending open callbacks: {folderOpenCallbacks.Count}");

            return;
        }

        folderOpenCallbacks.Remove((int)userData);

        SDL3.SDL_DestroyProperties(folderData.props);

        if (fileList == null)
        {
            ThreadHelper.Dispatch(() =>
            {
                folderData.failure?.Invoke();
            });

            return;
        }

        var files = new List<string>();

        var counter = 0;

        while (fileList[counter] != null)
        {
            var s = fileList[counter];

            var length = 0;

            while (s[length] != '\0')
            {
                length++;
            }

            var path = Encoding.UTF8.GetString(s, length);

            files.Add(path);

            counter++;
        }

        if (files.Count == 0)
        {
            ThreadHelper.Dispatch(() =>
            {
                folderData.failure?.Invoke();
            });

            return;
        }

        ThreadHelper.Dispatch(() =>
        {
            folderData.success?.Invoke(CollectionsMarshal.AsSpan(files));
        });
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private static unsafe void FileOpenCallback(nint userData, byte **fileList, int filter)
    {
        FileCallback(userData, fileList, filter, fileOpenCallbacks);
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    private static unsafe void FileSaveCallback(nint userData, byte** fileList, int filter)
    {
        FileCallback(userData, fileList, filter, fileSaveCallbacks);
    }

    public static SDL_MessageBoxFlags GetMessageBoxFlags(MessageBoxType type)
    {
        return type switch
        {
            MessageBoxType.Warning => SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
            MessageBoxType.Error => SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
            _ => SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION,
        };
    }

    private static void ShowFileDialog(SDL_FileDialogType type, ref int fileCounter, Dictionary<int, FileOpenData> container,
        string title, string okTitle, string cancelTitle, string startPath, Dictionary<string, string> filters,
        bool allowMultiple, Action<Span<string>> success, Action failure)
    {
        if(type == SDL_FileDialogType.SDL_FILEDIALOG_OPENFOLDER)
        {
            throw new ArgumentException("Type should be open or save file!", nameof(type));
        }

        ThreadHelper.Dispatch(() =>
        {
            var fileOpenData = new FileOpenData()
            {
                filterData = new SDL_DialogFileFilter[filters.Count],
                filterExtensions = new byte[filters.Count][],
                filterNames = new byte[filters.Count][],
                filterNameHandles = new GCHandle[filters.Count],
                filterExtensionHandles = new GCHandle[filters.Count],
                success = success,
                failure = failure,
            };

            var index = fileCounter++;

            container.Add(index, fileOpenData);

            var counter = 0;

            foreach (var (t, e) in filters)
            {
                fileOpenData.filterNames[counter] = Encoding.UTF8.GetBytes(t);
                fileOpenData.filterExtensions[counter] = Encoding.UTF8.GetBytes(e);
                fileOpenData.filterNameHandles[counter] = GCHandle.Alloc(fileOpenData.filterNames[counter], GCHandleType.Pinned);
                fileOpenData.filterExtensionHandles[counter] = GCHandle.Alloc(fileOpenData.filterExtensions[counter], GCHandleType.Pinned);

                unsafe
                {
                    fileOpenData.filterData[counter].name = (byte*)fileOpenData.filterNameHandles[counter].AddrOfPinnedObject();
                    fileOpenData.filterData[counter].pattern = (byte*)fileOpenData.filterExtensionHandles[counter].AddrOfPinnedObject();
                }

                counter++;
            }

            fileOpenData.filterDataHandle = GCHandle.Alloc(fileOpenData.filterData, GCHandleType.Pinned);

            var prop = SDL3.SDL_CreateProperties();

            fileOpenData.props = prop;

            SDL3.SDL_SetPointerProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_FILTERS_POINTER, fileOpenData.filterDataHandle.AddrOfPinnedObject());
            SDL3.SDL_SetNumberProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_NFILTERS_NUMBER, fileOpenData.filterData.Length);
            SDL3.SDL_SetBooleanProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_MANY_BOOLEAN, allowMultiple);

            if (!string.IsNullOrEmpty(startPath))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_LOCATION_STRING, startPath);
            }

            if (!string.IsNullOrEmpty(title))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_TITLE_STRING, title);
            }

            if (!string.IsNullOrEmpty(okTitle))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_ACCEPT_STRING, okTitle);
            }

            if (!string.IsNullOrEmpty(cancelTitle))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_CANCEL_STRING, cancelTitle);
            }

            unsafe
            {
                SDL3.SDL_ShowFileDialogWithProperties(type, type switch
                {
                    SDL_FileDialogType.SDL_FILEDIALOG_OPENFILE => &FileOpenCallback,
                    _ => &FileSaveCallback,
                },
                index, prop);
            }
        });
    }

    public static void ShowOpenFolderDialog(string title, string okTitle, string cancelTitle, string startPath, bool allowMultiple,
        Action<Span<string>> success, Action failure)
    {
        ThreadHelper.Dispatch(() =>
        {
            var folderOpenData = new FolderOpenData()
            {
                success = success,
                failure = failure,
            };

            var index = folderOpenCounter++;

            folderOpenCallbacks.Add(index, folderOpenData);

            var prop = SDL3.SDL_CreateProperties();

            folderOpenData.props = prop;

            SDL3.SDL_SetBooleanProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_MANY_BOOLEAN, allowMultiple);

            if (!string.IsNullOrEmpty(startPath))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_LOCATION_STRING, startPath);
            }

            if (!string.IsNullOrEmpty(title))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_TITLE_STRING, title);
            }

            if (!string.IsNullOrEmpty(okTitle))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_ACCEPT_STRING, okTitle);
            }

            if (!string.IsNullOrEmpty(cancelTitle))
            {
                SDL3.SDL_SetStringProperty(prop, SDL3.SDL_PROP_FILE_DIALOG_CANCEL_STRING, cancelTitle);
            }

            unsafe
            {
                SDL3.SDL_ShowFileDialogWithProperties(SDL_FileDialogType.SDL_FILEDIALOG_OPENFOLDER, &FolderCallback, index, prop);
            }
        });
    }

    public static void ShowOpenFileDialog(string title, string okTitle, string cancelTitle, string startPath,
        Dictionary<string, string> filters, bool allowMultiple, Action<Span<string>> success, Action failure)
    {
        ShowFileDialog(SDL_FileDialogType.SDL_FILEDIALOG_OPENFILE, ref fileOpenCounter, fileOpenCallbacks, title, okTitle, cancelTitle, startPath,
            filters, allowMultiple, success, failure);
    }

    public static void ShowSaveFileDialog(string title, string okTitle, string cancelTitle, string startPath,
        Dictionary<string, string> filters, Action<Span<string>> success, Action failure)
    {
        ShowFileDialog(SDL_FileDialogType.SDL_FILEDIALOG_SAVEFILE, ref fileSaveCounter, fileSaveCallbacks, title, okTitle, cancelTitle, startPath,
            filters, false, success, failure);
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
                                    if (buttonID == 0 || buttonData.Length == 1)
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
