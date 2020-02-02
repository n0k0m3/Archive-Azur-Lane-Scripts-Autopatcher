using Azurlane.IniFileParser.Model;

namespace Azurlane
{
    internal class Configuration
    {
        internal AAircraft Aircraft;
        internal CCommon Common;
        internal EEnemy Enemy;
        internal MMods Mods;
        internal OOther Other;
        internal PPath Path;
        internal WWeapon Weapon;

        internal Configuration()
        {
            if (Common == null)
                Common = new CCommon();

            if (Path == null)
                Path = new PPath();

            if (Mods == null)
                Mods = new MMods();

            if (Aircraft == null)
                Aircraft = new AAircraft();

            if (Weapon == null)
                Weapon = new WWeapon();

            if (Enemy == null)
                Enemy = new EEnemy();

            if (Other == null)
                Other = new OOther();
        }

        internal IniData Ini { get; set; }

        internal class AAircraft
        {
            internal string Accuracy { get; set; }
            internal string AccuracyGrowth { get; set; }
            internal string AttackPower { get; set; }
            internal string AttackPowerGrowth { get; set; }
            internal string CrashDamage { get; set; }
            internal string Hp { get; set; }
            internal string HpGrowth { get; set; }
            internal string Speed { get; set; }
        }

        internal class CCommon
        {
            internal string Version { get; set; }
        }

        internal class EEnemy
        {
            internal string AntiAir { get; set; }
            internal string AntiAirGrowth { get; set; }
            internal string AntiSubmarine { get; set; }
            internal string Armor { get; set; }
            internal string ArmorGrowth { get; set; }
            internal string Cannon { get; set; }
            internal string CannonGrowth { get; set; }
            internal string Evasion { get; set; }
            internal string EvasionGrowth { get; set; }
            internal string Hit { get; set; }
            internal string HitGrowth { get; set; }
            internal string Hp { get; set; }
            internal string HpGrowth { get; set; }
            internal string Luck { get; set; }
            internal string LuckGrowth { get; set; }
            internal string Reload { get; set; }
            internal string ReloadGrowth { get; set; }
            internal bool RemoveSkill { get; set; }
            internal string Speed { get; set; }
            internal string SpeedGrowth { get; set; }
            internal string Torpedo { get; set; }
            internal string TorpedoGrowth { get; set; }
        }

        internal class MMods
        {
            internal bool GodMode { get; set; }
            internal bool GodModeCooldown { get; set; }
            internal bool GodModeDamage { get; set; }
            internal bool GodModeDamageCooldown { get; set; }
            internal bool GodModeDamageCooldownWeakEnemy { get; set; }
            internal bool GodModeDamageWeakEnemy { get; set; }
            internal bool GodModeWeakEnemy { get; set; }
            internal bool WeakEnemy { get; set; }
        }

        internal class OOther
        {
            internal bool ReplaceSkin { get; set; }
            internal bool EasyMode { get; set; }
        }

        internal class PPath
        {
            internal string Thirdparty { get; set; }
            internal string Tmp { get; set; }
        }

        internal class WWeapon
        {
            internal string Damage { get; set; }
            internal string ReloadMax { get; set; }
        }
    }
}