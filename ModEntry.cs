using DXVision;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TABModLoader;
using TABModLoader.Utils;
using static TABHelperMod.GamePatch;

namespace TABHelperMod
{
    public class ModEntry : ModBase
    {
        internal static Harmony harmonyInstance = new Harmony("com.example.tabhelpermod");
        public override void OnLoad(ModInfos modInfos)
        {
            try
            {
                ModOptions.Instance.Load(modInfos);

                //Key Listener
                Type type = AccessToolsEX.TypeByNameWithDecrypt("DXVision.DXGame");
                MethodInfo originalMethod = AccessTools.Method(type, "ProcessKeyUp", new Type[] { typeof(DXKeys) });
                HarmonyMethod postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnKeyUp)));
                harmonyInstance.Patch(originalMethod, postfix: postfixMethod);

                if (ModOptions.Instance.KeepDisplayAllLifeMeters)
                {
                    //DisplayAllLifeMeters
                    //type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CLife");
                    //originalMethod = AccessToolsEX.MethodWithDecrypt(type, "get_DisplayAllLifeMeters");
                    //postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnGetDisplayAllLifeMeters)));
                    //harmonyInstance.Patch(originalMethod, postfix: postfixMethod);

                    //DisplayAllLifeMeters
                    //System.Void ZX.Components.CLife::OnUpdateSceneObjectParallel()
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CLife");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "OnUpdateSceneObjectParallel");
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnUpdateSceneObjectParallel)));
                    harmonyInstance.Patch(originalMethod, transpiler: postfixMethod);
                }

                if (ModOptions.Instance.FastAttack)
                {
                    //FastAttack
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommand");
                    var ZXEntityType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.ZXEntity");
                    var ZXCommandTargetType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "PerformEffect", new Type[] { ZXEntityType, ZXCommandTargetType });
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnPerformAttack)));
                    harmonyInstance.Patch(originalMethod, postfix: postfixMethod);
                }

                if (ModOptions.Instance.OptimizeTrainingSequence)
                {
                    //Training Order Correction
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "_CommandsManager_OnOptionActivated", new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXControlCommand") });
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnOptionActivated)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);
                }

                if (ModOptions.Instance.GameMenuEnhancer)
                {
                    //GameMenuEnhancer
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "ShowGameMenu");
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnShowInGameMenu)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);

                    //GamePatch.DeleteGameSavePatch();
                    if (ModOptions.Instance.AutoDeleteBackups)
                    {
                        type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGame");
                        originalMethod = AccessToolsEX.MethodWithDecrypt(type, "DeleteSaveGames", new Type[] { typeof(string) });
                        var transpilerMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.DeleteSaveGamesTranspiler)));
                        postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnDeleteSaveGames)));
                        harmonyInstance.Patch(originalMethod, postfix: postfixMethod, transpiler: transpilerMethod);
                    }
                }

                if (ModOptions.Instance.CancelResearchAnytime)
                {
                    //Cancel Research Anytime
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXCampaignState");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "get_IDResearchsRecentUnlocked");
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnGetIDResearchsRecentUnlocked)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);

                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXCampaignState");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "get_HeroPerksTakenNow");
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnGetHeroPerksTakenNow)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);
                }

                if (ModOptions.Instance.OptimizeAttackPriority)
                {
                    //Attack Priority Assistance
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CEntityWatcher");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "UpdateNearestEnemy", new Type[] { typeof(bool) });
                    var transpilerMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnUpdateNearestEnemyTranspiler)));
                    harmonyInstance.Patch(originalMethod, transpiler: transpilerMethod);
                }



                if (ModOptions.Instance.GameSpeedChange)
                {
                    //Game Speed Change
                    type = AccessToolsEX.TypeByNameWithDecrypt("DXVision.DXGame");
                    originalMethod = AccessTools.Method(type, "set_Paused", new Type[] { typeof(bool) });
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnSetPaused)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);
                }

                if (ModOptions.Instance.DisableAutoSave)
                {
                    //Disable Auto-Save
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "SaveBackup");
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnSaveBackup)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);
                }

                if (!ModOptions.Instance.DisableAutoSave)
                {
                    //change Auto-Save interval
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "CheckAutoBackup");
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnCheckAutoBackup)));
                    harmonyInstance.Patch(originalMethod, transpiler: postfixMethod);
                }

                if (ModOptions.Instance.EnhancedSelection)
                {
                    //System.Void ZX.ZXSystem_GameLevel::_OWorld_MouseDragFinish(DXVision.DXObject,System.Drawing.Point)
                    type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                    originalMethod = AccessToolsEX.MethodWithDecrypt(type, "_OWorld_MouseDragFinish", new Type[] { typeof(DXObject), typeof(System.Drawing.Point) });
                    postfixMethod = new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(GamePatch.OnCorrectSelection)));
                    harmonyInstance.Patch(originalMethod, prefix: postfixMethod);
                }

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }
}
