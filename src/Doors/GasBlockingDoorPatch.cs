using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using PeterHan.PLib.PatchManager;
using UnityEngine;

namespace ONIUtilityTweaks.Doors
{
    internal static class GasBlockingDoorPatchBootstrap
    {
        private const string HarmonyId =
            "phglong1999.ONIUtilityTweaks.GasBlockingDoors";

        private static bool installed;

        [PLibMethod(RunAt.AfterDbInit)]
        internal static void Install()
        {
            if (installed)
                return;

            KAnimFile doorInteraction = Assets.GetAnim("anim_use_remote_kanim");
            if (doorInteraction == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Gas-Sealing Doors was disabled " +
                    "because anim_use_remote_kanim is not loaded.");
                return;
            }

            MethodInfo setSimState = AccessTools.Method(typeof(Door), "SetSimState");
            MethodInfo setWorldState = AccessTools.Method(typeof(Door), "SetWorldState");
            MethodInfo onCleanUp = AccessTools.Method(typeof(Door), "OnCleanUp");
            MethodInfo updateFalling = AccessTools.Method(
                typeof(FallMonitor.Instance), nameof(FallMonitor.Instance.UpdateFalling));
            if (setSimState == null || setWorldState == null || onCleanUp == null ||
                updateFalling == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Gas-Sealing Doors could not find " +
                    "the required game methods and was not installed.");
                return;
            }

            var harmony = new Harmony(HarmonyId);
            harmony.Patch(setSimState, prefix: new HarmonyMethod(
                typeof(DoorSetSimStatePatch), nameof(DoorSetSimStatePatch.Prefix)));
            harmony.Patch(setWorldState, postfix: new HarmonyMethod(
                typeof(DoorSetWorldStatePatch), nameof(DoorSetWorldStatePatch.Postfix)));
            harmony.Patch(onCleanUp, postfix: new HarmonyMethod(
                typeof(DoorOnCleanUpPatch), nameof(DoorOnCleanUpPatch.Postfix)));
            harmony.Patch(updateFalling, transpiler: new HarmonyMethod(
                typeof(FallMonitorUpdateFallingPatch),
                nameof(FallMonitorUpdateFallingPatch.Transpiler)));
            installed = true;
            Debug.Log("[ONIUtilityTweaks] Gas-Sealing Doors patches installed after Db.Initialize.");
        }
    }

    internal static class GasBlockingDoors
    {
        private static readonly MethodInfo setWorldState =
            AccessTools.Method(typeof(Door), "SetWorldState");

        internal static bool Enabled => ModSettings.Current.EnableGasBlockingDoors;

        internal static bool IsSupportedDoor(Door door)
        {
            return door != null && (door.doorType == Door.DoorType.ManualPressure ||
                door.doorType == Door.DoorType.Pressure);
        }

        internal static bool IsPassableDoorCell(int cell)
        {
            if (!Enabled || !Grid.IsValidCell(cell) || !Grid.DupePassable[cell])
                return false;

            GameObject building = Grid.Objects[cell, (int)ObjectLayer.Building];
            return building != null && IsSupportedDoor(building.GetComponent<Door>());
        }

        internal static void ApplyWorldState(Door door)
        {
            if (!Enabled || !IsSupportedDoor(door) || door.building == null ||
                Game.Instance == null)
                return;

            bool passable = door.CurrentState != Door.ControlState.Locked;
            foreach (int cell in door.building.PlacementCells)
            {
                if (!Grid.IsValidCell(cell))
                    continue;

                Game.Instance.SetDupePassableSolid(cell, passable, solid: true);
                Pathfinding.Instance.AddDirtyNavGridCell(cell);
            }
        }

        internal static void ClearWorldState(Door door)
        {
            if (!IsSupportedDoor(door) || door.building == null || Game.Instance == null)
                return;

            foreach (int cell in door.building.PlacementCells)
                if (Grid.IsValidCell(cell))
                    Game.Instance.SetDupePassableSolid(cell, passable: false, solid: false);
        }

        internal static void RefreshAll()
        {
            if (Game.Instance == null || setWorldState == null)
                return;

            foreach (Door door in Object.FindObjectsByType<Door>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!IsSupportedDoor(door) || !door.isSpawned || door.building == null)
                    continue;

                try
                {
                    if (!Enabled)
                        ClearWorldState(door);
                    setWorldState.Invoke(door, new object[] { true });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[ONIUtilityTweaks] Could not refresh door '" +
                        door.name + "': " + ex.Message);
                }
            }
        }
    }

    internal static class DoorSetSimStatePatch
    {
        internal static void Prefix(Door __instance, ref bool is_door_open)
        {
            if (GasBlockingDoors.Enabled &&
                GasBlockingDoors.IsSupportedDoor(__instance))
                is_door_open = false;
        }
    }

    internal static class DoorSetWorldStatePatch
    {
        internal static void Postfix(Door __instance)
        {
            GasBlockingDoors.ApplyWorldState(__instance);
        }
    }

    internal static class DoorOnCleanUpPatch
    {
        internal static void Postfix(Door __instance)
        {
            if (GasBlockingDoors.Enabled &&
                GasBlockingDoors.IsSupportedDoor(__instance))
                GasBlockingDoors.ClearWorldState(__instance);
        }
    }

    internal static class FallMonitorUpdateFallingPatch
    {
        private static bool IsSolidUnlessPassableDoor(
            ref Grid.BuildFlagsSolidIndexer _,
            int cell)
        {
            return Grid.Solid[cell] && !GasBlockingDoors.IsPassableDoorCell(cell);
        }

        internal static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo solidGetter = AccessTools.PropertyGetter(
                typeof(Grid.BuildFlagsSolidIndexer), "Item");
            MethodInfo replacement = AccessTools.Method(
                typeof(FallMonitorUpdateFallingPatch),
                nameof(IsSolidUnlessPassableDoor));

            foreach (CodeInstruction instruction in instructions)
            {
                if (solidGetter != null && instruction.Calls(solidGetter))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }
    }
}
