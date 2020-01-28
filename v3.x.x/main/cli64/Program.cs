using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Azurlane
{
    internal enum Tasks
    {
        Encrypt,
        Decrypt,
        Decompile,
        Recompile,
        Unpack,
        Repack,
    }

    public class Program
    {
        internal static bool IsDevMode;
        internal static bool IsValid = false;

        private static readonly List<string> ListOfAssetBundle = new List<string>(), ListOfLua = new List<string>();
        private static readonly Dictionary<Options, List<string>> Parameters = new Dictionary<Options, List<string>>();

        private static bool _abort;
        private static Options _currentOption = Options.None;

        private enum Options
        {
            None,
            LuaUnlock,
            LuaLock,
            LuaDecompile,
            LuaRecompile,
            AssetBundleDecrypt,
            AssetBundleEncrypt,
            AssetBundleUnpack,
            AssetBundleRepack,
        }

        private static void CheckDependencies()
        {
            var missingCount = 0;
            var pythonVersion = 0.0;

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "python";
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;

                    process.Start();
                    var result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (result.Contains("Python"))
                        pythonVersion = Convert.ToDouble(result.Split(' ')[1].Remove(3));
                    else pythonVersion = -0.0;
                }
            }
            catch
            {
                // Empty
            }

            if (pythonVersion.Equals(0.0) || pythonVersion.Equals(-0.0))
            {
                Utils.LogDebug("No python detected", true, true);
                Utils.LogInfo(Properties.Resources.SolutionPythonMessage, true, true);
                missingCount++;
            }
            else if (pythonVersion < 3.7)
            {
                Utils.LogDebug("Detected Python version {0}.x - expected 3.7.x or newer", true, true, pythonVersion);
                Utils.LogInfo(Properties.Resources.SolutionPythonMessage, true, true);
                missingCount++;
            }

            if (!Directory.Exists(PathMgr.Thirdparty("ljd")))
            {
                Utils.LogDebug(Properties.Resources.LuajitNotFoundMessage, true, true);
                Utils.LogInfo(Properties.Resources.SolutionReferMessage, true, true);
                missingCount++;
            }

            if (!Directory.Exists(PathMgr.Thirdparty("luajit")))
            {
                Utils.LogDebug(Properties.Resources.LjdNotFoundMessage, true, true);
                Utils.LogInfo(Properties.Resources.SolutionReferMessage, true, true);
                missingCount++;
            }

            if (!Directory.Exists(PathMgr.Thirdparty("unityex")))
            {
                Utils.LogDebug(Properties.Resources.UnityExNotFoundMessage, true, true);
                Utils.LogInfo(Properties.Resources.SolutionReferMessage, true, true);
                missingCount++;
            }

            if (missingCount > 0)
                _abort = true;
        }

        private static void CheckVersion()
        {
            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    var latestStatus = wc.DownloadString(Properties.Resources.CliStatus);
                    if (latestStatus != "ok")
                    {
                        _abort = true;
                        return;
                    }

                    var latestVersion = wc.DownloadString(Properties.Resources.CliVersion);
                    if ((string)ConfigMgr.GetValue(ConfigMgr.Key.Version) != latestVersion)
                    {
                        Utils.Write("[Obsolete CLI version]", true, true);
                        Utils.Write("Download the latest version from:", true, true);
                        Utils.Write(Properties.Resources.Repository, true, true);
                        _abort = true;
                    }
                }
            }
            catch
            {
                _abort = true;
            }
        }

        private static void Help(OptionSet options)
        {
            Utils.Write("Usage: Azurlane.exe <option> <path-to-file(s) or path-to-directory(s)>", true, true);
            Console.WriteLine();
            Utils.Write("Options:", true, true);
            options.WriteOptionDescriptions(Console.Out);
        }

        private static void Initialize()
        {
            ConfigMgr.Initialize();
            Message();
            CheckDependencies();
        }

        private static void Main(string[] args)
        {
            Initialize();
            if (_abort)
                return;

            var showHelp = args.Length < 2;
            var options = new OptionSet()
            {
                {"dev", "Development Mode", v => IsDevMode = true},
                {"u|unlock", "Decrypt Lua", v => _currentOption = Options.LuaUnlock},
                {"l|lock", "Encrypt Lua", v => _currentOption = Options.LuaLock},
                {"d|decompile", "Decompile Lua", v => _currentOption = Options.LuaDecompile},
                {"r|recompile", "Recompile Lua", v => _currentOption = Options.LuaRecompile},
                {"decrypt", "Decrypt AssetBundle", v => _currentOption = Options.AssetBundleDecrypt},
                {"encrypt", "Encrypt AssetBundle", v => _currentOption = Options.AssetBundleEncrypt},
                {"unpack", "Unpack AssetBundle", v => _currentOption = Options.AssetBundleUnpack},
                {"repack", "Repack AssetBundle", v => _currentOption = Options.AssetBundleRepack},
                {"<>", v => {
                    if (_currentOption == Options.None) {
                        showHelp = true;
                        return;
                    }

                    if (Parameters.TryGetValue(_currentOption, out var values))
                    {
                        values.Add(v);
                    }
                    else
                    {
                        values = new List<string> { v };
                        Parameters.Add(_currentOption, values);
                    }
                }}
            };

            if (showHelp)
            {
                Help(options);
                return;
            }

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Utils.LogException("Exception detected during parsing options", e);
            }

            CheckVersion();

            foreach (var parameter in Parameters)
            {
                foreach (var value in parameter.Value)
                {
                    if (!File.Exists(value) && !Directory.Exists(value))
                    {
                        Utils.Write($@"A file or directory named {value} does not exists.", true, true);
                    }
                    if (File.Exists(value))
                    {
                        if (OpContains(parameter.Key, "Lua")) ListOfLua.Add(Path.GetFullPath(value));
                        else ListOfAssetBundle.Add(Path.GetFullPath(value));
                    }
                    else if (Directory.Exists(value))
                    {
                        if (OpContains(parameter.Key, "Lua"))
                        {
                            foreach (var file in Directory.GetFiles(Path.GetFullPath(value), "*.lua*", SearchOption.AllDirectories))
                                ListOfLua.Add(file);
                        }
                        else
                        {
                            foreach (var file in Directory.GetFiles(Path.GetFullPath(value), "*", SearchOption.AllDirectories))
                                ListOfAssetBundle.Add(file);
                        }
                    }
                }
            }

            if (OpContains("Lua"))
            {
                foreach (var lua in ListOfLua)
                    LuaMgr.Initialize(lua, OpContains(Options.LuaUnlock) ? Tasks.Decrypt : OpContains(Options.LuaLock) ? Tasks.Encrypt : OpContains(Options.LuaRecompile) ? Tasks.Recompile : Tasks.Decompile);
            }
            else if (OpContains("AssetBundle"))
            {
                foreach (var assetbundle in ListOfAssetBundle)
                    AssetBundleMgr.Initialize(assetbundle, OpContains(Options.AssetBundleDecrypt) ? Tasks.Decrypt : OpContains(Options.AssetBundleEncrypt) ? Tasks.Encrypt : OpContains(Options.AssetBundleUnpack) ? Tasks.Unpack : Tasks.Repack);
            }

            if (IsValid && !OpContains(Options.AssetBundleRepack) && !OpContains(Options.AssetBundleDecrypt) && !OpContains(Options.AssetBundleEncrypt))
            {
                Console.WriteLine();
                Utils.Write($"{(OpContains(Options.LuaUnlock) || OpContains(Options.AssetBundleDecrypt) ? "Decrypt" : OpContains(Options.LuaLock) || OpContains(Options.AssetBundleEncrypt) ? "Encrypt" : OpContains(Options.LuaDecompile) ? "Decompile" : OpContains(Options.LuaRecompile) ? "Recompile" : OpContains(Options.AssetBundleUnpack) ? "Unpacking" : "Repacking")} {(OpContains(_currentOption, "Lua") ? string.Empty : "assetbundle ")}is done", true, true);

                if (!IsDevMode && !OpContains(Options.AssetBundleUnpack))
                    Utils.Write("Success: {0} - Failed: {1}", true, true, LuaMgr.SuccessCount, LuaMgr.FailedCount);
            }
        }

        private static void Message()
        {
            Utils.Write("", true, true);
            Utils.Write("Azur Lane Command Line Tool", true, true);
            Utils.Write("Version {0}", true, true, ConfigMgr.GetValue(ConfigMgr.Key.Version));
            Utils.Write("{0}", true, true, Properties.Resources.Author);
            Utils.Write("", true, true);
        }

        private static bool OpContains(Options option) => _currentOption == option;

        private static bool OpContains(Options option, string key) => option.ToString().Contains(key);

        private static bool OpContains(string key) => _currentOption.ToString().Contains(key);
    }
}