using System;
using System.Reflection;
using HarmonyLib;
using ONIUtilityTweaks.Settings;
using UnityEngine;

namespace ONIUtilityTweaks.NaturalConstruction
{
    [HarmonyPatch(typeof(GeneratedBuildings),
        nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    internal static class NaturalConstructionBuildingRegistrationPatch
    {
        private const string ExternalNaturalTileId = "NC_NaturalTile";
        private const string ExternalNaturalBackwallId = "NC_NaturalBackwall";

        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            if (!ModSettings.Current.EnableNaturalConstruction)
                return;

            RemovePlanEntries(NaturalTileBuildingConfig.ID,
                NaturalBackwallBuildingConfig.ID,
                ExternalNaturalTileId, ExternalNaturalBackwallId);
            TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[
                NaturalTileBuildingConfig.ID] = "tiles";
            TUNING.BUILDINGS.PLANSUBCATEGORYSORTING[
                NaturalBackwallBuildingConfig.ID] = "tiles";
            ModUtil.AddBuildingToPlanScreen(new HashedString("Base"),
                NaturalTileBuildingConfig.ID, "tiles", TileConfig.ID,
                ModUtil.BuildingOrdering.After);
            ModUtil.AddBuildingToPlanScreen(new HashedString("Base"),
                NaturalBackwallBuildingConfig.ID, "tiles",
                ExteriorWallConfig.ID, ModUtil.BuildingOrdering.After);

            Tech tech = Db.Get().Techs.Get("FarmingTech");
            if (tech != null)
            {
                tech.unlockedItemIDs.RemoveAll(id =>
                    id == NaturalTileBuildingConfig.ID ||
                    id == NaturalBackwallBuildingConfig.ID ||
                    id == ExternalNaturalTileId ||
                    id == ExternalNaturalBackwallId);
                tech.AddUnlockedItemIDs(NaturalTileBuildingConfig.ID,
                    NaturalBackwallBuildingConfig.ID);
            }

            if (AccessTools.TypeByName(
                "NaturalConstruction.Content.Defs.NaturalTileBuildingConfig") != null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] The Steam Natural " +
                    "Construction mod is also enabled. Its build-menu entries " +
                    "were replaced by the integrated fixed versions.");
            }
        }

        private static void RemovePlanEntries(params string[] ids)
        {
            foreach (PlanScreen.PlanInfo info in TUNING.BUILDINGS.PLANORDER)
            {
                info.buildingAndSubcategoryData.RemoveAll(entry =>
                    Array.IndexOf(ids, entry.Key) >= 0);
            }
        }
    }

    [HarmonyPatch(typeof(SimCellOccupier), "OnSpawn")]
    internal static class NaturalTileSimulationPatch
    {
        private static void Prefix(SimCellOccupier __instance)
        {
            NaturalTileMarker marker =
                __instance.GetComponent<NaturalTileMarker>();
            marker?.PrepareForSimulation();
        }
    }

    [HarmonyPatch(typeof(SimCellOccupier), "OnModifyComplete")]
    internal static class NaturalTileSimulationCompletePatch
    {
        private static void Postfix(SimCellOccupier __instance)
        {
            NaturalTileMarker marker =
                __instance.GetComponent<NaturalTileMarker>();
            marker?.ShowAsNaturalTerrain();
        }
    }

    [HarmonyPatch(typeof(Constructable), "ClearMaterialNeeds")]
    internal static class NaturalConstructionMaterialNeedsPatch
    {
        private static bool Prefix(Constructable __instance)
        {
            NaturalConstructionMassController controller =
                __instance.GetComponent<NaturalConstructionMassController>();
            if (controller == null)
                return true;

            controller.ClearMaterialNeeds();
            return false;
        }
    }

    [HarmonyPatch(typeof(Constructable), "OnSpawn")]
    internal static class NaturalTileLegacyTerrainPlacementPatch
    {
        private static void Prefix(Constructable __instance)
        {
            if (__instance.IsReplacementTile)
                return;

            Building building = __instance.GetComponent<Building>();
            if (building?.Def == null ||
                building.Def.PrefabID != NaturalTileBuildingConfig.ID)
                return;

            int cell = Grid.PosToCell(__instance.gameObject);
            if (NaturalConstructionUtility.IsReplaceableTerrainCell(cell))
                __instance.IsReplacementTile = true;
        }
    }

    [HarmonyPatch(typeof(Constructable), "OnCompleteWork")]
    internal static class NaturalTileMaterialReplacementPatch
    {
        private static bool Prefix(
            Constructable __instance, WorkerBase worker)
        {
            return !NaturalTileMaterialReplacement.TryComplete(
                __instance, worker);
        }
    }

    [HarmonyPatch(typeof(Constructable), "IsCellDigRequired")]
    internal static class NaturalTileUtilityDigPatch
    {
        private static void Postfix(
            Constructable __instance, int offset_cell, ref bool __result)
        {
            if (!__result)
                return;

            Building building = __instance.GetComponent<Building>();
            if (building?.Def != null && building.Def.isUtility &&
                NaturalConstructionUtility.GetNaturalTile(offset_cell) != null)
                __result = false;
        }
    }

    [HarmonyPatch]
    internal static class NaturalTileNotInTilesPlacementPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BuildingDef),
                nameof(BuildingDef.IsValidPlaceLocation), new[]
                {
                    typeof(GameObject), typeof(int), typeof(Orientation),
                    typeof(bool), typeof(string).MakeByRefType(), typeof(bool)
                });
        }

        private static void Postfix(BuildingDef __instance, int cell,
            Orientation orientation, ref string fail_reason,
            ref bool __result)
        {
            BlockNotInTiles(__instance, cell, orientation,
                ref fail_reason, ref __result);
        }

        internal static void BlockNotInTiles(BuildingDef def, int cell,
            Orientation orientation, ref string failReason, ref bool result)
        {
            if (!result || def.BuildLocationRule != BuildLocationRule.NotInTiles ||
                !NaturalConstructionUtility.HasNaturalTile(
                    def, cell, orientation))
                return;

            result = false;
            failReason = global::STRINGS.UI.TOOLTIPS
                .HELP_BUILDLOCATION_NOT_IN_TILES;
        }
    }

    [HarmonyPatch]
    internal static class NaturalTileNotInTilesBuildPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BuildingDef),
                nameof(BuildingDef.IsValidBuildLocation), new[]
                {
                    typeof(GameObject), typeof(int), typeof(Orientation),
                    typeof(bool), typeof(string).MakeByRefType()
                });
        }

        private static void Postfix(BuildingDef __instance, int cell,
            Orientation orientation, ref string fail_reason,
            ref bool __result)
        {
            NaturalTileNotInTilesPlacementPatch.BlockNotInTiles(
                __instance, cell, orientation,
                ref fail_reason, ref __result);
        }
    }

    [HarmonyPatch(typeof(PlantableSeed), nameof(PlantableSeed.TestSuitableGround))]
    internal static class NaturalTilePlantingPatch
    {
        private static bool Prefix(PlantableSeed __instance, int cell,
            ref bool __result)
        {
            if (!NaturalTilePlanting.TryGetSupportCell(
                __instance, cell, out int supportCell))
                return true;

            __result = NaturalTilePlanting.IsSuitableGround(
                __instance, cell, supportCell);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlantableCellQuery), "CheckValidPlotCell")]
    internal static class NaturalTilePlantableCellQueryPatch
    {
        private static bool Prefix(
            PlantableSeed seed,
            int plant_cell,
            int ___plantDetectionRadius,
            int ___maxPlantsInRadius,
            ref bool __result)
        {
            if (!NaturalTilePlanting.TryGetSupportCell(
                seed, plant_cell, out int supportCell))
                return true;

            __result = NaturalTilePlanting.CanPlantAt(
                    seed, plant_cell, supportCell) &&
                Grid.Objects[plant_cell, (int)ObjectLayer.Plants] == null &&
                Grid.Objects[plant_cell, (int)ObjectLayer.Building] == null &&
                CountNearbyPlants(supportCell, ___plantDetectionRadius) <=
                    ___maxPlantsInRadius;
            return false;
        }

        private static int CountNearbyPlants(int cell, int radius)
        {
            Vector2I coordinates = Grid.PosToXY(Grid.CellToPos(cell));
            int diameter = radius * 2;
            var entries = ListPool<ScenePartitionerEntry,
                GameScenePartitioner>.Allocate();
            GameScenePartitioner.Instance.GatherEntries(
                coordinates.x - radius,
                coordinates.y - radius,
                diameter,
                diameter,
                GameScenePartitioner.Instance.plants,
                entries);
            int count = 0;
            foreach (ScenePartitionerEntry entry in entries)
            {
                KPrefabID prefab = entry.obj as KPrefabID;
                if (prefab != null && prefab.GetComponent<TreeBud>() == null)
                    count++;
            }
            entries.Recycle();
            return count;
        }
    }

    [HarmonyPatch(typeof(SeedPlantingStates), "CheckValidPlotCell")]
    internal static class NaturalTilePlantingCompletionPatch
    {
        private static bool Prefix(
            PlantableSeed seed,
            int cell,
            ref PlantablePlot plot,
            ref bool __result)
        {
            if (!NaturalTilePlanting.TryGetSupportCell(
                seed, cell, out int supportCell))
                return true;

            plot = null;
            __result = NaturalTilePlanting.CanPlantAt(
                seed, cell, supportCell);
            return false;
        }
    }
}
