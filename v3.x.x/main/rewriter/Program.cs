using System.IO;
using System.Text.RegularExpressions;
using Azurlane.IniFileParser;

namespace Azurlane
{
    internal static class Program
    {
        private static bool GetBool(this Configuration config, string section, string key)
        {
            return config.Ini[section][key].ToLower() == "true";
        }

        private static string GetString(this Configuration config, string section, string key)
        {
            var value = config.Ini[section][key];
            return value.ToLower() == "false" || value.ToLower() == "ignore" ? "ignore" : value;
        }

        private static bool IsIgnore(this string s)
        {
            return s == "ignore";
        }

        private static void Load(this Configuration config, string configPath)
        {
            config.Ini = new FileIniDataParser().ReadFile(configPath);
            config.Parse();
        }

        private static void Main(string[] args)
        {
            var config = new Configuration();
            config.Load("Configuration.ini");

            foreach (var filePath in Directory.GetFiles(config.Path.Tmp, "*.lua*", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(filePath);

                if (filePath.Contains("weapon_property"))
                {
                    if (filePath.Contains("damage") && !config.Weapon.Damage.IsIgnore())
                        content = content.RewriteAttribute("damage", config.Weapon.Damage);

                    if (filePath.Contains("cooldown") && !config.Weapon.Damage.IsIgnore())
                        content = content.RewriteAttribute("reload_max", config.Weapon.ReloadMax);
                }

                if (filePath.Contains("enemy_data_statistics"))
                {
                    if (filePath.Contains("godmode"))
                        content = content.RewriteGroup("equipment_list", null);

                    if (filePath.Contains("weakenemy"))
                    {
                        if (!config.Enemy.Hp.IsIgnore())
                            content = content.RewriteAttribute("durability", config.Enemy.Hp);

                        if (!config.Enemy.HpGrowth.IsIgnore())
                            content = content.RewriteAttribute("durability_growth", config.Enemy.HpGrowth);
                    }

                    if (!config.Enemy.AntiAir.IsIgnore())
                        content = content.RewriteAttribute("antiaircraft", config.Enemy.AntiAir);

                    if (!config.Enemy.AntiAirGrowth.IsIgnore())
                        content = content.RewriteAttribute("antiaircraft_growth", config.Enemy.AntiAirGrowth);

                    if (!config.Enemy.AntiSubmarine.IsIgnore())
                        content = content.RewriteAttribute("antisub", config.Enemy.AntiSubmarine);

                    if (!config.Enemy.Armor.IsIgnore())
                        content = content.RewriteAttribute("armor", config.Enemy.Armor);

                    if (!config.Enemy.ArmorGrowth.IsIgnore())
                        content = content.RewriteAttribute("ArmorGrowth", config.Enemy.Armor);

                    if (!config.Enemy.Cannon.IsIgnore())
                        content = content.RewriteAttribute("cannon", config.Enemy.Cannon);

                    if (!config.Enemy.CannonGrowth.IsIgnore())
                        content = content.RewriteAttribute("cannon_growth", config.Enemy.CannonGrowth);

                    if (!config.Enemy.Evasion.IsIgnore())
                        content = content.RewriteAttribute("dodge", config.Enemy.Evasion);

                    if (!config.Enemy.EvasionGrowth.IsIgnore())
                        content = content.RewriteAttribute("dodge_growth", config.Enemy.EvasionGrowth);

                    if (!config.Enemy.Hit.IsIgnore())
                        content = content.RewriteAttribute("hit", config.Enemy.Hit);

                    if (!config.Enemy.HitGrowth.IsIgnore())
                        content = content.RewriteAttribute("hit_growth", config.Enemy.HitGrowth);

                    if (!config.Enemy.Luck.IsIgnore())
                        content = content.RewriteAttribute("luck", config.Enemy.Luck);

                    if (!config.Enemy.LuckGrowth.IsIgnore())
                        content = content.RewriteAttribute("luck_growth", config.Enemy.LuckGrowth);

                    if (!config.Enemy.Reload.IsIgnore())
                        content = content.RewriteAttribute("reload", config.Enemy.Reload);

                    if (!config.Enemy.ReloadGrowth.IsIgnore())
                        content = content.RewriteAttribute("reload_growth", config.Enemy.ReloadGrowth);

                    if (!config.Enemy.Speed.IsIgnore())
                        content = content.RewriteAttribute("speed", config.Enemy.Speed);

                    if (!config.Enemy.SpeedGrowth.IsIgnore())
                        content = content.RewriteAttribute("speed_growth", config.Enemy.SpeedGrowth);

                    if (!config.Enemy.Torpedo.IsIgnore())
                        content = content.RewriteAttribute("torpedo", config.Enemy.Torpedo);

                    if (!config.Enemy.TorpedoGrowth.IsIgnore())
                        content = content.RewriteAttribute("torpedo_growth", config.Enemy.TorpedoGrowth);
                }

                if (filePath.Contains("aircraft_template") && filePath.Contains("godmode"))
                {
                    if (!config.Aircraft.Hp.IsIgnore())
                        content = content.RewriteAttribute("max_hp", config.Aircraft.Hp);

                    if (!config.Aircraft.HpGrowth.IsIgnore())
                        content = content.RewriteAttribute("hp_growth", config.Aircraft.HpGrowth);

                    if (!config.Aircraft.Accuracy.IsIgnore())
                        content = content.RewriteAttribute("accuracy", config.Aircraft.Accuracy);

                    if (!config.Aircraft.AccuracyGrowth.IsIgnore())
                        content = content.RewriteAttribute("ACC_growth", config.Aircraft.AccuracyGrowth);

                    if (!config.Aircraft.AttackPower.IsIgnore())
                        content = content.RewriteAttribute("attack_power", config.Aircraft.AttackPower);

                    if (!config.Aircraft.AttackPowerGrowth.IsIgnore())
                        content = content.RewriteAttribute("AP_growth", config.Aircraft.AttackPowerGrowth);

                    if (!config.Aircraft.CrashDamage.IsIgnore())
                        content = content.RewriteAttribute("crash_DMG", config.Aircraft.CrashDamage);

                    if (!config.Aircraft.Speed.IsIgnore())
                        content = content.RewriteAttribute("speed", config.Aircraft.Speed);
                }

                if (filePath.Contains("enemy_data_skill") && config.Enemy.RemoveSkill)
                {
                    content = content.RewriteAttribute("is_repeat", "0");
                    content = content.RewriteGroup("skill_list", null);
                }

                if (filePath.Contains("ship_data_statistics") && config.Other.ReplaceSkin)
                    content = SkinMgr.Initialize(config, content);

                if (filePath.Contains("chapter_template") && config.Other.EasyMode)
                {
                    content = content.RewriteAttribute("investigation_ratio", "0");
                    content = content.RewriteLargeGroup("limitation",
                        "\n\t\t\t{\n\t\t\t\t{\n\t\t\t\t\t0,\n\t\t\t\t\t0,\n\t\t\t\t\t0\n\t\t\t\t},\n\t\t\t\t{\n\t\t\t\t\t0,\n\t\t\t\t\t0,\n\t\t\t\t\t0\n\t\t\t\t}\n\t\t\t},\n\t\t\t{\n\t\t\t\t{\n\t\t\t\t\t0,\n\t\t\t\t\t0,\n\t\t\t\t\t0\n\t\t\t\t},\n\t\t\t\t{\n\t\t\t\t\t0,\n\t\t\t\t\t0,\n\t\t\t\t\t0\n\t\t\t\t}\n\t\t\t}\n\t\t");
                    content = content.RewriteLargeGroup("property_limitation", null);
                    content = content.RewriteLargeGroup("ambush_expedition_list", null);
                    content = content.RewriteLargeGroup("ambush_event_ratio", null);
                    content = content.RewriteLargeGroup("ambush_ratio_extra",
                        "\n\t\t\t{\n\t\t\t\t-20000\n\t\t\t}\n\t\t");
                }

                File.WriteAllText(filePath, content);
            }
        }

        private static void Parse(this Configuration config)
        {
            // [Common]
            config.Common.Version = config.GetString("Common", "Version");

            // [Path]
            config.Path.Tmp = config.GetString("Path", "Tmp");
            config.Path.Thirdparty = config.GetString("Path", "Thirdparty");

            // [Mods]
            config.Mods.GodMode = config.GetBool("Mods", "GodMode");
            config.Mods.WeakEnemy = config.GetBool("Mods", "WeakEnemy");
            config.Mods.GodModeDamage = config.GetBool("Mods", "GodMode+Damage");
            config.Mods.GodModeCooldown = config.GetBool("Mods", "GodMode+Cooldown");
            config.Mods.GodModeWeakEnemy = config.GetBool("Mods", "GodMode+WeakEnemy");
            config.Mods.GodModeDamageCooldown = config.GetBool("Mods", "GodMode+Damage+Cooldown");
            config.Mods.GodModeDamageWeakEnemy = config.GetBool("Mods", "GodMode+Damage+WeakEnemy");
            config.Mods.GodModeDamageCooldownWeakEnemy = config.GetBool("Mods", "GodMode+Damage+Cooldown+WeakEnemy");

            // [Aircraft]
            config.Aircraft.Hp = config.GetString("Aircraft", "Hp");
            config.Aircraft.HpGrowth = config.GetString("Aircraft", "HpGrowth");
            config.Aircraft.Accuracy = config.GetString("Aircraft", "Accuracy");
            config.Aircraft.AccuracyGrowth = config.GetString("Aircraft", "AccuracyGrowth");
            config.Aircraft.AttackPower = config.GetString("Aircraft", "AttackPower");
            config.Aircraft.AttackPowerGrowth = config.GetString("Aircraft", "AttackPowerGrowth");
            config.Aircraft.CrashDamage = config.GetString("Aircraft", "CrashDamage");
            config.Aircraft.Speed = config.GetString("Aircraft", "Speed");

            // [Weapon]
            config.Weapon.Damage = config.GetString("Weapon", "Damage");
            config.Weapon.ReloadMax = config.GetString("Weapon", "ReloadMax");

            // [Enemy]
            config.Enemy.AntiAir = config.GetString("Enemy", "AntiAir");
            config.Enemy.AntiAirGrowth = config.GetString("Enemy", "AntiAirGrowth");
            config.Enemy.AntiSubmarine = config.GetString("Enemy", "AntiSubmarine");
            config.Enemy.Armor = config.GetString("Enemy", "Armor");
            config.Enemy.ArmorGrowth = config.GetString("Enemy", "ArmorGrowth");
            config.Enemy.Cannon = config.GetString("Enemy", "Cannon");
            config.Enemy.CannonGrowth = config.GetString("Enemy", "CannonGrowth");
            config.Enemy.Evasion = config.GetString("Enemy", "Evasion");
            config.Enemy.EvasionGrowth = config.GetString("Enemy", "EvasionGrowth");
            config.Enemy.Hit = config.GetString("Enemy", "Hit");
            config.Enemy.HitGrowth = config.GetString("Enemy", "HitGrowth");
            config.Enemy.Hp = config.GetString("Enemy", "Hp");
            config.Enemy.HpGrowth = config.GetString("Enemy", "HpGrowth");
            config.Enemy.Luck = config.GetString("Enemy", "Luck");
            config.Enemy.LuckGrowth = config.GetString("Enemy", "LuckGrowth");
            config.Enemy.Reload = config.GetString("Enemy", "Reload");
            config.Enemy.ReloadGrowth = config.GetString("Enemy", "ReloadGrowth");
            config.Enemy.Speed = config.GetString("Enemy", "Speed");
            config.Enemy.SpeedGrowth = config.GetString("Enemy", "SpeedGrowth");
            config.Enemy.Torpedo = config.GetString("Enemy", "Torpedo");
            config.Enemy.TorpedoGrowth = config.GetString("Enemy", "TorpedoGrowth");
            config.Enemy.RemoveSkill = config.GetBool("Enemy", "RemoveSkill");

            // [Other]
            config.Other.ReplaceSkin = config.GetBool("Other", "ReplaceSkin");
            config.Other.EasyMode = config.GetBool("Other", "EasyMode");
        }

        private static string RewriteAttribute(this string s, string pattern, string replacement)
        {
            return new Regex($"{pattern} = .*(,)").Replace(s, $"{pattern} = {replacement}$1");
        }

        private static string RewriteGroup(this string s, string pattern, string replacement)
        {
            return new Regex($"({pattern} = \\{{)[^\\}}]+(\\}})").Replace(s, $"$1{replacement}$2");
        }

        private static string RewriteLargeGroup(this string s, string pattern, string replacement)
        {
            return new Regex(@"((?<!\w)" + pattern + @" = {)[\s\S]*?(},(?=\s+?[a-z]))")
                .Replace(s, $"$1{replacement}$2");
        }

        private static string RewritePLimit(this string s)
        {
            return new Regex(@"property_limitation = {[\s\S]*?(?=\n.*?expedition)").Replace(s,
                "property_limitation = {},");
        }
    }
}