using System;
using System.Runtime.InteropServices;

namespace NfdSharp
{
    /// <summary>
    /// Wrapper class for the nfd library <see href="https://github.com/mlabbe/nativefiledialog"/>
    /// </summary>
    public static class Nfd
    {
        public enum NfdResult
        {
            /// <summary>
            /// Programmatic error
            /// </summary>
            NFD_ERROR,
            /// <summary>
            /// User pressed okay, or successful return
            /// </summary>
            NFD_OKAY,
            /// <summary>
            /// User pressed cancel
            /// </summary>
            NFD_CANCEL
        }

        private struct NfdPathset
        {
            public UIntPtr buf;
            /// <summary>
            /// Byte offsets into buf
            /// </summary>
            public UIntPtr indices;
            /// <summary>
            /// Number of indices into buf
            /// </summary>
            public ulong count;
        }

        /// <summary>
        /// Single file open dialog
        /// </summary>
        /// <param name="filterList">; denotes a new filter group and , extends the filter group.</param>
        /// <param name="defaultPath"></param>
        /// <param name="path">The output path</param>
        /// <returns>
        /// Should return <see cref="NfdResult.NFD_OKAY"/> or <see cref="NfdResult.NFD_CANCEL"/>.
        /// On <see cref="NfdResult.NFD_ERROR"/> check with <see cref="GetError"/>.
        /// </returns>
        public static NfdResult OpenDialog(string filterList, string defaultPath, out string path)
        {
            IntPtr filterListPtr = Utils.ToNfdString(filterList);
            IntPtr defaultPathPtr = Utils.ToNfdString(defaultPath);
            
            var res = NFD_OpenDialog(filterListPtr, defaultPathPtr, out IntPtr outPath);

            Marshal.FreeHGlobal(filterListPtr);
            Marshal.FreeHGlobal(defaultPathPtr);
            
            path = res != NfdResult.NFD_OKAY ? "" : Utils.FromNfdString(outPath);
            
            Marshal.FreeHGlobal(outPath);
            
            return res;
        }
        
        /// <summary>
        /// Multiple file open dialog
        /// </summary>
        /// <param name="filterList">; denotes a new filter group and , extends the filter group.</param>
        /// <param name="defaultPath"></param>
        /// <param name="outPaths">The output paths</param>
        /// <returns>
        /// Should return <see cref="NfdResult.NFD_OKAY"/> or <see cref="NfdResult.NFD_CANCEL"/>.
        /// On <see cref="NfdResult.NFD_ERROR"/> check with <see cref="GetError"/>.
        /// </returns>
        public static NfdResult OpenDialogMultiple(string filterList, string defaultPath, out string[] outPaths)
        {
            IntPtr filterListPtr = Utils.ToNfdString(filterList);
            IntPtr defaultPathPtr = Utils.ToNfdString(defaultPath);

            var res = NFD_OpenDialogMultiple(filterListPtr, defaultPathPtr, out NfdPathset paths);

            Marshal.FreeHGlobal(filterListPtr);
            Marshal.FreeHGlobal(defaultPathPtr);
            
            if (res == NfdResult.NFD_OKAY)
            {
                ulong count = paths.count;

                outPaths = count != 0 ? new string[count] : null;
            
                for (ulong i = 0; i < count; i++)
                {
                    outPaths[i] = Utils.FromNfdString(NFD_PathSet_GetPath(ref paths, i));
                }
                
                NFD_PathSet_Free(ref paths);
            }
            else
            {
                outPaths = null;
            }

            return res;
        }

        /// <summary>
        /// Save dialog
        /// </summary>
        /// <param name="filterList">; denotes a new filter group and , extends the filter group.</param>
        /// <param name="defaultPath"></param>
        /// <param name="path">The output path</param>
        /// <returns>
        /// Should return <see cref="NfdResult.NFD_OKAY"/> or <see cref="NfdResult.NFD_CANCEL"/>.
        /// On <see cref="NfdResult.NFD_ERROR"/> check with <see cref="GetError"/>.
        /// </returns>
        public static NfdResult SaveDialog(string filterList, string defaultPath, out string path)
        {
            IntPtr filterListPtr = Utils.ToNfdString(filterList);
            IntPtr defaultPathPtr = Utils.ToNfdString(defaultPath);
            
            var res = NFD_SaveDialog(filterListPtr, defaultPathPtr, out IntPtr outPath);

            Marshal.FreeHGlobal(filterListPtr);
            Marshal.FreeHGlobal(defaultPathPtr);
            
            path = res != NfdResult.NFD_OKAY ? "" : Utils.FromNfdString(outPath);
            
            Marshal.FreeHGlobal(outPath);
            
            return res;
        }

        /// <summary>
        /// Select folder dialog
        /// </summary>
        /// <param name="defaultPath">; denotes a new filter group and , extends the filter group.</param>
        /// <param name="path">The output path</param>
        /// <returns>
        /// Should return <see cref="NfdResult.NFD_OKAY"/> or <see cref="NfdResult.NFD_CANCEL"/>.
        /// On <see cref="NfdResult.NFD_ERROR"/> check with <see cref="GetError"/>.
        /// </returns>
        public static NfdResult PickFolder(string defaultPath, out string path)
        {
            IntPtr defaultPathPtr = Utils.ToNfdString(defaultPath);
            
            var res = NFD_PickFolder(defaultPathPtr, out IntPtr outPath);

            Marshal.FreeHGlobal(defaultPathPtr);
            
            path = res != NfdResult.NFD_OKAY ? "" : Utils.FromNfdString(outPath);
            
            Marshal.FreeHGlobal(outPath);
            
            return res;
        }

        /// <summary>
        /// To get an error message from nfd on <see cref="NfdResult.NFD_ERROR"/>
        /// </summary>
        /// <returns></returns>
        public static string GetError()
        {
            IntPtr message = NFD_GetError();
            string ret = Utils.FromNfdString(message);
            
            return ret;
        }

        #region DllImports

        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern NfdResult NFD_OpenDialog(
            IntPtr filterList,
            IntPtr defaultPath,
            out IntPtr outPath);
        
        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern NfdResult NFD_OpenDialogMultiple(
            IntPtr filterList,
            IntPtr defaultPath,
            out NfdPathset outPaths);
        
        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern NfdResult NFD_SaveDialog(
            IntPtr filterList,
            IntPtr defaultPath,
            out IntPtr outPath);
        
        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern NfdResult NFD_PickFolder(
            IntPtr defaultPath,
            out IntPtr outPath);
        
        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NFD_GetError();

        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong NFD_PathSet_GetCount(ref NfdPathset pathSet);
        
        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NFD_PathSet_GetPath(ref NfdPathset pathSet, ulong index);
        
        [DllImport("nfd", CallingConvention = CallingConvention.Cdecl)]
        private static extern void NFD_PathSet_Free(ref NfdPathset pathSet);

        #endregion
    }
}
