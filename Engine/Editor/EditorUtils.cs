using System.Text;
using System;

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

        public static string ExpandCamelCaseName(this string self)
        {
            var outString = new StringBuilder();

            var hadUppercase = true;
            var hadLowercase = false;
            var lastLowercaseIndex = 0;

            for (var i = 0; i < self.Length; i++)
            {
                if (char.IsUpper(self[i]))
                {
                    if (!hadUppercase && i > 0 && char.IsDigit(self[i - 1]) == false)
                    {
                        outString.Append(self.AsSpan(lastLowercaseIndex, i - lastLowercaseIndex));

                        if (outString.Length > 0)
                        {
                            outString.Append(' ');
                        }

                        lastLowercaseIndex = i;
                    }

                    outString.Append(self[i]);

                    hadUppercase = true;

                    if(i == self.Length - 1)
                    {
                        break;
                    }
                }
                else if (char.IsDigit(self[i]))
                {
                    if (i > 0 && !char.IsDigit(self[i - 1]) && !char.IsWhiteSpace(self[i - 1]) && hadLowercase)
                    {
                        outString.Append(self.AsSpan(lastLowercaseIndex, i - lastLowercaseIndex));

                        if (outString.Length > 0)
                        {
                            outString.Append(' ');
                        }
                    }

                    outString.Append(self[i]);

                    hadUppercase = false;
                    lastLowercaseIndex = (i + 1 < self.Length ? i + 1 : i);
                }
                else
                {
                    if (hadUppercase)
                    {
                        hadUppercase = false;
                        lastLowercaseIndex = i;
                    }

                    hadLowercase = true;
                }

                if (i == self.Length - 1)
                {
                    if(hadLowercase)
                    {
                        if (hadUppercase)
                        {
                            outString.Append(self.AsSpan(lastLowercaseIndex, (i + 1) - lastLowercaseIndex));
                        }
                        else
                        {
                            outString.Append(self.AsSpan(lastLowercaseIndex, (i + 1) - lastLowercaseIndex));
                        }
                    }
                }
            }

            var result = outString.ToString();

            if(result.Length > 0 && char.IsLower(result[0]))
            {
                result = char.ToUpperInvariant(result[0]) + result.Substring(1);
            }

            return result;
        }

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
