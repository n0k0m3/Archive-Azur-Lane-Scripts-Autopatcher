using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Azurlane
{
    internal static class SkinMgr
    {
        internal static Configuration Config;
        private const string DataPath = "Skin.ini";
        private static string RewriteRawSkin(this string s)
        {
            return new Regex(@"get_id_list_by_ship_group = {[\s\S]*?(?=\n.*?all)").Replace(s, "get_id_list_by_ship_group = {},");
        }
        internal static string Initialize(Configuration config, string s)
        {
            Config = config;

            var rawShip = LoadRawData("*ship_data_statistics.lua*");
            var rawSkin = LoadRawData("*ship_skin_template.lua*");
            rawSkin = rawSkin.RewriteRawSkin();

            if (rawShip == string.Empty && rawSkin == string.Empty)
                return s;

            GenerateData(rawShip, rawSkin);

            var skinId = LoadData(DataPath).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var id in skinId)
            {
                if (id.Length < 1)
                    break;

                var baseId = id.Remove(id.Length - 1);
                s = new Regex($"(skin_id =) {baseId}.*(,)").Replace(s, $"$1 {id}$2");
            }
            return s;
        }

        private static void GenerateData(string rawShip, string rawSkin)
        {
            Dictionary<string, string> shipData = new Dictionary<string, string>();
            foreach (var data in new Regex(@"\[.*\] = \{[^\[]+\}").Matches(rawShip))
            {
                var shipName = new Regex("english_name = \"(.*)\"").Match(data.ToString()).Result("$1");
                var shipId = new Regex(@"\[(.*)\]").Match(data.ToString()).Result("$1");

                if (!shipData.ContainsKey(shipName))
                    shipData.Add(shipName, shipId);
            }

            Dictionary<string, Dictionary<string, List<string>>> skinData = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (var ship in shipData)
            {
                var shipName = ship.Key;
                var shipId = ship.Value;
                string pattern = @"\["+Regex.Escape(shipId.Remove(shipId.Length - 1))+@".*\] = \{[^\[]+\}";
                foreach (var skin in new Regex(pattern).Matches(rawSkin))
                {
                    var skinName = new Regex("name = \"(.*)\"").Match(skin.ToString()).Result("$1");
                    var skinId = new Regex("\\[(.*)\\]").Match(skin.ToString()).Result("$1");

                    var id = Convert.ToInt32(skinId);

                    if (!skinId.EndsWith("0") && (id < 900000 || id > 904000))
                    {
                        if (!skinData.ContainsKey(shipName))
                        {
                            skinData.Add(shipName, new Dictionary<string, List<string>>());
                            skinData[shipName].Add("skin_name", new List<string>());
                            skinData[shipName].Add("skin_id", new List<string>());
                        }

                        skinData[shipName]["skin_name"].Add(skinName.TranslateName(skinId));
                        skinData[shipName]["skin_id"].Add(skinId);
                    }
                }
            }

            var rawText = string.Empty;
            foreach (var skin in skinData)
            {
                var shipName = skin.Key;
                var skinName = skin.Value["skin_name"];
                var skinId = skin.Value["skin_id"];

                rawText += $"[{shipName}]";
                rawText += Environment.NewLine;

                for (var i = 0; i < skinName.Count; i++)
                {
                    if (i == 0 && !skinName[i].Equals("Wedding") && !skinName[i].Equals("Kai"))
                        rawText += "+";
                    else rawText += "-";

                    rawText += $"{skinName[i]}:{skinId[i]}";
                    rawText += Environment.NewLine;
                }
                rawText += Environment.NewLine;
            }

            if (File.Exists(DataPath))
            {
                if (File.ReadAllText(DataPath).Length < rawText.Length)
                    File.WriteAllText(DataPath, rawText);
            }
            else File.WriteAllText(DataPath, rawText);
        }

        private static string LoadData(string path)
        {
            var result = string.Empty;
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Contains("+"))
                {
                    result += line.Split(':')[1];
                    result += Environment.NewLine;
                }
            }
            return result;
        }

        private static string LoadRawData(string fileName)
        {
            var result = string.Empty;
            foreach (var filePath in Directory.GetFiles(Config.Path.Tmp, fileName, SearchOption.AllDirectories))
            {
                if (result != string.Empty)
                    break;

                result = File.ReadAllText(filePath);
            }

            return result;
        }

        private static string TranslateName(this string skinName, string skinId)
        {
            if (skinId.EndsWith("8")) skinName = "Wedding";
            if (skinId.EndsWith("9")) skinName = "Kai";

            if (skinId.Equals("101251") || skinId.Equals("102122") ||
                skinId.Equals("103091") || skinId.Equals("107031") ||
                skinId.Equals("112011") || skinId.Equals("201101") ||
                skinId.Equals("201212") || skinId.Equals("204031") ||
                skinId.Equals("205041") || skinId.Equals("205061") ||
                skinId.Equals("206041") || skinId.Equals("207021") ||
                skinId.Equals("207061") || skinId.Equals("301151") ||
                skinId.Equals("302131") || skinId.Equals("303112") ||
                skinId.Equals("303121") || skinId.Equals("305021")) skinName = "Summer/Swimsuit";

            if (skinId.Equals("101172") || skinId.Equals("301141") ||
                skinId.Equals("301331") || skinId.Equals("303122")) skinName = "New Year";

            if (skinId.Equals("101061") || skinId.Equals("101262") ||
                skinId.Equals("101361") || skinId.Equals("101371") ||
                skinId.Equals("102092") || skinId.Equals("102121") ||
                skinId.Equals("103081") || skinId.Equals("103131") ||
                skinId.Equals("201021") || skinId.Equals("202111") ||
                skinId.Equals("301011") || skinId.Equals("301611") ||
                skinId.Equals("301631") || skinId.Equals("303114") ||
                skinId.Equals("303123") || skinId.Equals("304031") ||
                skinId.Equals("Kirishima"))
                skinName = "School Uniform";

            if (skinId.Equals("101051") || skinId.Equals("101261") ||
                skinId.Equals("102081") || skinId.Equals("107061") ||
                skinId.Equals("201102") || skinId.Equals("301091") ||
                skinId.Equals("301321") || skinId.Equals("305022"))
                skinName = "Xmas";

            if (skinId.Equals("301051") || skinId.Equals("101271") ||
                skinId.Equals("102091") || skinId.Equals("205031") ||
                skinId.Equals("213021") || skinId.Equals("301261") ||
                skinId.Equals("302051"))
                skinName = "Halloween";

            if (skinId.Equals("105141") || skinId.Equals("201011") ||
                skinId.Equals("202031") || skinId.Equals("202152") ||
                skinId.Equals("205011") || skinId.Equals("207032") ||
                skinId.Equals("305023")) skinName = "Orchestra/Party dress";

            if (skinId.Equals("301052")) skinName = "NicoNico";
            if (skinId.Equals("102181")) skinName = "Matsuri";

            if (skinId.Equals("102131") || skinId.Equals("201231") ||
                skinId.Equals("202151") || skinId.Equals("206032") ||
                skinId.Equals("303113") || skinId.Equals("305011"))
                skinName = "Spring/Chinese dress";

            if (skinId.Equals("101311") || skinId.Equals("201232"))
                skinName = "Nurse";

            if (skinId.Equals("103101") || skinId.Equals("301351") ||
                skinId.Equals("301381")) skinName = "Pajama";
            if (skinId.Equals("106011")) skinName = "NEET";
            if (skinId.Equals("202191")) skinName = "Rose of Sharon Scenery";
            if (skinId.Equals("206031")) skinName = "Little Star Songstress";

            if (skinId.Equals("207031")) skinName = "Tea Party Dress";

            if (skinId.Equals("213011")) skinName = "Valentine";
            if (skinId.Equals("301161")) skinName = "Swings-sama";
            if (skinId.Equals("301012")) skinName = "Poster Girl Bucky";

            if (skinId.Equals("301131") || skinId.Equals("301111") ||
                skinId.Equals("301121")) skinName = "Maid Skin";

            return skinName;
        }
    }
}