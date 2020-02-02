using System;
using System.Collections.Generic;
using System.IO;

namespace Azurlane
{
    internal static class ConfigMgr
    {
        internal static readonly Dictionary<Key, object> Instance;

        static ConfigMgr()
        {
            if (Instance == null)
                Instance = new Dictionary<Key, object>();
        }

        internal static object GetValue(Key key)
        {
            return Instance[key];
        }

        internal static void Initialize()
        {
            var iniPath = PathMgr.Local("Configuration.ini");

            foreach (var line in File.ReadAllLines(iniPath))
                if (line.Contains("="))
                {
                    var s = line.Split('=');
                    var key = s[0];
                    object value = s[1];

                    foreach (Key keyName in Enum.GetValues(typeof(Key)))
                        if (key.Compare(keyName))
                            Add(keyName, value.GetValue());

                    foreach (Mods modName in Enum.GetValues(typeof(Mods)))
                        if (key.Compare(modName))
                            Program.SetValue(modName, (bool) value.GetValue());
                }
        }

        private static void Add(Key key, object obj)
        {
            Instance.Add(key, obj);
        }

        private static bool Compare(this string s, Mods mod)
        {
            return s == mod.ToString().Replace("_", "+");
        }

        private static bool Compare(this string s, Key mod)
        {
            return s == mod.ToString();
        }

        private static object GetValue(this object o)
        {
            return ((string) o).ToLower() == "true" ? true :
                ((string) o).ToLower() == "ignore" || ((string) o).ToLower() == "false" ? false : o;
        }

        internal enum Key
        {
            Version,
            DevelopmentMode,
            iOS,
            Tmp,
            Thirdparty,
            RemoveSkill,
            ReplaceSkin,
            EasyMode
        }
    }
}