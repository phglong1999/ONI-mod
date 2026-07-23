using System;
using System.Reflection;
using HarmonyLib;
using KSerialization;
using ONIUtilityTweaks.Settings;
using UnityEngine;

namespace ONIUtilityTweaks.NaturalConstruction
{
    [SerializationConfig(MemberSerialization.OptIn)]
    internal sealed class NaturalTileMarker : KMonoBehaviour, ISaveLoadable
    {
        [Serialize]
        private bool massScaled;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            GameScheduler.Instance.ScheduleNextFrame(
                "Refresh natural tile terrain", _ =>
                {
                    if (this != null && gameObject != null)
                        ShowAsNaturalTerrain();
                });
        }

        internal void PrepareForSimulation()
        {
            PrimaryElement element = GetComponent<PrimaryElement>();
            if (element == null)
                return;

            int cell = Grid.PosToCell(gameObject);
            if (NaturalTileMaterialReplacement.TryTakeLegacyTerrainMass(
                cell, out float replacementMass))
            {
                NaturalConstructionUtility.MovePickupables(cell);
                element.Mass = replacementMass;
                massScaled = true;
            }
            else if (!massScaled)
            {
                NaturalConstructionUtility.MovePickupables(cell);
                element.Mass *= ModSettings.Current
                    .NaturalConstructionMassMultiplier;
                massScaled = true;
            }
        }

        internal void ShowAsNaturalTerrain()
        {
            Building building = GetComponent<Building>();
            if (building != null)
            {
                foreach (int cell in building.PlacementCells)
                {
                    if (!Grid.IsValidCell(cell))
                        continue;

                    // This object stays on the Building layer for terrain
                    // rendering, so preserve the engine's constructed-tile flag.
                    Grid.Foundation[cell] = true;
                    Grid.RenderedByWorld[cell] = true;
                    if (World.Instance != null)
                    {
                        World.Instance.OnSolidChanged?.Invoke(cell);
                        if (World.Instance.groundRenderer != null)
                            World.Instance.groundRenderer.MarkDirty(cell);
                    }
                    if (GameScenePartitioner.Instance != null)
                        GameScenePartitioner.Instance.TriggerEvent(cell,
                            GameScenePartitioner.Instance.solidChangedLayer,
                            null);
                }
            }

            KBatchedAnimController controller =
                GetComponent<KBatchedAnimController>();
            if (controller != null)
            {
                controller.SetVisiblity(false);
                controller.enabled = false;
            }
        }
    }

    internal static class NaturalConstructionUtility
    {
        internal static NaturalTileMarker GetNaturalTile(int cell)
        {
            if (!Grid.IsValidCell(cell))
                return null;

            GameObject tile = Grid.Objects[cell,
                (int)ObjectLayer.Building];
            NaturalTileMarker marker = tile == null ? null :
                tile.GetComponent<NaturalTileMarker>();
            if (marker != null)
                return marker;

            // Supports saves created by the first integrated implementation.
            tile = Grid.Objects[cell, (int)ObjectLayer.FoundationTile];
            return tile == null ? null :
                tile.GetComponent<NaturalTileMarker>();
        }

        internal static bool IsReplaceableTerrainCell(int cell)
        {
            return Grid.IsValidCell(cell) && Grid.Element[cell].IsSolid &&
                Grid.Element[cell].id != SimHashes.Unobtanium &&
                GetNaturalTile(cell) == null;
        }

        internal static bool HasNaturalTile(
            BuildingDef def, int cell, Orientation orientation)
        {
            if (def == null)
                return false;

            foreach (CellOffset offset in def.PlacementOffsets)
            {
                CellOffset rotated = Rotatable.GetRotatedCellOffset(
                    offset, orientation);
                int targetCell = Grid.OffsetCell(cell, rotated);
                if (GetNaturalTile(targetCell) != null)
                    return true;
            }
            return false;
        }

        internal static void MovePickupables(int cell)
        {
            int[] targets =
            {
                Grid.CellAbove(cell),
                Grid.CellBelow(cell),
                Grid.CellRight(cell),
                Grid.CellLeft(cell),
                Grid.CellUpRight(cell),
                Grid.CellDownRight(cell),
                Grid.CellUpLeft(cell),
                Grid.CellDownLeft(cell)
            };
            int target = Grid.InvalidCell;
            foreach (int candidate in targets)
            {
                if (Grid.IsValidCell(candidate) &&
                    !Grid.IsSolidCell(candidate) &&
                    !Grid.Foundation[candidate] && !Grid.HasDoor[candidate])
                {
                    target = candidate;
                    break;
                }
            }
            if (!Grid.IsValidCell(target))
                return;

            GameObject first = Grid.Objects[cell,
                (int)ObjectLayer.Pickupables];
            Pickupable pickupable = first == null ? null :
                first.GetComponent<Pickupable>();
            ObjectLayerListItem current = pickupable == null ? null :
                pickupable.objectLayerListItem;
            Vector3 destination = Grid.CellToPosCCC(target,
                Grid.SceneLayer.Ore);
            while (current != null)
            {
                GameObject content = current.gameObject;
                Pickupable currentPickupable = current.pickupable;
                current = current.nextItem;
                if (content == null || currentPickupable == null ||
                    content.GetComponent<MinionIdentity>() != null)
                    continue;

                content.transform.SetPosition(destination);
                if (currentPickupable.handleFallerComponents)
                {
                    if (GameComps.Fallers.Has(content))
                        GameComps.Fallers.Remove(content);
                    GameComps.Fallers.Add(content, Vector2.up);
                }
            }
        }
    }

    internal sealed class NaturalBackwallSpawner : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();

            PrimaryElement element = GetComponent<PrimaryElement>();
            Building building = GetComponent<Building>();
            KSelectable selectable = GetComponent<KSelectable>();
            if (element == null || building == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Backwall was missing " +
                    "a required component.");
                return;
            }

            if (selectable != null && selectable.IsSelected)
                selectable.Unselect();

            int cell = Grid.PosToCell(this);
            float mass = element.Mass * ModSettings.Current
                .NaturalConstructionMassMultiplier;
            SimMessages.SetBackwallData(cell, element.Element.idx, mass,
                element.Temperature);
            if (selectable != null)
                PopFXManager.Instance.SpawnFX(
                    PopFXManager.Instance.sprite_Building,
                    selectable.GetName(), transform);
            this.DeleteObject();
        }
    }

    internal sealed class NaturalConstructionMassController :
        KMonoBehaviour, ISingleSliderControl
    {
        private static readonly FieldInfo FetchListField = AccessTools.Field(
            typeof(Constructable), "fetchList");
        private static readonly FieldInfo BuildChoreField = AccessTools.Field(
            typeof(Constructable), "buildChore");
        private static readonly FieldInfo MaterialNeedsClearedField =
            AccessTools.Field(typeof(Constructable), "materialNeedsCleared");
        private static readonly MethodInfo OnFetchListCompleteMethod =
            AccessTools.Method(typeof(Constructable), "OnFetchListComplete");

        [Serialize]
        private float naturalMass = -1f;

        [Serialize]
        private bool materialReplacement;

        private Constructable constructable;
        private Storage storage;
        private Building building;
        private float accountedMass;
        private bool needsCleared;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            constructable = GetComponent<Constructable>();
            storage = GetComponent<Storage>();
            building = GetComponent<Building>();
            if (naturalMass < 1f && building != null)
                naturalMass = building.Def.Mass[0];
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (constructable == null)
                constructable = GetComponent<Constructable>();
            if (storage == null)
                storage = GetComponent<Storage>();
            if (building == null)
                building = GetComponent<Building>();
            if (constructable == null || storage == null || building == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Construction mass " +
                    "slider was disabled because a required component is missing.");
                return;
            }

            int cell = Grid.PosToCell(gameObject);
            if (!constructable.IsReplacementTile &&
                building.Def.PrefabID == NaturalTileBuildingConfig.ID &&
                NaturalConstructionUtility.IsReplaceableTerrainCell(cell))
                constructable.IsReplacementTile = true;

            materialReplacement = constructable.IsReplacementTile &&
                building.Def.PrefabID == NaturalTileBuildingConfig.ID;
            if (materialReplacement)
            {
                NaturalTileMarker existing = NaturalConstructionUtility
                    .GetNaturalTile(cell);
                PrimaryElement existingElement = existing == null ? null :
                    existing.GetComponent<PrimaryElement>();
                float existingMass = Grid.Mass[cell];
                if (existingMass <= 0f && existingElement != null)
                    existingMass = existingElement.Mass;
                if (existingMass > 0f)
                    naturalMass = existingMass;
                else
                    materialReplacement = false;
            }

            accountedMass = building.Def.Mass[0];
            needsCleared = false;
            RefreshConstructionTime();
            if (!Mathf.Approximately(naturalMass, accountedMass))
            {
                GameScheduler.Instance.ScheduleNextFrame(
                    "Restore natural construction mass", _ =>
                    {
                        if (this != null && gameObject != null)
                            RebuildFetchList();
                    });
            }
        }

        protected override void OnCleanUp()
        {
            ClearMaterialNeeds();
            base.OnCleanUp();
        }

        internal void ClearMaterialNeeds()
        {
            if (needsCleared || constructable == null)
                return;

            foreach (Recipe.Ingredient ingredient in
                constructable.Recipe.GetAllIngredients(
                    constructable.SelectedElementsTags))
            {
                MaterialNeeds.UpdateNeed(ingredient.tag, -accountedMass,
                    gameObject.GetMyWorldId());
            }
            needsCleared = true;
            MaterialNeedsClearedField?.SetValue(constructable, true);
        }

        private void RebuildFetchList()
        {
            if (constructable == null || storage == null || building == null ||
                FetchListField == null || BuildChoreField == null ||
                MaterialNeedsClearedField == null ||
                OnFetchListCompleteMethod == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Construction could " +
                    "not update the selected mass with this game version.");
                return;
            }

            Chore buildChore = BuildChoreField.GetValue(constructable) as Chore;
            if (buildChore != null)
                buildChore.Cancel("Natural construction mass changed");
            BuildChoreField.SetValue(constructable, null);

            FetchList2 oldFetch = FetchListField.GetValue(constructable) as FetchList2;
            if (oldFetch != null)
                oldFetch.Cancel("Natural construction mass changed");
            storage.DropAll();

            float materialNeedDelta = needsCleared ?
                naturalMass : naturalMass - accountedMass;
            var fetch = new FetchList2(storage, Db.Get().ChoreTypes.BuildFetch);
            foreach (Recipe.Ingredient ingredient in
                constructable.Recipe.GetAllIngredients(
                    constructable.SelectedElementsTags))
            {
                fetch.Add(ingredient.tag, null, naturalMass);
                MaterialNeeds.UpdateNeed(ingredient.tag, materialNeedDelta,
                    gameObject.GetMyWorldId());
            }

            accountedMass = naturalMass;
            needsCleared = false;
            MaterialNeedsClearedField.SetValue(constructable, false);
            FetchListField.SetValue(constructable, fetch);
            fetch.Submit(OnFetchListComplete, true);
            RefreshConstructionTime();
        }

        private void OnFetchListComplete()
        {
            try
            {
                OnFetchListCompleteMethod.Invoke(constructable, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Construction fetch " +
                    "completion failed: " + ex.GetBaseException().Message);
            }
        }

        private void RefreshConstructionTime()
        {
            if (constructable == null || building == null)
                return;

            float workTime = building.Def.ConstructionTime;
            if (ModSettings.Current.ScaleNaturalConstructionTime)
                workTime = Mathf.Clamp(naturalMass / 20f, 5f, 100f);
            if (building.Def.PrefabID == NaturalTileBuildingConfig.ID)
                workTime *= ModSettings.Current.NaturalTileWorkMultiplier;
            constructable.SetWorkTime(workTime);
        }

        internal static void RefreshAllWorkTimes()
        {
            if (Game.Instance == null)
                return;

            foreach (NaturalConstructionMassController controller in
                UnityEngine.Object.FindObjectsByType<NaturalConstructionMassController>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (controller != null && controller.isSpawned)
                    controller.RefreshConstructionTime();
            }
        }

        public string SliderTitleKey =>
            "STRINGS.UI.SANDBOXTOOLS.SETTINGS.MASS.NAME";

        public string SliderUnits =>
            global::STRINGS.UI.UNITSUFFIXES.MASS.KILOGRAM;

        public int SliderDecimalPlaces(int index) => 0;

        public float GetSliderMin(int index) =>
            materialReplacement ? naturalMass : 1f;

        public float GetSliderMax(int index) =>
            materialReplacement ? naturalMass : 2000f;

        public float GetSliderValue(int index) => naturalMass;

        public void SetSliderValue(float value, int index)
        {
            if (materialReplacement)
                return;

            float clamped = Mathf.Clamp(value, GetSliderMin(index),
                GetSliderMax(index));
            if (Mathf.Approximately(clamped, naturalMass))
                return;

            naturalMass = clamped;
            RebuildFetchList();
        }

        public string GetSliderTooltipKey(int index) => null;

        public string GetSliderTooltip(int index) =>
            global::STRINGS.UI.SANDBOXTOOLS.SETTINGS.MASS.TOOLTIP;

        internal float NaturalMass => naturalMass;
    }

    internal static class NaturalTileMaterialReplacement
    {
        private static readonly FieldInfo CachedElementField =
            AccessTools.Field(typeof(PrimaryElement), "_Element");
        private static readonly FieldInfo SimCellOccupierCallDestroyField =
            AccessTools.Field(typeof(SimCellOccupier), "callDestroy");
        private static readonly FieldInfo InitialTemperatureField =
            AccessTools.Field(typeof(Constructable), "initialTemperature");
        private static readonly MethodInfo FinishConstructionMethod =
            AccessTools.Method(typeof(Constructable), "FinishConstruction");
        private static readonly System.Collections.Generic.Dictionary<int, float>
            LegacyTerrainMass =
                new System.Collections.Generic.Dictionary<int, float>();

        internal static bool TryComplete(
            Constructable blueprint, WorkerBase worker)
        {
            Building replacementBuilding = blueprint.GetComponent<Building>();
            if (!blueprint.IsReplacementTile || replacementBuilding?.Def == null ||
                replacementBuilding.Def.PrefabID != NaturalTileBuildingConfig.ID)
                return false;

            int cell = Grid.PosToCell(blueprint.gameObject);
            NaturalTileMarker marker = NaturalConstructionUtility
                .GetNaturalTile(cell);
            Storage replacementStorage = blueprint.GetComponent<Storage>();
            NaturalConstructionMassController massController = blueprint
                .GetComponent<NaturalConstructionMassController>();
            if (replacementStorage == null || massController == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile material " +
                    "replacement was missing a required component.");
                return true;
            }

            if (marker == null)
            {
                GameObject candidate = replacementBuilding.Def
                    .GetReplacementCandidate(cell);
                if (candidate != null && candidate
                    .GetComponent<KPrefabID>()?.HasTag(GameTags.FloorTiles) == true)
                {
                    return ReplaceFloorTileCandidate(
                        blueprint, candidate, massController.NaturalMass,
                        worker);
                }

                if (NaturalConstructionUtility.IsReplaceableTerrainCell(cell))
                {
                    RegisterLegacyTerrainMass(
                        cell, massController.NaturalMass);
                    return false;
                }

                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile replacement " +
                    "could not find the existing tile or terrain cell.");
                return true;
            }

            PrimaryElement oldPrimary = marker.GetComponent<PrimaryElement>();
            if (oldPrimary == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile material " +
                    "replacement was missing the existing PrimaryElement.");
                return true;
            }

            PrimaryElement suppliedMaterial = null;
            foreach (GameObject item in replacementStorage.GetItems())
            {
                suppliedMaterial = item == null ? null :
                    item.GetComponent<PrimaryElement>();
                if (suppliedMaterial != null)
                    break;
            }
            if (suppliedMaterial?.Element == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile material " +
                    "replacement had no delivered material.");
                return true;
            }

            Element oldElement = oldPrimary.Element;
            Element newElement = suppliedMaterial.Element;
            float mass = massController.NaturalMass;
            float oldTemperature = oldPrimary.Temperature;
            byte oldDiseaseIdx = oldPrimary.DiseaseIdx;
            int oldDiseaseCount = oldPrimary.DiseaseCount;
            replacementStorage.ConsumeAndGetDisease(newElement.tag, mass,
                out float consumedMass,
                out Klei.SimUtil.DiseaseInfo diseaseInfo,
                out float newTemperature);
            if (consumedMass + 0.01f < mass)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile material " +
                    $"replacement consumed {consumedMass} kg instead of {mass} kg.");
                return true;
            }

            if (oldElement?.substance != null)
            {
                oldElement.substance.SpawnResource(
                    Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore),
                    mass, oldTemperature, oldDiseaseIdx, oldDiseaseCount);
            }

            CachedElementField?.SetValue(oldPrimary, null);
            oldPrimary.SetElement(newElement.id);
            oldPrimary.Mass = mass;
            oldPrimary.Temperature = newTemperature;

            Deconstructable deconstructable = marker
                .GetComponent<Deconstructable>();
            if (deconstructable != null)
                deconstructable.constructionElements =
                    new[] { newElement.tag };

            SimMessages.ReplaceElement(cell, newElement.id, null, mass,
                newTemperature, diseaseInfo.idx, diseaseInfo.count);
            Grid.Foundation[cell] = true;
            NaturalConstructionUtility.MovePickupables(cell);

            KSelectable selectable = marker.GetComponent<KSelectable>();
            if (selectable != null)
                PopFXManager.Instance.SpawnFX(
                    PopFXManager.Instance.sprite_Building,
                    selectable.GetName(), marker.transform);

            blueprint.gameObject.DeleteObject();
            GameScheduler.Instance.ScheduleNextFrame(
                "Refresh replaced natural tile", _ =>
                {
                    if (marker != null && marker.gameObject != null)
                        marker.ShowAsNaturalTerrain();
                });
            return true;
        }

        private static bool ReplaceFloorTileCandidate(
            Constructable blueprint, GameObject candidate, float mass,
            WorkerBase worker)
        {
            SimCellOccupier occupier = candidate
                .GetComponent<SimCellOccupier>();
            if (occupier == null || SimCellOccupierCallDestroyField == null ||
                InitialTemperatureField == null ||
                FinishConstructionMethod == null)
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile could not " +
                    "replace the existing floor tile without breaking its cell.");
                return true;
            }

            Storage storage = blueprint.GetComponent<Storage>();
            if (!TryGetConstructionTemperature(storage,
                out float initialTemperature))
            {
                Debug.LogWarning("[ONIUtilityTweaks] Natural Tile floor " +
                    "replacement had no valid delivered material temperature.");
                return true;
            }

            int cell = Grid.PosToCell(blueprint.gameObject);
            KAnimGraphTileVisualizer visualizer = blueprint
                .GetComponent<KAnimGraphTileVisualizer>();
            UtilityConnections connections = visualizer == null ?
                (UtilityConnections)0 : visualizer.Connections;
            RegisterLegacyTerrainMass(cell, mass);
            InitialTemperatureField.SetValue(
                blueprint, initialTemperature);
            SimCellOccupierCallDestroyField.SetValue(occupier, false);

            try
            {
                FinishConstructionMethod.Invoke(blueprint,
                    new object[] { connections, worker });
                candidate.DeleteObject();
            }
            catch (Exception ex)
            {
                SimCellOccupierCallDestroyField.SetValue(occupier, true);
                LegacyTerrainMass.Remove(cell);
                Debug.LogError("[ONIUtilityTweaks] Natural Tile floor " +
                    "replacement failed: " + ex.GetBaseException().Message);
            }
            return true;
        }

        private static bool TryGetConstructionTemperature(
            Storage storage, out float temperature)
        {
            float totalMass = 0f;
            float weightedTemperature = 0f;
            bool allLiquifiable = true;
            if (storage != null)
            {
                foreach (GameObject item in storage.GetItems())
                {
                    PrimaryElement element = item == null ? null :
                        item.GetComponent<PrimaryElement>();
                    if (element == null)
                        continue;

                    totalMass += element.Mass;
                    weightedTemperature +=
                        element.Temperature * element.Mass;
                    allLiquifiable &= element.HasTag(GameTags.Liquifiable);
                }
            }

            if (totalMass <= 0f)
            {
                temperature = 0f;
                return false;
            }

            float average = weightedTemperature / totalMass;
            temperature = allLiquifiable ?
                Mathf.Min(average, 318.15f) :
                Mathf.Clamp(average, 0f, 318.15f);
            return temperature > 0f && !float.IsNaN(temperature) &&
                !float.IsInfinity(temperature);
        }

        internal static bool TryTakeLegacyTerrainMass(
            int cell, out float mass)
        {
            if (LegacyTerrainMass.TryGetValue(cell, out mass))
            {
                LegacyTerrainMass.Remove(cell);
                return true;
            }

            mass = 0f;
            return false;
        }

        private static void RegisterLegacyTerrainMass(int cell, float mass)
        {
            LegacyTerrainMass[cell] = mass;
            GameScheduler.Instance.ScheduleNextFrame(
                "Expire legacy Natural Tile replacement", _ =>
                    LegacyTerrainMass.Remove(cell));
        }
    }
}
