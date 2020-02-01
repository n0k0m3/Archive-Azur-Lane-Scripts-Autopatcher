namespace Azurlane
{
    internal static class PathMgr
    {
        private const string AssetsRoot = "Unity_Assets_Files";

        internal static string Local(string path = null)
        {
            var root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (path != null && !File.Exists(path) && !Directory.Exists(path) && !path.Contains("."))
                Directory.CreateDirectory(path);

            return path == null ? root : Path.Combine(root, path);
        }

        internal static string Assets()
        {
            return Temp(AssetsRoot);
        }

        internal static string Assets(string name)
        {
            return Path.Combine(Temp(AssetsRoot), name);
        }

        internal static string Lua(string name)
        {
            return Path.Combine(Path.Combine(Assets(), name), Program.DirName);
        }

        internal static string Lua(string name, string lua)
        {
            return Path.Combine(Lua(name), lua);
        }

        internal static string Temp(string path = null)
        {
            return path != null
                ? Path.Combine(Local((string) ConfigMgr.GetValue(ConfigMgr.Key.Tmp)), path)
                : Local((string) ConfigMgr.GetValue(ConfigMgr.Key.Tmp));
        }

        internal static string Temp(string s1, string s2)
        {
            return Path.Combine(Temp(s1), s2);
        }

        internal static string Thirdparty(string path = null)
        {
            return path != null
                ? Path.Combine(Local((string) ConfigMgr.GetValue(ConfigMgr.Key.Thirdparty)), path)
                : Local((string) ConfigMgr.GetValue(ConfigMgr.Key.Thirdparty));
        }
    }
}