namespace Staple
{
    public static class StaplePlayer
    {
        public static void Run(string[] args)
        {
            new AppPlayer(new AppSettings()
            {
                appName = "Test",
                companyName = "Test Company",
                runInBackground = false,
            }, args).Run();
        }
    }
}
