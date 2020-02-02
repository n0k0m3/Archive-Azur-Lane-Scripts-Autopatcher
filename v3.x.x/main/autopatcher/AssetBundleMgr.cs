using System;
using System.Collections.Generic;
using System.IO;

namespace Azurlane
{
    public class AssetBundleMgr
    {
        private static readonly List<byte[]> DecryptionPatterns, EncryptionPatterns;

        static AssetBundleMgr()
        {
            // Check whether decryption patterns are null
            if (DecryptionPatterns == null)
                // Initialize
                DecryptionPatterns = new List<byte[]>
                {
                    new byte[]
                    {
                        0x55, 0x6E, 0x69, 0x74, 0x79, 0x46, 0x53, 0x00,
                        0x00, 0x00, 0x00, 0x06, 0x35, 0x2E, 0x78, 0x2E
                    }
                };

            // Check whether encryption patterns are null
            if (EncryptionPatterns == null)
                // Initialize
                EncryptionPatterns = new List<byte[]>
                {
                    new byte[]
                    {
                        0xC7, 0xD5, 0xFC, 0x1F, 0x4C, 0x92, 0x94, 0x55,
                        0x85, 0x03, 0x16, 0xA3, 0x7F, 0x7B, 0x8B, 0x55
                    }
                };
        }

        internal static bool CheckAssetBundle(string path)
        {
            var bytes = File.ReadAllBytes(path);
            return Compare(bytes, EncryptionPatterns);
        }

        /*
        internal static bool Compare(byte[] b1, byte[] b2)
        {
            try
            {
                for (var i = 0; i < b2.Length; i++)
                {
                    if (b1[i] != b2[i])
                        return false;
                }
            }
            catch (Exception e)
            {
                Utils.LogException("Exception detected during Compare.1", e);
            }
            return true;
        }*/

        internal static bool Compare(byte[] b1, List<byte[]> b2)
        {
            try
            {
                foreach (var b in b2)
                    for (var i = 0; i < b.Length; i++)
                        if (b1[i] != b[i])
                            return false;
            }
            catch (Exception e)
            {
                Utils.LogException("Exception detected during Compare.2", e);
            }

            return true;
        }
    }
}