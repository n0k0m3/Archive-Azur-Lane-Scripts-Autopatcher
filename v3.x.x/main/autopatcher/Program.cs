using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Azurlane.Properties;

namespace Azurlane
{
    internal static class Program
    {
        internal static bool Abort;
        internal static List<string> ListOfLua;
        internal static Dictionary<Mods, bool> ListOfMod;
        internal static string DirName = "CAB-";
        internal static string Arch;
        internal static string LuaArch;
        internal static string os;

        private static List<Action> _listOfAction;

        internal static void SetValue(Mods key, bool value)
        {
            ListOfMod[key] = value;
        }

        private static void AddLua(string value)
        {
            ListOfLua.Add(value);
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
                Utils.LogInfo(Resources.SolutionPythonMessage, true, true);
                missingCount++;
            }
            else if (pythonVersion < 3.7)
            {
                Utils.LogDebug("Detected Python version {0}.x - expected 3.7.x or newer", true, true, pythonVersion);
                Utils.LogInfo(Resources.SolutionPythonMessage, true, true);
                missingCount++;
            }

            if (!Directory.Exists(PathMgr.Thirdparty("ljd")))
            {
                Utils.LogDebug(Resources.LuajitNotFoundMessage, true, true);
                Utils.LogInfo(Resources.SolutionReferMessage, true, true);
                missingCount++;
            }

            if (!Directory.Exists(PathMgr.Thirdparty("luajit")))
            {
                Utils.LogDebug(Resources.LjdNotFoundMessage, true, true);
                Utils.LogInfo(Resources.SolutionReferMessage, true, true);
                missingCount++;
            }

            if (!Directory.Exists(PathMgr.Thirdparty("unityex")))
            {
                Utils.LogDebug(Resources.UnityExNotFoundMessage, true, true);
                Utils.LogInfo(Resources.SolutionReferMessage, true, true);
                missingCount++;
            }

            if (missingCount > 0)
                Abort = true;
        }

        private static void CheckVersion()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    var latestStatus = wc.DownloadString(Resources.AutopatcherStatus);
                    if (latestStatus != "ok")
                    {
                        Abort = true;
                        return;
                    }

                    var latestVersion = wc.DownloadString(Resources.AutopatcherVersion);
                    if ((string) ConfigMgr.GetValue(ConfigMgr.Key.Version) != latestVersion)
                    {
                        Utils.Write("[Obsolete Autopatcher version]", true, true);
                        Utils.Write("Download the latest version from:", true, true);
                        Utils.Write(Resources.Repository, true, true);
                        Abort = false;
                    }
                }
            }
            catch
            {
                Abort = true;
            }
        }

        private static void Clean(string fileName)
        {
            try
            {
                if (File.Exists(PathMgr.Temp(fileName))) File.Delete(PathMgr.Temp(fileName));
                if (Directory.Exists(PathMgr.Lua(fileName).Replace($"\\{DirName}", "")))
                    Utils.Rmdir(PathMgr.Lua(fileName).Replace($"\\{DirName}", ""));

                foreach (var mod in ListOfMod.Keys)
                {
                    var modName = $"scripts{Arch}-{mod.ToString().ToLower().Replace("_", "-")}";
                    if (File.Exists(PathMgr.Temp(modName))) File.Delete(PathMgr.Temp(modName));
                    if (Directory.Exists(PathMgr.Lua(modName).Replace($"\\{DirName}", "")))
                        Utils.Rmdir(PathMgr.Lua(modName).Replace($"\\{DirName}", ""));
                }
            }
            catch (Exception e)
            {
                Utils.LogException("Exception detected during cleaning", e);
            }
        }

        private static bool GetValue(Mods key)
        {
            return ListOfMod[key];
        }

        private static void Initialize()
        {
            if (ListOfMod == null)
                ListOfMod = new Dictionary<Mods, bool>();

            foreach (Mods mod in Enum.GetValues(typeof(Mods)))
                ListOfMod.Add(mod, false);

            if (ListOfLua == null)
                ListOfLua = new List<string>();

            ConfigMgr.Initialize();

            Message();
            //CheckVersion();
            CheckDependencies();

            if ((bool) ConfigMgr.GetValue(ConfigMgr.Key.iOS))
                os = @"ios";
            else
                os = @"android";

            AddLua(Resources.Aircraft);
            AddLua(Resources.Enemy);

            if (GetValue(Mods.GodMode_Damage) || GetValue(Mods.GodMode_Cooldown) ||
                GetValue(Mods.GodMode_Damage_Cooldown) ||
                GetValue(Mods.GodMode_Damage_WeakEnemy) || GetValue(Mods.GodMode_Damage_Cooldown_WeakEnemy))
                AddLua(Resources.Weapon);

            if ((bool) ConfigMgr.GetValue(ConfigMgr.Key.ReplaceSkin))
            {
                AddLua(Resources.Ship);
                AddLua(Resources.ShipSkin);
            }

            if ((bool) ConfigMgr.GetValue(ConfigMgr.Key.RemoveSkill))
                AddLua(Resources.EnemySkill);

            if ((bool) ConfigMgr.GetValue(ConfigMgr.Key.EasyMode))
            {
                AddLua(Resources.MapData);
                AddLua(Resources.MapDataLoop);
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            Initialize();
            if (Abort)
                return;

            if (args.Length < 1)
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = @"Open an AssetBundle...";
                    dialog.Filter = @"Azurlane AssetBundle|scripts*";
                    dialog.CheckFileExists = true;
                    dialog.Multiselect = false;
                    dialog.ShowDialog();

                    if (File.Exists(dialog.FileName))
                    {
                        args = new[] {dialog.FileName};
                    }
                    else
                    {
                        Utils.Write(@"Please open an AssetBundle...", true, true);
                        goto END;
                    }
                }
            }
            else if (args.Length > 1)
            {
                Utils.Write(@"Invalid argument, usage: Azurlane.exe <path-to-assetbundle>", true, true);
                goto END;
            }

            var filePath = Path.GetFullPath(args[0]);
            var fileDirectoryPath = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            if (!File.Exists(filePath))
            {
                Utils.Write(
                    Directory.Exists(fileDirectoryPath)
                        ? $"{args[0]} is a directory, please input a file..."
                        : $"{args[0]} does not exists...", true, true);
                goto END;
            }

            if (!AssetBundleMgr.CheckAssetBundle(filePath))
            {
                Utils.Write("Not a valid AssetBundle file...", true, true);
                goto END;
            }

            if (fileName.Contains("64"))
            {
                Arch = @"64";
                LuaArch = @"64";
                Utils.LogInfo(@"Selected scripts is 64 bits", true, true);
            }
            else if (fileName.Contains("32"))
            {
                Arch = @"32";
                Utils.LogInfo(@"Selected scripts is 32 bits", true, true);
            }

            DirName = DirName + os + Arch;

            Clean(fileName);

            if (!Directory.Exists(PathMgr.Temp()))
                Directory.CreateDirectory(PathMgr.Temp());

            var startTime = DateTime.Now;
            var index = 1;
            if (_listOfAction == null)
                _listOfAction = new List<Action>
                {
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Copying AssetBundle to tmp workspace...", true, false);
                            File.Copy(filePath, PathMgr.Temp(fileName), true);
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during copying AssetBundle to tmp workspace", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Decrypting AssetBundle...", true, false);
                            Utils.Command($"Azcli{LuaArch}.exe --dev --decrypt \"{PathMgr.Temp(fileName)}\"");
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during decrypting AssetBundle", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Unpacking AssetBundle...", true, false);
                            Utils.Command($"Azcli{LuaArch}.exe --dev --unpack \"{PathMgr.Temp(fileName)}\"");
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during unpacking AssetBundle", e);
                        }
                    },
                    () =>
/*                     {
                        try {
                            var showDoneMessage = true;
                            Utils.LogInfo("Decrypting Lua...", true, false);
                            foreach (var lua in ListOfLua) {
                                Utils.Command($"Azcli{LuaArch}.exe --dev --unlock \"{PathMgr.Lua(fileName, lua)}\"");

                                if (LuaMgr.CheckLuaState(PathMgr.Lua(fileName, lua)) != LuaMgr.State.Encrypted)
                                    break;

                                Console.WriteLine();
                                Utils.LogDebug($"Failed to decrypt {Path.GetFileName(lua)}", true, true);
                                showDoneMessage = false;
                            }
                            if (showDoneMessage)
                                Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during decrypting Lua", e);
                        }
                    },
                    () => */
                    {
                        try
                        {
                            Utils.LogInfo("Decompiling Lua...", true, false);
                            var tasks = new List<Task>();
                            foreach (var lua in ListOfLua)
                                tasks.Add(Task.Factory.StartNew(() =>
                                {
                                    Utils.Command(
                                        $"Azcli{LuaArch}.exe --dev --decompile \"{PathMgr.Lua(fileName, lua)}\"");
                                    Utils.Write($@" {index}/{ListOfLua.Count}", false, false);
                                    index++;
                                }));
                            Task.WaitAll(tasks.ToArray());
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during decompiling Lua", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Cloning Lua & AssetBundle", true, false);
                            foreach (var mod in ListOfMod)
                                if (mod.Value)
                                {
                                    var modName = ($"scripts{Arch}-" + mod.Key).ToLower().Replace("_", "-");

                                    if (!Directory.Exists(PathMgr.Lua(modName)))
                                        Directory.CreateDirectory(PathMgr.Lua(modName));

                                    foreach (var lua in ListOfLua)
                                        if (File.Exists(PathMgr.Lua(fileName, lua)))
                                            File.Copy(PathMgr.Lua(fileName, lua), PathMgr.Lua(modName, lua), true);

                                    if (File.Exists(PathMgr.Temp(fileName)))
                                        File.Copy(PathMgr.Temp(fileName), PathMgr.Temp(modName), true);
                                }

                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during cloning lua & assetbundle", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Cleaning...", true, false);
                            if (File.Exists(PathMgr.Temp(fileName))) File.Delete(PathMgr.Temp(fileName));
                            if (Directory.Exists(PathMgr.Lua(fileName).Replace($"\\{DirName}", "")))
                                Utils.Rmdir(PathMgr.Lua(fileName).Replace($"\\{DirName}", ""));
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during cleaning", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Rewriting Lua...", true, false);
                            Utils.Command("Azrewriter.exe");
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during rewriting Lua", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Recompiling Lua...", true, false);
                            var tasks = new List<Task>();
                            foreach (var mod in ListOfMod)
                                if (mod.Value)
                                {
                                    var modName = ($"scripts{Arch}-" + mod.Key).ToLower().Replace("_", "-");
                                    foreach (var lua in ListOfLua)
                                        tasks.Add(Task.Factory.StartNew(() =>
                                        {
                                            if (os == "android")
                                            {
                                                Utils.Command(
                                                    $"Azcli{LuaArch}.exe --dev --recompile \"{PathMgr.Lua(modName, lua)}\"");
                                            }
                                        }));
                                }

                            Task.WaitAll(tasks.ToArray());
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during recompiling Lua", e);
                        }
                    },
/*                     () =>
                    {
                        try {
                            var showDoneMessage = true;
                            Utils.LogInfo("Encrypting Lua...", true, false);
                            foreach (var mod in ListOfMod)
                            {
                                if (mod.Value)
                                {
                                    var modName = ($"scripts{Arch}-" + mod.Key).ToLower().Replace("_", "-");

                                    foreach (var lua in ListOfLua) {
                                        Utils.Command($"Azcli{LuaArch}.exe --dev --lock \"{PathMgr.Lua(modName, lua)}\"");

                                        if (LuaMgr.CheckLuaState(PathMgr.Lua(modName, lua)) != LuaMgr.State.Decrypted)
                                            break;

                                        Console.WriteLine();
                                        Utils.LogDebug($"Failed to encrypt {mod}/{Path.GetFileName(lua)}...", true, true);
                                        showDoneMessage = false;
                                    }
                                }
                            }
                            if (showDoneMessage)
                                Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during encrypting Lua", e);
                        }
                    }, */
                    DevMode,
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Repacking AssetBundle...", true, false);
                            var tasks = new List<Task>();
                            foreach (var mod in ListOfMod)
                                if (mod.Value)
                                {
                                    var modName = ($"scripts{Arch}-" + mod.Key).ToLower().Replace("_", "-");

                                    tasks.Add(Task.Factory.StartNew(() =>
                                    {
                                        Utils.Command($"Azcli{LuaArch}.exe --dev --repack \"{PathMgr.Temp(modName)}\"");
                                        Utils.Write($@" {index}/{ListOfMod.Count(x => x.Value)}", false, false);
                                        index++;
                                    }));
                                }

                            Task.WaitAll(tasks.ToArray());
                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during repacking AssetBundle", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Encrypting AssetBundle...", true, false);
                            foreach (var mod in ListOfMod)
                                if (mod.Value)
                                {
                                    var modName = ($"scripts{Arch}-" + mod.Key).ToLower().Replace("_", "-");
                                    Utils.Command($"Azcli{LuaArch}.exe --dev --encrypt \"{PathMgr.Temp(modName)}\"");
                                }

                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException("Exception detected during encrypting AssetBundle", e);
                        }
                    },
                    () =>
                    {
                        try
                        {
                            Utils.LogInfo("Copying modified AssetBundle to original location...", true, false);
                            foreach (var mod in ListOfMod)
                                if (mod.Value)
                                {
                                    var modName = ($"scripts{Arch}-" + mod.Key).ToLower().Replace("_", "-");

                                    if (File.Exists(Path.Combine(fileDirectoryPath, modName)))
                                        File.Delete(Path.Combine(fileDirectoryPath, modName));

                                    File.Copy(PathMgr.Temp(modName), Path.Combine(fileDirectoryPath, modName));
                                }

                            Utils.Write(" <done>", false, true);
                        }
                        catch (Exception e)
                        {
                            Utils.Write(" <failed>", false, true);
                            Utils.LogException(
                                "Exception detected during copying modified AssetBundle to original location", e);
                        }
                    }
                };

            try
            {
                foreach (var action in _listOfAction)
                {
                    if (Abort)
                        break;
                    index = 1;

                    action.Invoke();
                }
            }
            finally
            {
                Utils.LogInfo("Cleaning...", true, true);
                Clean(fileName);

                Console.WriteLine();
                Utils.Write("Finished.", true, true);

                var endTime = DateTime.Now;
                var timeSpan = endTime - startTime;
                Utils.Write("Started at {0} - Ended at {1}", true, true, startTime.ToString("HH:mm"),
                    endTime.ToString("HH:mm"));
                Utils.Write("{0} seconds elapsed.", true, true, timeSpan.TotalSeconds.ToString());

                Console.WriteLine(); // Please don't delete this lol
                Utils.Write("Good work (even though you have done nothing at all).", true, true);
            }

            END:
            Utils.Write("Press any key to exit...", true, true);
            Console.ReadKey();
        }

        private static void DevMode()
        {
            if ((bool) ConfigMgr.GetValue(ConfigMgr.Key.DevelopmentMode))
            {
                var text = PathMgr.Temp("dev");
                var text2 = PathMgr.Temp("dev", "raw");
                var path = PathMgr.Local("raw.zip");
                if (Directory.Exists(text)) Utils.Rmdir(text);
                if (File.Exists(path)) File.Delete(path);
                Directory.CreateDirectory(text);
                Directory.CreateDirectory(text2);
                foreach (var keyValuePair in ListOfMod)
                    if (keyValuePair.Value)
                    {
                        var name = $"scripts{Arch}-{keyValuePair.Key.ToString().ToLower().Replace("_", "-")}";
                        var path2 = keyValuePair.Key.ToString().ToLower().Replace("_", "-");
                        Directory.CreateDirectory(Path.Combine(text2, path2));
                        foreach (var text3 in Directory.GetFiles(PathMgr.Assets(name), "*.*",
                            SearchOption.AllDirectories))
                            File.Copy(text3, Path.Combine(Path.Combine(text2, path2), Path.GetFileName(text3)));
                    }

                ZipFile.CreateFromDirectory(text, "raw.zip");
            }
        }


        private static void Message()
        {
            Utils.Write("", true, true);
            Utils.Write("Azurlane Autopatcher", true, true);
            Utils.Write("Version {0}", true, true, ConfigMgr.GetValue(ConfigMgr.Key.Version));
            Utils.Write("{0}", true, true, Resources.Author);
            Utils.Write("", true, true);
        }
    }
}