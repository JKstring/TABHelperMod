using Fasterflect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TABModLoader;

namespace TABHelperMod
{
    internal class ModOptions
    {
        public enum FuncConfigList
        {
            KeepDisplayAllLifeMeters,
            AutoGoWatchTower,
            AutoGoWatchTowerRadius,
            NumpadFilterVeteran,
            GameSpeedChange,
            FilterVeteranUnit,
            AutoFindBonusItem,
            AutoDisperse,
            FastAttack,
            OptimizeTrainingSequence,
            DestroyAllSelectedUnits,
            GameMenuEnhancer,
            OptimizeAttackPriority,
            CancelResearchAnytime,
            DisableAutoSave,
            AutoSaveInterval,
            MaxSaveBackup,
            AutoDeleteBackups,
            EnhancedSelection
        }
        public bool KeepDisplayAllLifeMeters { get; set; } = true;
        public bool AutoGoWatchTower { get; set; } = true;
        public int AutoGoWatchTowerRadius { get; set; } = 10;
        public bool NumpadFilterVeteran { get; set; } = true;
        public bool GameSpeedChange { get; set; } = true;
        public bool FilterVeteranUnit { get; set; } = true;
        public bool AutoFindBonusItem { get; set; } = true;
        public bool AutoDisperse { get; set; } = false;
        public bool FastAttack { get; set; } = false;
        public bool OptimizeTrainingSequence { get; set; } = true;
        public bool DestroyAllSelectedUnits { get; set; } = true;
        public bool GameMenuEnhancer { get; set; } = true;
        public bool OptimizeAttackPriority { get; set; } = false;
        public bool CancelResearchAnytime { get; set; } = false;
        public bool DisableAutoSave { get; set; } = false;
        public int AutoSaveInterval { get; set; } = 1200;
        public int MaxSaveBackup { get; set; } = 5;
        public bool AutoDeleteBackups { get; set; } = true;
        public bool EnhancedSelection { get; set; } = true;

        public static ModOptions Instance { get; } = new ModOptions();
        private bool IsLoaded = false;

        public void Load(ModInfos modInfos)
        {
            if (IsLoaded)
                return;
            IsLoaded = true;
            EasySharpIni.IniFile ini;
            //判断路径和文件是否存在,不存在则创建
            string path;
            if (modInfos.SteamID > 0)
                path = "Mods/ModData/" + modInfos.SteamID + "/TABHelperMod.ini";
            else
                path = "Mods/ModData/TAB Helper/TABHelperMod.ini";
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }

            ini = new EasySharpIni.IniFile(path);
            ini.AddField("KeepDisplayAllLifeMeters", "true", "true");
            ini.AddField("AutoGoWatchTower", "true", "true");
            ini.AddField("AutoGoWatchTowerRadius", "10", "10");
            ini.AddField("NumpadFilterVeteran", "true", "true");
            ini.AddField("GameSpeedChange", "true", "true");
            ini.AddField("FilterVeteranUnit", "true", "true");
            ini.AddField("AutoFindBonusItem", "true", "true");
            ini.AddField("AutoDisperse", "false", "false");
            ini.AddField("FastAttack", "false", "false");
            ini.AddField("OptimizeTrainingSequence", "true", "true");
            ini.AddField("DestroyAllSelectedUnits", "true", "true");
            ini.AddField("GameMenuEnhancer", "true", "true");
            ini.AddField("OptimizeAttackPriority", "false", "false");
            ini.AddField("CancelResearchAnytime", "false", "false");
            ini.AddField("DisableAutoSave", "false", "false");
            ini.AddField("AutoSaveInterval", "1200", "1200");
            ini.AddField("MaxSaveBackup", "5", "5");
            ini.AddField("AutoDeleteBackups", "true", "true");
            ini.AddField("EnhancedSelection", "true", "true");
            if (!System.IO.File.Exists(path))
            {
                ini.Write();
            }
            else
            {
                var ini2 = new EasySharpIni.IniFile(path).Parse();
                foreach (var fieldName in Enum.GetNames(typeof(FuncConfigList)))
                {
                    if (ini2.GetField(fieldName) == "")
                    {
                        ini2.AddField(fieldName, ini.GetField(fieldName).Get(), ini.GetField(fieldName).Get());
                    }
                }
                ini2.Write();
                ini = new EasySharpIni.IniFile(path).Parse();
            }

            KeepDisplayAllLifeMeters = bool.Parse(ini.GetField("KeepDisplayAllLifeMeters", "true").Get());
            AutoGoWatchTower = bool.Parse(ini.GetField("AutoGoWatchTower", "true").Get());
            AutoGoWatchTowerRadius = int.Parse(ini.GetField("AutoGoWatchTowerRadius", "10").Get());
            NumpadFilterVeteran = bool.Parse(ini.GetField("NumpadFilterVeteran", "true").Get());
            GameSpeedChange = bool.Parse(ini.GetField("GameSpeedChange", "true").Get());
            FilterVeteranUnit = bool.Parse(ini.GetField("FilterVeteranUnit", "true").Get());
            AutoFindBonusItem = bool.Parse(ini.GetField("AutoFindBonusItem", "true").Get());
            AutoDisperse = bool.Parse(ini.GetField("AutoDisperse", "false").Get());
            FastAttack = bool.Parse(ini.GetField("FastAttack", "false").Get());
            OptimizeTrainingSequence = bool.Parse(ini.GetField("OptimizeTrainingSequence", "true").Get());
            DestroyAllSelectedUnits = bool.Parse(ini.GetField("DestroyAllSelectedUnits", "true").Get());
            GameMenuEnhancer = bool.Parse(ini.GetField("GameMenuEnhancer", "true").Get());
            OptimizeAttackPriority = bool.Parse(ini.GetField("OptimizeAttackPriority", "false").Get());
            CancelResearchAnytime = bool.Parse(ini.GetField("CancelResearchAnytime", "false").Get());
            DisableAutoSave = bool.Parse(ini.GetField("DisableAutoSave", "false").Get());
            AutoSaveInterval = int.Parse(ini.GetField("AutoSaveInterval", "1200").Get());
            MaxSaveBackup = int.Parse(ini.GetField("MaxSaveBackup", "5").Get());
            AutoDeleteBackups = bool.Parse(ini.GetField("AutoDeleteBackups", "true").Get());
            EnhancedSelection = bool.Parse(ini.GetField("EnhancedSelection", "true").Get());
        }
    }
}
