using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Azurlane
{
    public class AssetBundleMgr
    {
        private static readonly List<byte[]> DPatterns, EPatterns;

        private static readonly object Instance;

        static AssetBundleMgr()
        {
            if (DPatterns == null)
            {
                DPatterns = new List<byte[]>
                {
                    new byte[]
                    {
                        0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00,
                        0x00, 0x00, 0x00, 0x06, 0x35, 0x2E, 0x78, 0x2E
                    }
                };
            }

            if (EPatterns == null)
            {
                EPatterns = new List<byte[]>
                {
                    new byte[]
                    {
                        0xC7, 0xD5, 0xFC, 0x1F, 0x4C, 0x92, 0x94, 0x55,
                        0x85, 0x03, 0x16, 0xA3, 0x7F, 0x7B, 0x8B, 0x55
                    }
                };
            }

            if (DPatterns == null || EPatterns == null)
                return;

            var assembly = Assembly.Load(Properties.Resources.Salt);
            Instance = Activator.CreateInstance(assembly.GetType("LL.Salt"));
        }

        internal static bool Compare(byte[] b1, List<byte[]> b2)
        {
            try
            {
                foreach (var b in b2)
                {
                    for (var i = 0; i < b.Length; i++)
                    {
                        if (b1[i] != b[i])
                            return false;
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogException("Exception detected during Compare.2", e);
            }
            return true;
        }

        internal static void Initialize(string path, Tasks task)
        {
            var bytes = File.ReadAllBytes(path);
            if (Compare(bytes, EPatterns))
            {
                if (task == Tasks.Encrypt)
                {
                    Utils.LogInfo("AssetBundle is already encrypted... <aborted>", true, true);
                    return;
                }
                if (task == Tasks.Unpack || task == Tasks.Repack)
                    Execute(bytes, path, Tasks.Decrypt);
            }
            else if (Compare(bytes, DPatterns))
            {
                if (task == Tasks.Decrypt)
                {
                    Utils.LogInfo("AssetBundle is already decrypted... <aborted>", true, true);
                    return;
                }
            }
            else
            {
                Utils.LogInfo("Not a valid/damaged AssetBundle... <aborted>", true, true);
                return;
            }

            if (task == Tasks.Decrypt || task == Tasks.Encrypt)
            {
                Execute(bytes, path, task);
            }
            else if (task == Tasks.Unpack || task == Tasks.Repack)
            {
                Execute(path, task);
            }
            Program.IsValid = true;
        }

        private static void Execute(byte[] bytes, string path, Tasks task)
        {
            Utils.LogInfo("{0} {1}...", true, false, task == Tasks.Decrypt ? "Decrypting" : "Encrypting", Path.GetFileName(path));

            var method = Instance.GetType().GetMethod("Make", BindingFlags.Static | BindingFlags.Public);
            bytes = (byte[])method.Invoke(Instance, new object[] { bytes, task == Tasks.Encrypt });

            File.WriteAllBytes(path, bytes);
            Utils.Write(" <done>", false, true);
        }

        private static void Execute(string path, Tasks task)
        {
            Utils.LogInfo("{0} {1}...", true, false, task == Tasks.Unpack ? "Unpacking" : "Repacking", Path.GetFileName(path));
            Utils.Command($"UnityEX.exe {(task == Tasks.Unpack ? "export" : "import")} \"{path}\"", PathMgr.Thirdparty("unityex"));
            Utils.Write(" <done>", false, true);
        }
    }
}