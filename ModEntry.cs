using DXVision;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TABModLoader;
using TABModLoader.Utils;

namespace TABHelperMod
{
    public class ModEntry : ModBase
    {
        internal static Harmony harmonyInstance = new Harmony("com.example.tabhelpermod");
        public override void OnLoad(ModInfos modInfos)
        {
            try
            {
                if (modInfos.SteamID > 0)
                    GamePatch.ini = new EasySharpIni.IniFile("Mods/" + modInfos.SteamID + "/TABHelperMod.ini").Parse();
                else
                    GamePatch.ini = new EasySharpIni.IniFile("Mods/TAB Helper/TABHelperMod.ini").Parse();
                //Key Listener
                Type type = AccessToolsEX.TypeByNameWithDecrypt("DXVision.DXGame");
                MethodInfo originalMethod = AccessTools.Method(type, "ProcessKeyUp", new Type[] { typeof(DXKeys) });
                HarmonyMethod postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnKeyUp)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod);
                //DisplayAllLifeMeters
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CLife");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "get_DisplayAllLifeMeters");
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnGetDisplayAllLifeMeters)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod);
                //FastAttack
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommand");
                var ZXEntityType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.ZXEntity");
                var ZXCommandTargetType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "PerformEffect", new Type[] { ZXEntityType, ZXCommandTargetType });
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnPerformAttack)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod);

                //Clear ThreadID
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_StartScreen");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "ShowScene");
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnShowStartGameMenu)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod);

                //Training Order Correction
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "_CommandsManager_OnOptionActivated", new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXControlCommand") });
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnOptionActivated)));
                harmonyInstance.Patch(originalMethod, prefix: postfixMethod);


                //GameMenuEnhancer
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "ShowGameMenu");
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnShowInGameMenu)));
                harmonyInstance.Patch(originalMethod, prefix: postfixMethod);

                //Cancel Research Anytime
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXCampaignState");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "get_IDResearchsRecentUnlocked");
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnGetIDResearchsRecentUnlocked)));
                harmonyInstance.Patch(originalMethod, prefix: postfixMethod);

                //Attack Priority Correction
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXEntityDefaultParams");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "get_Power");
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnGetPower)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod);

                //Attack Priority Assistance
                type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CEntityWatcher");
                originalMethod = AccessToolsEX.MethodWithDecrypt(type, "UpdateNearestEnemy", new Type[] { typeof(bool) });
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnUpdateNearestEnemy)));
                var prefixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnUpdateNearestEnemyPrefix)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod, prefix: prefixMethod);


                //Game Speed Change
                type = AccessToolsEX.TypeByNameWithDecrypt("DXVision.DXGame");
                originalMethod = AccessTools.Method(type, "set_Paused", new Type[] { typeof(bool) });
                postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnSetPaused)));
                harmonyInstance.Patch(originalMethod, prefix: postfixMethod);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }
}
