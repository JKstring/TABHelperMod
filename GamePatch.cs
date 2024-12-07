using DXVision;
using DXVision.GUI;
using EasySharpIni.Converters;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TABModLoader;
using TABModLoader.Utils;
using Fasterflect;
using static TABModLoader.Utils.Decryptor;
using System.Reflection.Emit;
using ZX.Entities;

namespace TABHelperMod
{
    public class GamePatch
    {
        static string[] aviableCommands = new string[] { "ZX.Commands.Hold", "ZX.Commands.Attack", "ZX.Commands.Patrol", "ZX.Commands.Stop", "ZX.Commands.Chase" };
        static Type HumanArmyUnitType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.HumanArmyUnit");
        static Type CInsideBuildingType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CInsideBuilding");
        static Type ZXCommandTargetType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget");
        static Type ZXCommandType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommand");
        static MethodInfo AttackCommandGetMethon = AccessToolsEX.MethodWithDecrypt(AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommand"), "Get", generics: new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.Attack") });
        internal static void OnPerformAttack(Object __instance, MethodBase __originalMethod, object[] __args)
        {
            try
            {
                var actor = __args[0];
                var target = __args[1];
                bool isHumanArmyUnit = actor.GetType().IsSubclassOf(HumanArmyUnitType);
                if (!isHumanArmyUnit)
                    return;
                //System.Threading.Tasks.Task.Run(() =>
                //{
                //Check if inside the building, if inside the building, do not execute the reset command
                //var HasComponent = AccessToolsEX.MethodWithDecrypt(actor.GetType(), "HasComponent", null, new Type[] { CInsideBuildingType });
                //bool isInBuilding = (bool)HasComponent.Invoke(actor, null);
                //if (isInBuilding)
                //    return;
                bool isInBuilding = (bool)actor.CallMethod(new Type[] { CInsideBuildingType }, D("HasComponent"));
                if (isInBuilding)
                    return;
                var CCommandable = actor.CallMethod(D("get_CCommandable"));
                //var CCommandable = Traverse.Create(actor).MethodWithDecrypt("get_CCommandable").GetValue();
                //var CurrentCommand = Traverse.Create(CCommandable).MethodWithDecrypt("get_Command").GetValue();
                var CurrentCommand = CCommandable.CallMethod(D("get_Command"));
                Type CurrentCommandType = CurrentCommand != null ? CurrentCommand.GetType() : null;

                if (CurrentCommandType != null && !aviableCommands.Contains(CurrentCommandType.FullName))
                {
                    return;
                }

                if (CurrentCommandType != null && CurrentCommandType.FullName == "ZX.Commands.Hold")
                {
                    var CanStop = Traverse.Create(actor).MethodWithDecrypt("get_Params").Property<bool>("CanStop").Value;
                    if (CanStop)
                    {


                        var Position = ((DXEntity)actor).Position;
                        var ZXCommandTarget = AccessToolsEX.CreateInstance(AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget"), new object[] { Position });


                        //Traverse.Create(actor).MethodWithDecrypt("get_CCommandable").PropertyWithDecrypt("Target").SetValue(ZXCommandTarget);
                        CCommandable.SetPropertyValue(D("Target"), ZXCommandTarget);

                        var AttackCommand = AttackCommandGetMethon.Invoke(null, null);
                        //Traverse.Create(actor).MethodWithDecrypt("get_CCommandable").MethodWithDecrypt("set_Command", new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommand") }).GetValue(AttackCommand);
                        CCommandable.CallMethod(D("set_Command"), AttackCommand);
                        //Traverse.Create(AttackCommand).MethodWithDecrypt("Execute", new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.ZXEntity"), AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget") }).GetValue(actor, ZXCommandTarget);
                        AttackCommand.CallMethod(D("Execute"), actor, target);

                        //Traverse.Create(CCommandable).MethodWithDecrypt("CancellAllCommandsQueued").GetValue();
                        CCommandable.CallMethod(D("CancellAllCommandsQueued"));
                        //var OrdersQueued = Traverse.Create(CCommandable).PropertyWithDecrypt("OrdersQueued").GetValue();
                        var OrdersQueued = CCommandable.GetPropertyValue(D("OrdersQueued"));
                        if (OrdersQueued == null)
                        {
                            var listType = typeof(List<>).MakeGenericType(AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXOrder"));
                            var list = Activator.CreateInstance(listType);
                            //Traverse.Create(CCommandable).PropertyWithDecrypt("OrdersQueued").SetValue(list);
                            CCommandable.SetPropertyValue(D("OrdersQueued"), list);
                        }
                        //var count = Traverse.Create(CCommandable).PropertyWithDecrypt("OrdersQueued").Property("Count").GetValue<int>();
                        OrdersQueued = CCommandable.GetPropertyValue(D("OrdersQueued"));
                        var count = (int)OrdersQueued.GetPropertyValue("Count");
                        if (count == 0)
                        {
                            var hold = AccessToolsEX.MethodWithDecrypt(AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommand"), "Get", generics: new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.Hold") });
                            var HoldCommand = hold.Invoke(null, null);
                            //OrdersQueued = Traverse.Create(CCommandable).PropertyWithDecrypt("OrdersQueued").GetValue();
                            //Traverse.Create(CCommandable).MethodWithDecrypt("EnqueueCommand", new Type[] { ZXCommandType, ZXCommandTargetType }).GetValue(HoldCommand, target);
                            CCommandable.CallMethod(D("EnqueueCommand"), HoldCommand, target);
                        }

                        return;
                    }
                }
                var CBehaviour = actor.CallMethod(D("get_CBehaviour"));
                if (CBehaviour != null)
                {
                    CBehaviour.CallMethod(D("set_PerformResetMemNodes"), new object[] { true });
                }

                //if (Traverse.Create(actor).MethodWithDecrypt("get_CBehaviour").GetValue() != null)
                //{
                //    Traverse.Create(actor).MethodWithDecrypt("get_CBehaviour").MethodWithDecrypt("set_PerformResetMemNodes", new Type[] { typeof(bool) }).GetValue(true);
                //}
                //});
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        enum DisplayAllLifeMetersType
        {
            None,
            All,
            Player,
            Zombie
        }

        static DisplayAllLifeMetersType displayAllLifeMetersType = DisplayAllLifeMetersType.None;

        internal static void OnKeyUp(DXGame __instance, DXKeys key)
        {
            try
            {
                if (key == DXKeys.Y)
                {
                    if (!ModOptions.Instance.KeepDisplayAllLifeMeters)
                    {
                        return;
                    }
                    string message = "";
                    if (displayAllLifeMetersType == DisplayAllLifeMetersType.None)
                    {
                        displayAllLifeMetersType = DisplayAllLifeMetersType.All;
                        message = "Display All Life Meters: All";
                    }
                    else if (displayAllLifeMetersType == DisplayAllLifeMetersType.All)
                    {
                        displayAllLifeMetersType = DisplayAllLifeMetersType.Player;
                        message = "Display All Life Meters: Player";
                    }
                    else if (displayAllLifeMetersType == DisplayAllLifeMetersType.Player)
                    {
                        displayAllLifeMetersType = DisplayAllLifeMetersType.Zombie;
                        message = "Display All Life Meters: Zombie";
                    }
                    else if (displayAllLifeMetersType == DisplayAllLifeMetersType.Zombie)
                    {
                        displayAllLifeMetersType = DisplayAllLifeMetersType.None;
                        message = "Display All Life Meters: Default";
                    }

                    var ZXSystem_GameLevelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                    Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("ShowMessage", new Type[] { typeof(string), typeof(System.Drawing.Color), typeof(int) }).GetValue(message, System.Drawing.Color.White, 2000);
                }
                else if (key == DXKeys.F)
                {
                    if (!ModOptions.Instance.AutoGoWatchTower)
                    {
                        return;
                    }
                    GamePatch.AutoGoWatchTower();
                }
                else if (key >= DXKeys.NumPad1 && key <= DXKeys.NumPad9)
                {
                    if (!ModOptions.Instance.NumpadFilterVeteran)
                    {
                        return;
                    }
                    GamePatch.NumpadFilterVeteran(key);

                }
                else if (key == DXKeys.Oemplus)
                {
                    if (!ModOptions.Instance.GameSpeedChange)
                    {
                        return;
                    }

                    if (!DXGame.Current.Paused)
                    {
                        var type = AccessTools.TypeByName("DXVision.DXGame");
                        var gameSpeed = Traverse.Create(type).Property("Current").Property("GameSpeed").GetValue<double>();
                        gameSpeed += 1;
                        if (DXInputState.Current.IsKeyPressed(DXKeys.Control))
                        {
                            gameSpeed = 20;
                        }
                        Traverse.Create(type).Property("Current").Property("GameSpeed").SetValue(gameSpeed);
                        var ZXSystem_GameLevelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                        Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("ShowMessage", new Type[] { typeof(string), typeof(System.Drawing.Color), typeof(int) }).GetValue("Now Game Speed: " + gameSpeed, System.Drawing.Color.White, 2000);
                    }
                    else
                    {
                        GameSpeed += 1;
                        if (DXInputState.Current.IsKeyPressed(DXKeys.Control))
                        {
                            GameSpeed = 20;
                        }
                        var ZXSystem_GameLevelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                        Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("ShowMessage", new Type[] { typeof(string), typeof(System.Drawing.Color), typeof(int) }).GetValue("Now Game Speed: " + GameSpeed, System.Drawing.Color.White, 2000);
                    }
                }
                else if (key == DXKeys.OemMinus)
                {
                    if (!ModOptions.Instance.GameSpeedChange)
                    {
                        return;
                    }

                    if (!DXGame.Current.Paused)
                    {
                        var type = AccessTools.TypeByName("DXVision.DXGame");
                        var gameSpeed = Traverse.Create(type).Property("Current").Property("GameSpeed").GetValue<double>();
                        gameSpeed -= 1;
                        gameSpeed = Math.Max(gameSpeed, 1);
                        if (DXInputState.Current.IsKeyPressed(DXKeys.Control))
                        {
                            gameSpeed = 1;
                        }
                        Traverse.Create(type).Property("Current").Property("GameSpeed").SetValue(gameSpeed);
                        var ZXSystem_GameLevelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                        Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("ShowMessage", new Type[] { typeof(string), typeof(System.Drawing.Color), typeof(int) }).GetValue("Now Game Speed: " + gameSpeed, System.Drawing.Color.White, 2000);
                    }
                    else
                    {
                        GameSpeed -= 1;
                        GameSpeed = Math.Max(GameSpeed, 1);
                        if (DXInputState.Current.IsKeyPressed(DXKeys.Control))
                        {
                            GameSpeed = 1;
                        }
                        var ZXSystem_GameLevelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                        Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("ShowMessage", new Type[] { typeof(string), typeof(System.Drawing.Color), typeof(int) }).GetValue("Now Game Speed: " + GameSpeed, System.Drawing.Color.White, 2000);
                    }


                }
                else if (key == DXKeys.V)
                {
                    if (!ModOptions.Instance.FilterVeteranUnit)
                    {
                        return;
                    }
                    GamePatch.FilterVeteranUnit();
                }
                else if (key == DXKeys.L)
                {
                    if (!ModOptions.Instance.AutoFindBonusItem)
                    {
                        return;
                    }
                    var CSelectableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
                    var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
                    if (ZXEntity_CurrentEntitySelected == null)
                    {
                        return;
                    }
                    if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
                    {
                        return;
                    }
                    var entity = ZXEntity_CurrentEntitySelected;
                    var Cell = Traverse.Create(entity).Property("Cell").GetValue<System.Drawing.Point>();

                    if (!AccessTools.TypeByName("ZX.Entities.Hero").IsInstanceOfType(entity))
                    {
                        return;
                    }

                    dynamic CBonusItems = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CBonusItem")).MethodWithDecrypt("get_AllBonusNotPicked").GetValue();

                    object closet = null;
                    float minDistance = 999999999;
                    foreach (var item_dynamic in CBonusItems)
                    {
                        object item = item_dynamic;

                        var distance = Traverse.Create(item).Property("Entity").MethodWithDecrypt("DistanceTo", new Type[] { AccessTools.TypeByName("ZX.Entities.ZXEntity") }).GetValue<float>(entity);

                        if (distance < minDistance)
                        {
                            closet = item;
                            minDistance = distance;
                        }
                    }

                    DXEntity pickitem = Traverse.Create(closet).Property("Entity").GetValue<DXEntity>();

                    var pickitem_name = Traverse.Create(pickitem).MethodWithDecrypt("GetName").GetValue();

                    //Move to the location of pickitem
                    if (pickitem != null)
                    {
                        var ZXCommandType = AccessTools.TypeByName("ZX.Commands.ZXCommand");
                        var travel = AccessToolsEX.MethodWithDecrypt(AccessTools.TypeByName("ZX.Commands.ZXCommand"), "Get", generics: new Type[] { AccessTools.TypeByName("ZX.Commands.Interact") });
                        var TravelCommand = travel.Invoke(null, null);


                        var CCommandable = Traverse.Create(entity).MethodWithDecrypt("get_CCommandable").GetValue();
                        Traverse.Create(CCommandable).MethodWithDecrypt("set_Command", new Type[] { ZXCommandType }).GetValue(TravelCommand);
                        var DXEntityRefType = AccessToolsEX.TypeByNameWithDecrypt("DXVision.DXEntityRef`1").MakeGenericType(new Type[] { AccessTools.TypeByName("ZX.Entities.ZXEntity") });

                        var constructor = AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget"), new[] { pickitem.GetType() });
                        var zxCommandTargetInstance = constructor.Invoke(new object[] { pickitem });

                        var cellSize = Traverse.Create(entity).MethodWithDecrypt("get_CMovable").MethodWithDecrypt("get_CellSizeForPathfinding").GetValue<int>();

                        var position = Traverse.Create(entity).Property("Position").GetValue<System.Drawing.PointF>();
                        var nearestAvailablePositionNearToDestiny = DXWorldGrid.GetNearestAvailablePositionNearToDestiny(cellSize, Cell, pickitem.Position, 10, true);
                        if (nearestAvailablePositionNearToDestiny == null)
                        {
                            Traverse.Create(zxCommandTargetInstance).FieldWithDecrypt("Position").SetValue(pickitem.Position);
                            Traverse.Create(CCommandable).FieldWithDecrypt("_Target").SetValue(zxCommandTargetInstance);
                        }
                        else
                        {
                            Traverse.Create(zxCommandTargetInstance).FieldWithDecrypt("Position").SetValue(new System.Drawing.PointF(nearestAvailablePositionNearToDestiny.Value.X, nearestAvailablePositionNearToDestiny.Value.Y));
                            Traverse.Create(CCommandable).FieldWithDecrypt("_Target").SetValue(zxCommandTargetInstance);
                        }

                        var Command = Traverse.Create(entity).MethodWithDecrypt("get_CCommandable").MethodWithDecrypt("get_Command").GetValue();

                        var ZXEntityType = AccessTools.TypeByName("ZX.Entities.ZXEntity");
                        var ZXCommandTargetType = AccessTools.TypeByName("ZX.Commands.ZXCommandTarget");
                        var _Target = Traverse.Create(entity).MethodWithDecrypt("get_CCommandable").FieldWithDecrypt("_Target").GetValue();
                        Traverse.Create(Command).MethodWithDecrypt("Execute", new Type[] { ZXEntityType, ZXCommandTargetType }).GetValue(entity, _Target);
                    }
                }
                else if (key == DXKeys.E)
                {
                    if (!ModOptions.Instance.AutoDisperse)
                    {
                        return;
                    }

                    var CSelectableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
                    var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
                    if (ZXEntity_CurrentEntitySelected == null)
                    {
                        return;
                    }
                    if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
                    {
                        return;
                    }

                    var AllSelected = Traverse.Create(CSelectableType).FieldWithDecrypt("AllSelected").GetValue();
                    dynamic AllSelectedList = Traverse.Create(AllSelected).MethodWithDecrypt("ToList").GetValue();

                    List<DXEntity> AvailableTargets = new List<DXEntity>();

                    foreach (var cselectable in AllSelectedList)
                    {
                        DXEntity entity = cselectable.Entity;
                        if (entity != null)
                        {
                            if (Traverse.Create(entity).MethodWithDecrypt("get_Params").PropertyWithDecrypt("CanTravel").GetValue<bool>())
                                AvailableTargets.Add(entity);
                        }
                    }


                    int unitCount = AvailableTargets.Count;
                    float avgX = 0, avgY = 0;

                    foreach (var target in AvailableTargets)
                    {
                        var entity = target;
                        avgX += entity.Position.X;
                        avgY += entity.Position.Y;
                    }

                    avgX /= unitCount;
                    avgY /= unitCount;


                    int cols = (int)Math.Ceiling(Math.Sqrt(unitCount));
                    int rows = (int)Math.Ceiling((double)unitCount / cols);

                    float interval = 1f;

                    for (int i = 0; i < unitCount; i++)
                    {

                        int row = i / cols; // Calculate row index
                        int col = i % cols; // Calculate column index

                        var TravelCommand = AccessToolsEX.MethodWithDecrypt(AccessTools.TypeByName("ZX.Commands.ZXCommand"), "Get", generics: new Type[] { AccessTools.TypeByName("ZX.Commands.Travel") }).Invoke(null, null);

                        var newPosition = new System.Drawing.PointF(avgX + (col - (cols - 1) / 2f) * interval, avgY + (row - (rows - 1) / 2f) * interval);
                        // Assign entity to the unit in AvailableTargets that is closest to newPosition, using a loop to iterate through AvailableTargets
                        DXEntity nearestEntity = null;
                        float minDistance = 999999999;
                        foreach (var item in AvailableTargets)
                        {
                            var distance = item.DistanceTo(newPosition);
                            if (distance < minDistance)
                            {
                                nearestEntity = item;
                                minDistance = distance;
                            }
                        }
                        var entity = nearestEntity;
                        //Remove this unit from AvailableTargets to avoid duplicate selection
                        AvailableTargets.Remove(entity);

                        var Target = AccessToolsEX.CreateInstance(AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.ZXCommandTarget"), new object[] { newPosition });
                        Traverse.Create(entity).MethodWithDecrypt("get_CCommandable").MethodWithDecrypt("set_Command", new Type[] { AccessTools.TypeByName("ZX.Commands.ZXCommand") }).GetValue(TravelCommand);
                        Traverse.Create(TravelCommand).MethodWithDecrypt("Execute", new Type[] { AccessTools.TypeByName("ZX.Entities.ZXEntity"), AccessTools.TypeByName("ZX.Commands.ZXCommandTarget") }).GetValue(entity, Target);
                    }
                }
                else if (key == DXKeys.Delete)
                {
                    if (!ModOptions.Instance.DestroyAllSelectedUnits)
                    {
                        return;
                    }

                    var CSelectableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
                    var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
                    if (ZXEntity_CurrentEntitySelected == null)
                    {
                        return;
                    }
                    if (!ZXEntity_CurrentEntitySelected.GetType().IsSubclassOf(AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.HumanArmyUnit")))
                    {
                        return;
                    }

                    if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
                    {
                        return;
                    }

                    var allSelected = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable")).FieldWithDecrypt("AllSelected").PropertyWithDecrypt("Data").GetValue().ConvertToList();
                    if (allSelected.Count > 1)
                    {
                        var title = "Destroy all selected units?";
                        var description = "Are you sure you want to destroy all selected units?";
                        var onYes = new Action(() =>
                        {

                            var ZXSystem_GameLevel_instance = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel")).MethodWithDecrypt("get_Current").GetValue();
                            var target = Traverse.Create(ZXSystem_GameLevel_instance).FieldWithDecrypt("_CurrentTarget").GetValue();
                            Traverse.Create(target).MethodWithDecrypt("Clear").GetValue();

                            var DestroyCommandGetMethon = AccessToolsEX.MethodWithDecrypt(AccessTools.TypeByName("ZX.Commands.ZXCommand"), "Get", generics: new Type[] { AccessTools.TypeByName("ZX.Commands.Destroy") });
                            var currentCommand = DestroyCommandGetMethon.Invoke(null, null);
                            foreach (var item in allSelected)
                            {
                                var entity = Traverse.Create(item).PropertyWithDecrypt("Entity").GetValue<DXEntity>();
                                if (entity != null)
                                {

                                    var ccommandableZxentity = (entity != null) ? Traverse.Create(entity).MethodWithDecrypt("get_CCommandable").GetValue() : null;
                                    if (ccommandableZxentity != null
                                    && Traverse.Create(currentCommand).PropertyWithDecrypt("AvailableOnSelfAsTarget").GetValue<bool>()
                                    && Traverse.Create(ccommandableZxentity).PropertyWithDecrypt("AvailableCommands").MethodWithDecrypt("Contains", new Type[] { currentCommand.GetType() }).GetValue<bool>(currentCommand)
                                    && Traverse.Create(currentCommand).MethodWithDecrypt("IsEnabledFor", new Type[] { entity.GetType() }).GetValue<bool>(entity))
                                    {
                                        Traverse.Create(currentCommand).MethodWithDecrypt("Execute", new Type[] { entity.GetType(), target.GetType() }).GetValue(entity, target);
                                        if (Traverse.Create(entity).MethodWithDecrypt("get_CBehaviour").GetValue() != null && Traverse.Create(currentCommand).PropertyWithDecrypt("ResetMemModesAfterSelectingTarget").GetValue<bool>())
                                        {
                                            Traverse.Create(entity).MethodWithDecrypt("get_CBehaviour").MethodWithDecrypt("set_PerformResetMemNodes", new Type[] { typeof(bool) }).GetValue(true);
                                        }
                                    }
                                }
                            }
                        });
                        var onNo = new Action(() => { });

                        Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXMessageBox")).MethodWithDecrypt("AskYesNo", new Type[] { typeof(string), typeof(string), typeof(Action), typeof(Action) }).GetValue(title, description, onYes, onNo);
                    }
                }
                else if (key == DXKeys.N)
                {
                    if (!ModOptions.Instance.BatchCancelCommand)
                    {
                        return;
                    }
                    GamePatch.BatchCancelCommand();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
        private static void BatchCancelCommand()
        {
            var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
            if (ZXEntity_CurrentEntitySelected == null)
            {
                return;
            }
            if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
            {
                return;
            }

            //check if the unit is a building
            //ZX.Entities.Structure
            var checkType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.Structure");
            bool isBuilding = ZXEntity_CurrentEntitySelected.GetType().IsSubclassOf(checkType);
            if (!isBuilding)
                return;

            var AllSelected = Traverse.Create(CSelectableType).FieldWithDecrypt("AllSelected").GetValue();
            var AllSelectedList = AllSelected.CallMethod("ToList").ConvertToList();

            bool ControlPressed = DXInputState.Current.IsKeyPressed(DXKeys.Control);
            bool ShiftPressed = DXInputState.Current.IsKeyPressed(DXKeys.Shift);
            foreach (var item in AllSelectedList)
            {
                var cselectable = item as DXComponent;
                var entity = cselectable.Entity;
                var CCommandable = entity.CallMethod(D("get_CCommandable"));
                if (CCommandable == null)
                    continue;

                if (ShiftPressed)
                {
                    var OrdersQueued = CCommandable.GetPropertyValue(D("OrdersQueued"));
                    if (OrdersQueued != null)
                    {
                        var OrdersQueuedCount = (int)OrdersQueued.GetPropertyValue(D("Count"));
                        if (OrdersQueuedCount > 0)
                        {
                            for (int i = OrdersQueuedCount - 1; i >= 0; i--)
                            {
                                CCommandable.CallMethod(D("CancelCommandQueued"), i);
                            }
                        }
                    }
                }

                //check ctrl is pressed
                if (ControlPressed)
                {
                    CCommandable.CallMethod(D("CancelCurrentCommand"));
                    continue;
                }
                else
                {
                    var OrdersQueued = CCommandable.GetPropertyValue(D("OrdersQueued"));
                    if (OrdersQueued == null)
                    {
                        CCommandable.CallMethod(D("CancelCurrentCommand"));
                        continue;
                    }
                    var OrdersQueuedCount = (int)OrdersQueued.GetPropertyValue(D("Count"));
                    if (OrdersQueuedCount == 0)
                    {
                        CCommandable.CallMethod(D("CancelCurrentCommand"));
                        continue;
                    }
                    else
                    {
                        CCommandable.CallMethod(D("CancelCommandQueued"), OrdersQueuedCount - 1);
                        continue;
                    }
                }
                //cancel last order
            }
        }

        private static void FilterVeteranUnit()
        {
            var CSelectableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
            var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
            if (ZXEntity_CurrentEntitySelected == null)
            {
                return;
            }
            if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
            {
                return;
            }

            var AllSelected = Traverse.Create(CSelectableType).FieldWithDecrypt("AllSelected").GetValue();
            var AllSelectedList = AllSelected.CallMethod("ToList").ConvertToList();
            var CVeteranUnitType = AccessTools.TypeByName("ZX.Components.CVeteranUnit");
            foreach (var item in AllSelectedList)
            {
                var cselectable = item as DXComponent;
                //var HasComponent = AccessToolsEX.MethodWithDecrypt(cselectable.Entity.GetType(), "HasComponent", null, new Type[] { CVeteranUnitType });
                if (cselectable == null || cselectable.Entity == null)
                {
                    continue;
                }

                //var ZXComponentsCVeteranUnit = HasComponent.Invoke(cselectable.Entity, null);
                bool IsVeteran = (bool)cselectable.Entity.CallMethod(new Type[] { CVeteranUnitType }, D("HasComponent"));
                if (!IsVeteran && !DXVision.DXInputState.Current.IsKeyPressed(DXKeys.Shift))
                {
                    //Traverse.Create(cselectable).PropertyWithDecrypt("Selected").SetValue(false);
                    cselectable.SetPropertyValue(D("Selected"), false);
                }
                else if (IsVeteran && DXVision.DXInputState.Current.IsKeyPressed(DXKeys.Shift))
                {
                    //Traverse.Create((object)cselectable).PropertyWithDecrypt("Selected").SetValue(false);
                    cselectable.SetPropertyValue(D("Selected"), false);
                }
            }
            var ZXSystem_GameLevelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
            Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("PlaySelectionSound").GetValue();
            Traverse.Create(ZXSystem_GameLevelType).MethodWithDecrypt("get_Current").MethodWithDecrypt("UpdateSelection").GetValue();
        }

        private static void NumpadFilterVeteran(DXKeys key)
        {
            var CSelectableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
            var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
            if (ZXEntity_CurrentEntitySelected == null)
            {
                return;
            }
            if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
            {
                return;
            }
            int num = key - DXKeys.NumPad0;
            var AllSelected = Traverse.Create(CSelectableType).FieldWithDecrypt("AllSelected").GetValue();
            dynamic AllSelectedList = Traverse.Create(AllSelected).MethodWithDecrypt("ToList").GetValue();

            List<object> itemsToDeselect = new List<object>();

            foreach (var cselectable in AllSelectedList)
            {
                var HasComponent = AccessToolsEX.MethodWithDecrypt(cselectable.Entity.GetType(), "HasComponent", null, new Type[] { AccessTools.TypeByName("ZX.Components.CVeteranUnit") });
                if (cselectable == null || cselectable.Entity == null)
                {
                    continue;
                }
                var ZXComponentsCVeteranUnit = HasComponent.Invoke(cselectable.Entity, null);
                if (ZXComponentsCVeteranUnit == null)
                {
                    continue;
                }
                bool IsVeteran = (bool)ZXComponentsCVeteranUnit;

                if (!IsVeteran && !DXVision.DXInputState.Current.IsKeyPressed(DXKeys.Shift))
                {
                    Traverse.Create((object)cselectable).PropertyWithDecrypt("Selected").SetValue(false);
                }
                else if (IsVeteran && DXVision.DXInputState.Current.IsKeyPressed(DXKeys.Shift))
                {
                    Traverse.Create((object)cselectable).PropertyWithDecrypt("Selected").SetValue(false);
                }
                else
                {
                    itemsToDeselect.Add(cselectable);
                }
            }

            // Check if the quantity exceeds num, remove excess until the quantity is no longer greater than num

            // Retrieve all selected units again

            if (itemsToDeselect.Count > num)
            {
                AllSelectedList = Traverse.Create(AllSelected).MethodWithDecrypt("ToList").GetValue();

                itemsToDeselect = itemsToDeselect.Skip(num).ToList();

                foreach (var item in itemsToDeselect)
                {
                    Traverse.Create(item).PropertyWithDecrypt("Selected").SetValue(false);
                }
            }

            Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel")).MethodWithDecrypt("get_Current").MethodWithDecrypt("PlaySelectionSound").GetValue();
            Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel")).MethodWithDecrypt("get_Current").MethodWithDecrypt("UpdateSelection").GetValue();
        }

        static Type TravelType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Commands.Travel");
        static Type ZXEntityType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.ZXEntity");
        static Type CSelectableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
        static Type CUnitsHolderType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CUnitsHolder");
        static Type ZXSystem_GameLevel = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
        static Type CBuildableType = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CBuildable");
        private static void AutoGoWatchTower()
        {
            var ZXEntity_CurrentEntitySelected = Traverse.Create(CSelectableType).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
            if (ZXEntity_CurrentEntitySelected == null)
            {
                return;
            }

            if (Traverse.Create(ZXEntity_CurrentEntitySelected).PropertyWithDecrypt("Team").GetValue<ZX.ZXTeamType>() != ZX.ZXTeamType.Player)
            {
                return;
            }

            if (!ZXEntity_CurrentEntitySelected.GetType().IsSubclassOf(HumanArmyUnitType))
            {
                if ((bool)((DXEntity)ZXEntity_CurrentEntitySelected).CallMethod(new Type[] { CUnitsHolderType }, "HasComponent"))
                {
                    AutoGoWatchTowerB();
                }
                return;
            }

            //if (DXGame.Current.Paused)
            //{
            //    return;
            //}

            var AllSelected = CSelectableType.GetFieldValue(D("AllSelected")).CallMethod("ToList").ConvertToList();
            var AllHolder = DXGame.Current.ComponentsOfType(CUnitsHolderType);

            var CommandableEntityType = HumanArmyUnitType;//AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.HumanArmyUnit");
            var AllCommandableEntity = DXGame.Current.EntitiesOfType(CommandableEntityType);

            Dictionary<DXEntity, int[]> holderUsedSlotCount = new Dictionary<DXEntity, int[]>();

            //Count the remaining empty slots in the tower
            foreach (var item in AllHolder)
            {
                var HolderEntity = item.Entity;
                var Enabled = Traverse.Create(HolderEntity).PropertyWithDecrypt("Enabled").GetValue<bool>();
                if (!Enabled)
                    continue;
                //if HolderEntity is upgrading, skip it
                var component = HolderEntity.CallMethod(new Type[] { CBuildableType }, D("GetComponent"));
                if (component != null && (ZX.ZXBuildMode)component.GetPropertyValue(D("BuildMode")) == ZX.ZXBuildMode.Upgrading)
                    continue;


                int NTotalSlots = Traverse.Create(item).PropertyWithDecrypt("NTotalSlots").GetValue<int>();
                int NSlotsUsed = Traverse.Create(item).PropertyWithDecrypt("NSlotsUsed").GetValue<int>();
                if (NSlotsUsed < NTotalSlots)
                    holderUsedSlotCount.Add(item.Entity, new int[] { NTotalSlots, NSlotsUsed });
            }
            //Count whether there are units already on the way to the tower
            foreach (var item in AllCommandableEntity)
            {
                var CCommandable = Traverse.Create(item).MethodWithDecrypt("get_CCommandable").GetValue<DXComponent>();
                if (Traverse.Create(CCommandable).MethodWithDecrypt("get_Command").GetValue()?.GetType().FullName == "ZX.Commands.Travel")
                {
                    var CCommandableTargetEntity = Traverse.Create(CCommandable).PropertyWithDecrypt("Target").FieldWithDecrypt("EntityRef").PropertyWithDecrypt("Entity").GetValue<DXEntity>();
                    if (CCommandableTargetEntity == null)
                        continue;
                    var HasComponent = AccessToolsEX.MethodWithDecrypt(CCommandableTargetEntity.GetType(), "HasComponent", null, new Type[] { CUnitsHolderType });
                    bool isHasComponent = (bool)HasComponent.Invoke(CCommandableTargetEntity, null);
                    if (isHasComponent)
                    {
                        var TargetHolderEntity = CCommandableTargetEntity;
                        if (TargetHolderEntity == null)
                            continue;
                        AllSelected.Remove(CCommandable);
                        if (holderUsedSlotCount.ContainsKey(TargetHolderEntity))
                        {
                            holderUsedSlotCount[TargetHolderEntity][1] += 1;
                            if (holderUsedSlotCount[TargetHolderEntity][1] >= holderUsedSlotCount[TargetHolderEntity][0])
                            {
                                holderUsedSlotCount.Remove(TargetHolderEntity);
                            }
                        }
                    }
                }
            }

            List<object> allSelectedList = AllSelected.Where(x =>
            {
                return (bool)((DXComponent)x).Entity.CallMethod(D("get_Params")).GetPropertyValue(D("CanEnterInBuildings"));
            }).ToList();
            while (allSelectedList.Count > 0)
            {
                var current = (DXComponent)allSelectedList.First();
                var currentEntity = current.Entity;

                var closetHolderWhere = holderUsedSlotCount.Where(x =>
                {
                    var holderCell = x.Key.Position;
                    var distance = currentEntity.Position.DistanceTo(holderCell);
                    return distance <= ModOptions.Instance.AutoGoWatchTowerRadius;
                });
                if (!closetHolderWhere.Any())
                {
                    allSelectedList.Remove(current);
                    continue;
                };
                var closetHolder = closetHolderWhere.MinBy(x =>
                {
                    var holderCell = x.Key.Position;
                    var distance = currentEntity.Position.DistanceTo(holderCell);
                    return distance;
                });

                var closetHolderLeftSlot = closetHolder.Value[0] - closetHolder.Value[1];
                var closetHolderLeftSlotUnits = allSelectedList.Where(x =>
                {
                    var entity = ((DXComponent)x).Entity;
                    var distance = closetHolder.Key.Position.DistanceTo(entity.Position);
                    return distance <= ModOptions.Instance.AutoGoWatchTowerRadius;
                }).OrderBy(x =>
                {
                    var entity = ((DXComponent)x).Entity;
                    var distance = closetHolder.Key.Position.DistanceTo(entity.Position);
                    return distance;
                }).Take(closetHolderLeftSlot).ToList();

                if (closetHolderLeftSlotUnits.Count == 0)
                    allSelectedList.Remove(current);//can not find any unit in the radius of the tower, remove it from the list

                // alloc the units to the closetHolder
                foreach (var item in closetHolderLeftSlotUnits)
                {
                    var unity = ((DXComponent)item).Entity;
                    var TravelCommand = ZXCommandType.CallMethod(new Type[] { TravelType }, D("Get"));
                    var TargetHolder = closetHolder.Key;
                    var TargetHolderPosition = TargetHolder.Position;
                    var Target = ZXCommandTargetType.CreateInstance(new object[] { TargetHolder });
                    Target = Target.WrapIfValueType();
                    Target.SetFieldValue(D("Position"), TargetHolderPosition);
                    Target = Target.UnwrapIfWrapped();
                    TravelCommand.CallMethod(D("Execute"), unity, Target);
                    // Update tower vacancy statistics
                    holderUsedSlotCount[closetHolder.Key][1] += 1;
                    if (holderUsedSlotCount[closetHolder.Key][1] >= holderUsedSlotCount[closetHolder.Key][0])
                    {
                        holderUsedSlotCount.Remove(closetHolder.Key);
                    }

                    // Set the target's selection status to false to avoid repeated tower placement
                    if (ModOptions.Instance.DeselectUnitsAfterTowerSearch)
                        item.SetPropertyValue(D("Selected"), false);
                    allSelectedList.Remove(item);
                }
            }
            Traverse.Create(ZXSystem_GameLevel).MethodWithDecrypt("get_Current").MethodWithDecrypt("PlaySelectionSound").GetValue();
            Traverse.Create(ZXSystem_GameLevel).MethodWithDecrypt("get_Current").MethodWithDecrypt("UpdateSelection").GetValue();
        }

        /// <summary>
        ///  if choose a tower, the units around it can enter the tower automatically
        /// </summary>
        private static void AutoGoWatchTowerB()
        {
            var AllSelected = CSelectableType.GetFieldValue(D("AllSelected")).CallMethod("ToList").ConvertToList();
            var AllHolder = AllSelected.ToList();
            var CommandableEntityType = HumanArmyUnitType;
            var AllCommandableEntity = DXGame.Current.EntitiesOfType(CommandableEntityType);


            Dictionary<DXEntity, int[]> holderUsedSlotCount = new Dictionary<DXEntity, int[]>();

            //Count the remaining empty slots in the tower
            foreach (var item2 in AllHolder)
            {
                var item = (DXComponent)item2;
                var HolderEntity = item.Entity;
                var Enabled = Traverse.Create(HolderEntity).PropertyWithDecrypt("Enabled").GetValue<bool>();
                if (!Enabled)
                    continue;
                //if HolderEntity is upgrading, skip it
                var component = HolderEntity.CallMethod(new Type[] { CBuildableType }, D("GetComponent"));
                if (component != null && (ZX.ZXBuildMode)component.GetPropertyValue(D("BuildMode")) == ZX.ZXBuildMode.Upgrading)
                    continue;

                var CUnitsHolder = HolderEntity.CallMethod(new Type[] { CUnitsHolderType }, D("GetComponent"));
                int NTotalSlots = Traverse.Create(CUnitsHolder).PropertyWithDecrypt("NTotalSlots").GetValue<int>();
                int NSlotsUsed = Traverse.Create(CUnitsHolder).PropertyWithDecrypt("NSlotsUsed").GetValue<int>();
                if (NSlotsUsed < NTotalSlots)
                    holderUsedSlotCount.Add(item.Entity, new int[] { NTotalSlots, NSlotsUsed });
            }
            //合法的单位
            List<DXEntity> avaliableUnits = AllCommandableEntity.Where(x =>
            {
                bool isInBuilding = (bool)x.CallMethod(new Type[] { CInsideBuildingType }, D("HasComponent"));
                bool CanEnterInBuildings = (bool)x.CallMethod(D("get_Params")).GetPropertyValue(D("CanEnterInBuildings"));
                return !isInBuilding && CanEnterInBuildings;
            }).ToList();

            //Count whether there are units already on the way to the tower
            foreach (var item in AllCommandableEntity)
            {
                var CCommandable = Traverse.Create(item).MethodWithDecrypt("get_CCommandable").GetValue<DXComponent>();
                if (Traverse.Create(CCommandable).MethodWithDecrypt("get_Command").GetValue()?.GetType().FullName == "ZX.Commands.Travel")
                {
                    var CCommandableTargetEntity = Traverse.Create(CCommandable).PropertyWithDecrypt("Target").FieldWithDecrypt("EntityRef").PropertyWithDecrypt("Entity").GetValue<DXEntity>();
                    if (CCommandableTargetEntity == null)
                        continue;
                    var HasComponent = AccessToolsEX.MethodWithDecrypt(CCommandableTargetEntity.GetType(), "HasComponent", null, new Type[] { CUnitsHolderType });
                    bool isHasComponent = (bool)HasComponent.Invoke(CCommandableTargetEntity, null);
                    if (isHasComponent)
                    {
                        var TargetHolderEntity = CCommandableTargetEntity;
                        if (TargetHolderEntity == null)
                            continue;
                        avaliableUnits.Remove(item);
                        if (holderUsedSlotCount.ContainsKey(TargetHolderEntity))
                        {
                            holderUsedSlotCount[TargetHolderEntity][1] += 1;
                            if (holderUsedSlotCount[TargetHolderEntity][1] >= holderUsedSlotCount[TargetHolderEntity][0])
                            {
                                holderUsedSlotCount.Remove(TargetHolderEntity);
                            }
                        }
                    }
                }
            }

            var AllHandlerHolder = holderUsedSlotCount.Keys.ToList();
            foreach (var item in AllHandlerHolder)
            {
                var holderLeftSlot = holderUsedSlotCount[item][0] - holderUsedSlotCount[item][1];
                var holderLeftSlotUnits = avaliableUnits.Where(x =>
                {
                    var entity = x;
                    var distance = item.Position.DistanceTo(entity.Position);
                    return distance <= ModOptions.Instance.AutoGoWatchTowerRadius;
                }).OrderBy(x =>
                {
                    var entity = x;
                    var distance = item.Position.DistanceTo(entity.Position);
                    return distance;
                }).Take(holderLeftSlot).ToList();
                if (holderLeftSlotUnits.Count == 0)
                    continue;

                // alloc the units to the holder
                foreach (var unit in holderLeftSlotUnits)
                {
                    var TravelCommand = ZXCommandType.CallMethod(new Type[] { TravelType }, D("Get"));
                    var TargetHolder = item;
                    var TargetHolderPosition = TargetHolder.Position;
                    var Target = ZXCommandTargetType.CreateInstance(new object[] { TargetHolder });
                    Target = Target.WrapIfValueType();
                    Target.SetFieldValue(D("Position"), TargetHolderPosition);
                    Target = Target.UnwrapIfWrapped();
                    TravelCommand.CallMethod(D("Execute"), unit, Target);
                    // Update tower vacancy statistics
                    holderUsedSlotCount[item][1] += 1;
                    if (holderUsedSlotCount[item][1] >= holderUsedSlotCount[item][0])
                    {
                        holderUsedSlotCount.Remove(item);
                    }
                    avaliableUnits.Remove(unit);
                }
            }
        }

        static internal double GameSpeed = 1.0;
        internal static bool OnSetPaused(DXGame __instance, ref bool value)
        {
            if (value)
            {
                //Record game speed
                GameSpeed = __instance.GameSpeed > 0 ? __instance.GameSpeed : 1.0;
                return true;
            }
            __instance.GameSpeed = GameSpeed;
            return false;
        }

        internal enum ResourceType
        {
            Workers,
            Food,
            Energy,
            Wood,
            Stone,
            Iron,
            Oil,
            Gold
        }

        internal static bool OnOptionActivated(object __instance, MethodBase __originalMethod, object[] __args)
        {
            try
            {
                object cc = __args[0];
                var ZXSystem_GameLevel_instance = __instance;
                var Tag = Traverse.Create(cc).PropertyWithDecrypt("Tag").GetValue();
                Dictionary<DXEntity, float> RecordTrainCount = new Dictionary<DXEntity, float>();
                if (Tag != null && Tag.GetType().FullName == "ZX.Commands.Train")
                {
                    var currentCommand = Tag;
                    var target = Traverse.Create(ZXSystem_GameLevel_instance).FieldWithDecrypt("_CurrentTarget").GetValue();
                    Traverse.Create(target).MethodWithDecrypt("Clear").GetValue();

                    Dictionary<DXEntity, float> buildCountMap = new Dictionary<DXEntity, float>();
                    var allSelected = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable")).FieldWithDecrypt("AllSelected").PropertyWithDecrypt("Data").GetValue().ConvertToList();
                    int needToTrainCount = 0;
                    int maxTrainCount = allSelected.Where(x =>
                    {
                        var zxentity = Traverse.Create(x).PropertyWithDecrypt("Entity").GetValue<DXEntity>();
                        bool canCount = Traverse.Create(zxentity).PropertyWithDecrypt("Enabled").GetValue<bool>();
                        if (canCount)
                        {
                            needToTrainCount++;
                            canCount = false;
                            var ccommandableZxentity = (zxentity != null) ? Traverse.Create(zxentity).MethodWithDecrypt("get_CCommandable").GetValue() : null;
                            if (ccommandableZxentity != null
                            && Traverse.Create(currentCommand).PropertyWithDecrypt("AvailableOnSelfAsTarget").GetValue<bool>()
                            && Traverse.Create(ccommandableZxentity).PropertyWithDecrypt("AvailableCommands").MethodWithDecrypt("Contains", new Type[] { currentCommand.GetType() }).GetValue<bool>(currentCommand)
                            && Traverse.Create(currentCommand).MethodWithDecrypt("IsEnabledFor", new Type[] { zxentity.GetType() }).GetValue<bool>(zxentity))
                            {
                                canCount = true;
                                // Easily update the current building's load
                                var CanQueueCommands = Traverse.Create(zxentity).MethodWithDecrypt("get_Params").PropertyWithDecrypt("CanQueueCommands").GetValue<bool>();
                                //Current number of training units
                                int bCount = Traverse.Create(zxentity).MethodWithDecrypt("get_CCommandable").PropertyWithDecrypt("OrdersQueued").PropertyWithDecrypt("Count").GetValue<int>();
                                buildCountMap.Add(zxentity, bCount);

                                var temp_Command = Traverse.Create(zxentity).MethodWithDecrypt("get_CCommandable").MethodWithDecrypt("get_Command").GetValue();
                                if (temp_Command != null)
                                {
                                    var component = AccessToolsEX.MethodWithDecrypt(zxentity.GetType(), "GetComponent", null, new Type[] { AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CBuilder") }).Invoke(zxentity, null);
                                    if (component != null)
                                    {
                                        var BuildingFactor = Traverse.Create(component).PropertyWithDecrypt("BuildingFactor").GetValue<float>();
                                        var nowProccess = Math.Max(0f, 1 - BuildingFactor);
                                        buildCountMap[zxentity] += nowProccess;
                                    }
                                }
                            }
                        }

                        return canCount;
                    }
                    ).ToList().Count;

                    //Calculate how many soldiers can be produced based on energy consumption
                    var CurrentEntitySelected = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable")).MethodWithDecrypt("get_CurrentEntitySelected").GetValue();
                    var obj = Traverse.Create(currentCommand).MethodWithDecrypt("GetResourcesCost", new Type[] { CurrentEntitySelected.GetType() }).GetValue(CurrentEntitySelected);
                    var ZXLevelState_Traverse = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXLevelState")).MethodWithDecrypt("get_Current");

                    int[] resourcesCost = new int[8];

                    for (int i = 0; i < 8; i++)
                    {
                        //Current resource count
                        resourcesCost[i] = ZXLevelState_Traverse.MethodWithDecrypt("GetAvailabilityOfResource", new Type[] { typeof(int) }).GetValue<int>(i);
                        //Maximum number of productions that can be paid
                        int cost = Traverse.Create(obj).PropertyWithDecrypt(Enum.GetName(typeof(ResourceType), i)).GetValue<int>();
                        if (cost > 0)
                        {
                            needToTrainCount = Math.Min(needToTrainCount, (int)(resourcesCost[i] / cost));
                        }
                    }

                    for (int i = 0; i < needToTrainCount; i++)
                    {
                        // Find the one with the least quantity in buildCountMap
                        DXEntity zxentity = null;
                        float minCount = float.MaxValue;
                        foreach (var item in buildCountMap)
                        {
                            //向上取证,如果大于等于5则跳过,因为队列已满
                            if (Math.Ceiling(item.Value) >= 5)
                            {
                                continue;
                            }

                            if (item.Value < minCount)
                            {
                                minCount = item.Value;
                                zxentity = item.Key;
                            }
                        }
                        // If no buildings are available, break out of the loop
                        if (zxentity == null)
                        {
                            break;
                        }

                        //Every time it is necessary to judge whether the output is sufficient for production
                        bool IsAvailableCommand = (bool)zxentity.CallMethod(D("get_CCommandable")).GetPropertyValue(D("AvailableCommands")).CallMethod(D("Contains"), currentCommand);
                        bool IsEnabled = (bool)currentCommand.CallMethod(D("IsEnabledFor"), zxentity);
                        if (!IsAvailableCommand || !IsEnabled)
                        {
                            continue;
                        }

                        Traverse.Create(currentCommand).MethodWithDecrypt("Execute", new Type[] { zxentity.GetType(), target.GetType() }).GetValue(zxentity, target);
                        if (Traverse.Create(zxentity).MethodWithDecrypt("get_CBehaviour").GetValue() != null && Traverse.Create(currentCommand).PropertyWithDecrypt("ResetMemModesAfterSelectingTarget").GetValue<bool>())
                        {
                            Traverse.Create(zxentity).MethodWithDecrypt("get_CBehaviour").MethodWithDecrypt("set_PerformResetMemNodes", new Type[] { typeof(bool) }).GetValue(true);
                        }
                        buildCountMap[zxentity]++;
                    }

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.Log("Error in OnOptionActivated: " + e.ToString());
                return false;
            }
        }

        internal static bool OnShowInGameMenu(object __instance)
        {
            try
            {
                var methodW = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem")).MethodWithDecrypt("W", new Type[] { typeof(string) });
                Traverse Current = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGame")).MethodWithDecrypt("get_Current");

                bool paused = DXGame.Current.Paused;
                DXGame.Current.Paused = true;
                DXVision.DXGame.Current.ResetInput();

                //ZX.Clips/Interface
                //DXVision.DXProjectClip #=zvpIN4HssN4esYi0ibA==/#=zMnJEyAU=::#=zbLDsGnbmbQA7fIPgPA==()

                var type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Clips");
                type = AccessToolsEX.InnerTypeWithDecrypt(type, "Interface");
                var GameMenu = Traverse.Create(type).MethodWithDecrypt("get_GameMenu").GetValue<DXProjectClip>();

                DXVision.DXObject_ProjectClip oClip = GameMenu.GetDXObject(DXScene.ScreenVirtualRatio, 0);
                oClip.InheritImageAttributes = false;
                oClip.State.SetCurrentAreaForAnchorChilds();
                oClip.Area = DXVision.DXScreenParameters.Current.ViewArea;
                oClip.State.AreaStyle = DXVision.DXObjectState.TAreaStyle.AreaInPixels;
                oClip["@Background"].Image = DXVision.DXImage.FromColor(System.Drawing.Color.FromArgb(220, 8, 0, 0));
                DXVision.DXObject dxobject = oClip["@MainMenuArea"];
                DXVision.GUI.DXGridLayout dxgridLayout = DXVision.GUI.DXGridLayout.NewVerticalFlowLayout(dxobject.Width, 0, false);
                oClip.ChangeObjectBy("@MainMenuArea", dxgridLayout);
                dxgridLayout.Location = dxobject.Location;
                dxgridLayout.DefaultCellSpacing = new System.Drawing.SizeF(DXVision.DXScene.ScreenVirtualUnit * 10f, DXVision.DXScene.ScreenVirtualUnit * 10f);
                dxgridLayout.DefaultRowHeight = DXVision.DXScene.ScreenVirtualUnit * 20f;
                DXTextButton zxbuttonLink = (DXTextButton)AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXButtonLink"), new Type[] { typeof(string), typeof(DXKeys[]) }).Invoke(new object[] { methodW.GetValue<string>("Back to the Game"), new DXKeys[] { DXKeys.Escape } });

                dxgridLayout.AddRow(new DXVision.DXObject[] { zxbuttonLink });
                zxbuttonLink.MinTimeBetweenActivations = 2000;
                zxbuttonLink.Activated += delegate (DXVision.GUI.DXButton s)
                {
                    DXVision.GUI.DXModalScreen.RemoveModal(oClip);
                    DXVision.DXGame.Current.Paused = paused;
                };
                dxgridLayout.AddRow(System.Array.Empty<DXVision.DXObject>());
                dxgridLayout.AddRow(System.Array.Empty<DXVision.DXObject>());
                DXTextButton zxbuttonLink2 = (DXTextButton)AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXButtonLink"), new Type[] { typeof(string), typeof(DXKeys[]) }).Invoke(new object[] { methodW.GetValue<string>("Options"), new DXKeys[] { DXKeys.O } });

                zxbuttonLink2.MinTimeBetweenActivations = 2000;
                dxgridLayout.AddRow(new DXVision.DXObject[] { zxbuttonLink2 });
                zxbuttonLink2.Activated += delegate (DXVision.GUI.DXButton s)
                {
                    Current.MethodWithDecrypt("ShowOptionsWindow").GetValue();
                };
                DXTextButton zxbuttonLink3 = (DXTextButton)AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXButtonLink"), new Type[] { typeof(string), typeof(DXKeys[]) }).Invoke(new object[] { methodW.GetValue<string>("Help"), new DXKeys[] { DXKeys.H } });

                dxgridLayout.AddRow(new DXVision.DXObject[] { zxbuttonLink3 });
                zxbuttonLink3.Activated += delegate (DXVision.GUI.DXButton o)
                {
                    //ZX.ZXGame.get_Current().ShowHelpWindow();
                    Current.MethodWithDecrypt("ShowHelpWindow").GetValue();
                };

                dxgridLayout.AddRow(System.Array.Empty<DXVision.DXObject>());

                DXTextButton zxbuttonLink5 = (DXTextButton)AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXButtonLink"), new Type[] { typeof(string), typeof(DXKeys[]) }).Invoke(new object[] { "Exit", new DXKeys[] { } });
                zxbuttonLink5.MinTimeBetweenActivations = 10000;
                dxgridLayout.AddRow(new DXVision.DXObject[] { zxbuttonLink5 });
                zxbuttonLink5.Activated += delegate (DXVision.GUI.DXButton s)
                {
                    Current.MethodWithDecrypt("ShowStartScreen", new Type[] { typeof(Action), typeof(bool) }).GetValue(null, true);
                };




                DXTextButton zxbuttonLink6 = (DXTextButton)AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXButtonLink"), new Type[] { typeof(string), typeof(DXKeys[]) }).Invoke(new object[] { "Save", new DXKeys[] { } });

                zxbuttonLink6.MinTimeBetweenActivations = 10000;
                dxgridLayout.AddRow(new DXVision.DXObject[] { zxbuttonLink6 });
                zxbuttonLink6.Activated += delegate (DXVision.GUI.DXButton s)
                {



                    string name = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGameState")).MethodWithDecrypt("get_Current").PropertyWithDecrypt("Name").GetValue<string>();
                    name = DXHelper_Path.FixFileName(name);
                    var ZXGameType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGame");
                    var savePath = (string)ZXGameType.CallMethod(D("get_SaveGameFolder"));
                    var saveFile = System.IO.Path.Combine(savePath, name + ".zxsav");
                    saveFile = DXHelper_Path.FixFileName(saveFile);

                    string GetAvailableSaveSlots(string filename)
                    {
                        if (!System.IO.File.Exists(saveFile))
                        {
                            return saveFile;
                        }


                        //判断备份存档是否到达最大数量
                        if (ModOptions.Instance.MaxSaveBackup == 0)
                        {
                            return name;
                        }

                        //查找目录所有符合名字的存档,按照最后修改时间升序排序
                        string[] files = System.IO.Directory.GetFiles(savePath, name + "_*.zxsav").OrderBy(x => System.IO.File.GetLastWriteTime(x)).ToArray();
                        if (files.Length >= ModOptions.Instance.MaxSaveBackup)
                        {
                            //删除所有超过或等于最大数量的备份,给新存档留出空间
                            for (int i = 0; i <= files.Length - ModOptions.Instance.MaxSaveBackup; i++)
                            {
                                System.IO.File.Delete(files[i]);
                                var zxcheckFilename = files[i].Replace(".zxsav", ".zxcheck");
                                if (System.IO.File.Exists(zxcheckFilename))
                                {
                                    System.IO.File.Delete(zxcheckFilename);
                                }
                            }
                        }

                        //返回新的存档名,名字规则为name+-年-月-日-时-分-秒
                        string time = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

                        //返回新的存档名,名字规则为name+年y月m日d时H时M分S秒,标注是年月日时分秒
                        //string time = DateTime.Now.ToString("yyyyyMMddHHmmss");
                        //System.Int32 ZX.ZXLevelState::ColonyDays()//System.Int32 ZX.ZXLevelState::ColonyDays()
                        int ColonyDays = (int)ZXLevelStateType.CallMethod(D("get_Current")).GetPropertyValue(D("ColonyDays"));
                        int ColonyHourOfDay = (int)ZXLevelStateType.CallMethod(D("get_Current")).GetPropertyValue(D("ColonyHourOfDay"));
                        string gameDayEXT = "_D" + ColonyDays + "H" + ColonyHourOfDay;
                        return name + gameDayEXT + "_" + time + ".zxsav";
                    }

                    //判断存档是否存在
                    var newfilename = GetAvailableSaveSlots(saveFile);
                    //Current.MethodWithDecrypt("SaveGame", new Type[] { typeof(string), typeof(Action), typeof(bool), typeof(bool) }).GetValue(name, null, true, true);
                    Current.MethodWithDecrypt("SaveGame", new Type[] { typeof(string), typeof(Action), typeof(bool), typeof(bool) }).GetValue(newfilename, null, true, true);
                    //重置自动保存时间
                    Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel")).MethodWithDecrypt("get_Current").FieldWithDecrypt("_LastAutoBackupTime").SetValue(DXVision.DXScene.Current.VirtualTimeInt);
                };



                DXTextButton zxbuttonLink4 = (DXTextButton)AccessTools.Constructor(AccessToolsEX.TypeByNameWithDecrypt("ZX.GUI.ZXButtonLink"), new Type[] { typeof(string), typeof(DXKeys[]) }).Invoke(new object[] { methodW.GetValue<string>("Save and Exit"), new DXKeys[] { } });

                zxbuttonLink4.MinTimeBetweenActivations = 10000;
                dxgridLayout.AddRow(new DXVision.DXObject[] { zxbuttonLink4 });
                zxbuttonLink4.Activated += delegate (DXVision.GUI.DXButton s)
                {
                    string name = Traverse.Create(AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGameState")).MethodWithDecrypt("get_Current").PropertyWithDecrypt("Name").GetValue<string>();
                    Current.MethodWithDecrypt("SaveGame", new Type[] { typeof(string), typeof(Action), typeof(bool), typeof(bool) }).GetValue(name, new Action(() =>
                    {
                        Current.MethodWithDecrypt("ShowStartScreen", new Type[] { typeof(Action), typeof(bool) }).GetValue(null, true);
                    })
                    , true, true);
                };
                DXVision.GUI.DXModalScreen.AddModal(oClip, null);
            }
            catch (Exception e)
            {
                FileLog.Log("Error in OnShowInGameMenu: " + e.ToString());
                return true;
            }

            return false;
        }

        internal static bool OnGetIDResearchsRecentUnlocked(object __instance, ref List<string> __result)
        {
            var IDResearchUnlocked = Traverse.Create(__instance).PropertyWithDecrypt("IDResearchUnlocked").GetValue<List<string>>();
            List<string> result = new List<string>();
            foreach (var research in IDResearchUnlocked)
            {
                result.Add(research);
            }
            __result = result;
            return false;
        }

        internal static bool OnGetHeroPerksTakenNow(object __instance, ref List<string> __result)
        {
            var result = ((List<string>)__instance.GetPropertyValue(D("HeroPerksTaken"))).ToList();
            __result = result;
            return false;
        }

        internal static bool OnSaveBackup()
        {
            return false;
        }

        internal static IEnumerable<CodeInstruction> OnCheckAutoBackup(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> newInstructions = new List<CodeInstruction>();
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == System.Reflection.Emit.OpCodes.Ldc_I4 && (int)instruction.operand == 1200000)
                {
                    instruction.operand = ModOptions.Instance.AutoSaveInterval * 1000;
                }
                newInstructions.Add(instruction);
            }
            return newInstructions;
        }

        //public static void OnDeleteSaveGames(object[] __args)
        //{
        //    string name = (string)__args[0];
        //    var savePath = (string)AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGame").CallMethod(D("get_SaveGameFolder"));


        //    //删除所有符合名字的存档,名称格式为年-月-日 时.分.秒
        //    string[] files = System.IO.Directory.GetFiles(savePath, name + "_*.zxsav").OrderBy(x => System.IO.File.GetLastWriteTime(x)).ToArray();
        //    for (int i = 0; i <= files.Length; i++)
        //    {
        //        System.IO.File.Delete(files[i]);
        //        var zxcheckFilename = files[i].Replace(".zxsav", ".zxcheck");
        //        if (System.IO.File.Exists(zxcheckFilename))
        //        {
        //            System.IO.File.Delete(zxcheckFilename);
        //        }
        //    }
        //}

        public static IEnumerable<CodeInstruction> DeleteSaveGamesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //remove catch where
            var code = new List<CodeInstruction>(instructions);
            var newInstructions = new List<CodeInstruction>();
            for (int i = 0; i < code.Count; i++)
            {
                if (i >= 58 && i <= 67)
                {
                    continue;
                }
                newInstructions.Add(code[i]);
            }
            return newInstructions;
        }

        public static void OnDeleteSaveGames(object __instance, object[] __args)
        {
            var name = __args[0] as string;
            var type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXGame");
            string savePath = type.CallMethod(D("get_SaveGameFolder")) as string;

            //删除备份存档
            string[] files = System.IO.Directory.GetFiles(savePath, name + "_*.zxsav").OrderBy(x => System.IO.File.GetLastWriteTime(x)).ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                System.IO.File.Delete(files[i]);
                var zxcheckFilename = files[i].Replace(".zxsav", ".zxcheck");
                if (System.IO.File.Exists(zxcheckFilename))
                {
                    System.IO.File.Delete(zxcheckFilename);
                }
            }
        }

        internal static bool OnCorrectSelection(object __instance)
        {
            if ((bool)__instance.GetPropertyValue(D("SelectingTargetForCommand")))
            {
                return true;
            }
            var _OSelectorByArea = ((DXSelectorByArea)__instance.GetFieldValue(D("_OSelectorByArea")));
            _OSelectorByArea.Hide();
            if (((DXScene)__instance.GetPropertyValue(D("Scene"))).RootObject.ObjectAreaToObjectArea(_OSelectorByArea, _OSelectorByArea.InnerArea, false).Size.GetMaxDimension() < 1f)
            {
                return true;
            }

            var list = __instance.CallMethod(D("GetSelectablesInSelectionArea")).ConvertToList();

            if (DXInputState.Current.IsKeyPressed(DXKeys.Shift, false))
            {
                using (List<object>.Enumerator enumerator = list.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var cselectable = enumerator.Current;
                        cselectable.SetPropertyValue(D("Selected"), true);
                    }
                }
            }
            else if (DXInputState.Current.IsKeyPressed(DXKeys.Control, false))
            {
                var type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
                //ZX.Entities.ZXEntity ZX.Components.CSelectable::get_CurrentEntitySelected()
                var currentEntitySelected = type.CallMethod(D("get_CurrentEntitySelected"));
                if (currentEntitySelected == null)
                {
                    return true;
                }
                foreach (var cselectable in list)
                {
                    var Entity = cselectable.GetPropertyValue(D("Entity"));
                    if (Entity.GetType() == currentEntitySelected.GetType())
                    {
                        cselectable.SetPropertyValue(D("Selected"), true);
                    }
                }
            }
            else
            {
                var type = AccessToolsEX.TypeByNameWithDecrypt("ZX.Components.CSelectable");
                type.CallMethod(D("UnSelectAll"));
                foreach (var cselectable in list)
                {
                    cselectable.SetPropertyValue(D("Selected"), true);
                }
            }

            __instance.CallMethod(D("CorrectSelection"));
            __instance.CallMethod(D("UpdateSelection"));

            {
                var type = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXSystem_GameLevel");
                type.CallMethod(D("get_Current")).CallMethod(D("PlaySelectionSound"));
            }

            return false;
        }


        static Fasterflect.MethodInvoker ParamsDelegate = null;
        static Fasterflect.MemberGetter PowerGetter = null;
        static Func<DXEntity, DXEntity, int> GetStrongestFuncCache = null;
        static internal MemberGetter ZXEntityDefaultParamsIDGetter;
        public static Func<DXEntity, DXEntity, int> GetStrongestFunc(DXComponent __instance)
        {
            if (GetStrongestFuncCache == null)
            {
                lock (typeof(GamePatch))
                {
                    if (GetStrongestFuncCache == null)
                    {
                        var cell = ((DXComponent)__instance).Entity.Cell;
                        //ZX.Entities.ZXEntity
                        if (ParamsDelegate == null)
                        {
                            ParamsDelegate = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.ZXEntity").DelegateForCallMethod(D("get_Params"));

                            var ZXEntityDefaultParamsType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXEntityDefaultParams");
                            PowerGetter = ZXEntityDefaultParamsType.DelegateForGetPropertyValue(D("Power"));
                            ZXEntityDefaultParamsIDGetter = ZXEntityDefaultParamsType.DelegateForGetPropertyValue("ID");

                        }

                        GetStrongestFuncCache = new Func<DXEntity, DXEntity, int>((a, b) =>
                        {
                            var aParams = ParamsDelegate(a);
                            var bParams = ParamsDelegate(b);

                            var aPower = (int)PowerGetter(aParams);
                            var bPower = (int)PowerGetter(bParams);

                            string name = (string)ZXEntityDefaultParamsIDGetter(aParams);
                            if (name == "ZombieVenom")
                            {
                                aPower += 1;
                            }

                            name = (string)ZXEntityDefaultParamsIDGetter(bParams);
                            if (name == "ZombieVenom")
                            {
                                bPower += 1;
                            }

                            return (bPower - aPower) * 10000 + a.Cell.DistanceFast(cell) - b.Cell.DistanceFast(cell);
                        });
                    }
                }
            }
            return GetStrongestFuncCache;
        }

        private static object UpdateNearestEnemyLock = new object();
        private static IEnumerable<CodeInstruction> UpdateNearestEnemyTranspilerCache = null;
        internal static IEnumerable<CodeInstruction> OnUpdateNearestEnemyTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            if (UpdateNearestEnemyTranspilerCache == null)
            {
                var newInstructions = new List<CodeInstruction>();
                for (int i = 0; i < instructions.Count(); i++)
                {
                    var instruction = instructions.ElementAt(i);
                    if (i > 157 && i <= 159)
                    {
                        continue;
                    }
                    else if (i == 157)
                    {
                        newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamePatch), nameof(GetStrongestFunc))));
                        continue;
                    }
                    newInstructions.Add(instruction);
                }
                UpdateNearestEnemyTranspilerCache = newInstructions;
            }
            return UpdateNearestEnemyTranspilerCache;
        }

        static MemberGetter TeamGetter = AccessToolsEX.TypeByNameWithDecrypt("ZX.Entities.ZXEntity").DelegateForGetPropertyValue(D("Team"));
        private static Type ZXLevelStateType = AccessToolsEX.TypeByNameWithDecrypt("ZX.ZXLevelState");

        public static bool CheckShowHealthBar(DXComponent clife)
        {
            if (displayAllLifeMetersType == DisplayAllLifeMetersType.All)
                return true;
            if (displayAllLifeMetersType == DisplayAllLifeMetersType.None)
                return false;

            var team = (ZX.ZXTeamType)TeamGetter(clife.Entity);
            if (team == ZX.ZXTeamType.Player && displayAllLifeMetersType == DisplayAllLifeMetersType.Player)
            {
                return true;
            }
            else if (team == ZX.ZXTeamType.Zombie && displayAllLifeMetersType == DisplayAllLifeMetersType.Zombie)
            {
                return true;
            }
            return false;
        }

        internal static IEnumerable<CodeInstruction> OnUpdateSceneObjectParallel(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            var source = instructions.ToList();
            var showHealthBarLabel = source[18].labels.First();

            for (int i = 0; i < instructions.Count(); i++)
            {
                var instruction = instructions.ElementAt(i);
                if (i == 16)
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GamePatch), nameof(CheckShowHealthBar))));
                    newInstructions.Add(new CodeInstruction(OpCodes.Brtrue, showHealthBarLabel));
                }

                newInstructions.Add(instruction);
            }

            return newInstructions;
        }

        internal static void OnTradeCommandExecuted(Object __instance, MethodBase __originalMethod, object[] __args)
        {
            ModEntry.harmonyInstance.Unpatch(__originalMethod, HarmonyPatchType.All, ModEntry.harmonyInstance.Id);
            try
            {
                if (DXInputState.Current.IsKeyPressed(DXKeys.Control))
                {
                    //sell/buy all
                    //
                    while (true)
                    {
                        bool canTrade = (bool)__instance.CallMethod(D("IsEnabledFor"), __args[0]);
                        if (!canTrade)
                        {
                            break;
                        }
                        __instance.CallMethod(D("OnExecute"), __args[0], __args[1]);
                    }
                }
                else if (DXInputState.Current.IsKeyPressed(DXKeys.Shift))
                {
                    DXHelper_Task.ExecuteWithDelay(ModOptions.Instance.QuickBuyResourceDelay, () =>
                    {
                        bool canTrade = (bool)__instance.CallMethod(D("IsEnabledFor"), __args[0]);
                        if (!canTrade)
                        {
                            return;
                        }
                        __instance.CallMethod(D("OnExecute"), __args[0], __args[1]);
                    });
                }
            }
            finally
            {
                // 在循环结束后重新加回补丁
                ModEntry.harmonyInstance.Patch(__originalMethod, new HarmonyMethod(AccessTools.Method(typeof(GamePatch), nameof(OnTradeCommandExecuted))));
            }

        }
    }
}
