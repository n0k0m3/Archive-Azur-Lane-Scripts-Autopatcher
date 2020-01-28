using System.IO;
using System.Reflection;

namespace Azurlane
{
    internal static class PathMgr
    {
        internal static string Local(string path = null)
        {
            var root = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (path != null && !File.Exists(path) && !Directory.Exists(path) && !path.Contains("."))
                Directory.CreateDirectory(path);

            return path == null ? root : Path.Combine(root, path);
        }

        internal static string Thirdparty(string path = null) => path != null ? Path.Combine(Local((string)ConfigMgr.GetValue(ConfigMgr.Key.Thirdparty)), path) : Local((string)ConfigMgr.GetValue(ConfigMgr.Key.Thirdparty));
    }
}