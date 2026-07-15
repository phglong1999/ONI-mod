using System;
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
