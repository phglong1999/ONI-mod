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
            if (!massScaled && element != null)
            {
                NaturalConstructionUtility.MovePickupables(
                    Grid.PosToCell(gameObject));
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
            constructable.SetWorkTime(workTime);
        }

        public string SliderTitleKey =>
            "STRINGS.UI.SANDBOXTOOLS.SETTINGS.MASS.NAME";

        public string SliderUnits =>
            global::STRINGS.UI.UNITSUFFIXES.MASS.KILOGRAM;

        public int SliderDecimalPlaces(int index) => 0;

        public float GetSliderMin(int index) => 1f;

        public float GetSliderMax(int index) => 2000f;

        public float GetSliderValue(int index) => naturalMass;

        public void SetSliderValue(float value, int index)
        {
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
    }
}
