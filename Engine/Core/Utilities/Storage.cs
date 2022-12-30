using System;
using System.IO;

namespace Staple
{
    public static class Storage
    {
        internal static string basePath;

        internal static string BasePath
        {
            get
            {
                if(basePath != null)
                {
                    return basePath;
                }

                //TODO: other OSs

                basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow");

                return basePath;
            }
        }

        public static string AppName
        {
            get;
            private set;
        }

        public static string CompanyName
        {
            get;
            private set;
        }

        public static string PersistentDataPath
        {
            get
            {
                if(AppName == null || CompanyName == null)
                {
                    return null;
                }

                return Path.Combine(BasePath, CompanyName, AppName);
            }
        }

        internal static void Update(string appName, string companyName)
        {
            AppName = appName;
            CompanyName = companyName;

            try
            {
                var path = PersistentDataPath;

                if(path != null)
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}