namespace Staple.Editor
{
    public static class EditorUtils
    {
        private static readonly string[] byteSizes = new string[]
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        public static string ByteSizeString(long size)
        {
            var counter = 0;

            while (size >= 1024 && counter < byteSizes.Length)
            {
                counter++;

                size /= 1024;
            }

            return $"{size}{byteSizes[counter]}";
        }

        public static void RefreshAssets()
        {
            RefreshAssets(true);
        }

        internal static void RefreshAssets(bool updateProject)
        {
            if (StapleEditor.instance.TryGetTarget(out var editor))
            {
                editor.RefreshAssets(updateProject);
            }
        }
    }
}
