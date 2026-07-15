using UnityEngine;

namespace ONIUtilityTweaks.NaturalConstruction
{
    internal static class NaturalTilePlanting
    {
        internal static bool TryGetSupportCell(
            PlantableSeed seed,
            int plantCell,
            out int supportCell)
        {
            supportCell = Grid.InvalidCell;
            if (seed == null || !Grid.IsValidCell(plantCell))
                return false;

            supportCell = seed.Direction !=
                SingleEntityReceptacle.ReceptacleDirection.Bottom ?
                Grid.CellBelow(plantCell) : Grid.CellAbove(plantCell);
            return NaturalConstructionUtility.GetNaturalTile(supportCell) != null;
        }

        internal static bool CanPlantAt(
            PlantableSeed seed,
            int plantCell,
            int supportCell)
        {
            return Grid.IsValidCell(supportCell) && Grid.Solid[supportCell] &&
                IsSuitableGround(seed, plantCell, supportCell);
        }

        internal static bool IsSuitableGround(
            PlantableSeed seed,
            int plantCell,
            int supportCell)
        {
            if (seed == null || !Grid.IsValidCell(plantCell) ||
                !Grid.IsValidCell(supportCell))
                return false;
            if (Grid.Element[supportCell].hardness >= 150)
                return false;
            if (seed.replantGroundTag.IsValid &&
                !Grid.Element[supportCell].HasTag(seed.replantGroundTag))
                return false;

            GameObject prefab = Assets.GetPrefab(seed.PlantID);
            if (prefab == null)
                return false;
            EntombVulnerable entomb = prefab.GetComponent<EntombVulnerable>();
            if (entomb != null && !entomb.IsCellSafe(plantCell))
                return false;
            DrowningMonitor drowning = prefab.GetComponent<DrowningMonitor>();
            if (drowning != null && !drowning.IsCellSafe(plantCell))
                return false;
            TemperatureVulnerable temperature =
                prefab.GetComponent<TemperatureVulnerable>();
            if (temperature != null && !temperature.IsCellSafe(plantCell) &&
                Grid.Element[plantCell].id != SimHashes.Vacuum)
                return false;
            UprootedMonitor uprooted = prefab.GetComponent<UprootedMonitor>();
            if (uprooted != null && !uprooted.IsSuitableFoundation(plantCell))
                return false;
            OccupyArea area = prefab.GetComponent<OccupyArea>();
            return area == null || area.CanOccupyArea(plantCell,
                ObjectLayer.Building);
        }
    }
}
