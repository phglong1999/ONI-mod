using System.Collections.Generic;
using ONIUtilityTweaks.Settings;
using TUNING;
using UnityEngine;

namespace ONIUtilityTweaks.NaturalConstruction
{
    internal sealed class NaturalTileBuildingConfig : IBuildingConfig
    {
        internal const string ID = "OUT_NaturalTile";
        internal static readonly Tag MaterialReplacementTag =
            new Tag("OUT_NaturalTileMaterial");

        public override BuildingDef CreateBuildingDef()
        {
            ModSettingsData settings = ModSettings.Current;
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                1,
                "out_natural_tile_kanim",
                100,
                30f,
                new[] { (float)settings.DefaultNaturalTileMass },
                new[] { GameTags.Solid.ToString() },
                1600f,
                BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.NONE,
                NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateFoundationTileDef(def);

            // FoundationTile objects are deliberately excluded by the terrain
            // renderer. Keep the building on the normal building layer while
            // retaining IsFoundation so utilities do not request a dig chore.
            def.ObjectLayer = ObjectLayer.Building;
            def.TileLayer = ObjectLayer.NumLayers;
            def.ReplacementCandidateLayers = new List<ObjectLayer>
            {
                ObjectLayer.Building,
                ObjectLayer.FoundationTile
            };
            def.ReplacementTags = new List<Tag>
            {
                MaterialReplacementTag,
                GameTags.FloorTiles
            };
            def.Floodable = false;
            def.Entombable = false;
            def.Overheatable = false;
            def.UseStructureTemperature = false;
            def.ForegroundLayer = Grid.SceneLayer.BuildingBack;
            def.SceneLayer = Grid.SceneLayer.TileMain;
            def.AudioCategory = "HollowMetal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.ConstructionOffsetFilter =
                BuildingDef.ConstructionOffsetFilter_OneDown;
            def.DragBuild = true;
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TILE);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(
                typeof(RequiresFoundation), prefabTag);
            SimCellOccupier occupier = go.AddOrGet<SimCellOccupier>();
            occupier.doReplaceElement = true;
            occupier.strengthMultiplier = 1.5f;
            occupier.notifyOnMelt = true;
            go.AddOrGet<TileTemperature>();
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.AddOrGet<NaturalConstructionMassController>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            GeneratedBuildings.RemoveLoopingSounds(go);
            go.GetComponent<KPrefabID>()?.AddTag(MaterialReplacementTag);
            KBatchedAnimController controller =
                go.GetComponent<KBatchedAnimController>();
            if (controller != null)
                controller.initialBlendParameters = 4;
            go.AddOrGet<NaturalTileMarker>();
        }
    }

    internal sealed class NaturalBackwallBuildingConfig : IBuildingConfig
    {
        internal const string ID = "OUT_NaturalBackwall";

        public override BuildingDef CreateBuildingDef()
        {
            ModSettingsData settings = ModSettings.Current;
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID,
                1,
                1,
                "out_natural_backwall_kanim",
                30,
                30f,
                new[] { (float)settings.DefaultNaturalBackwallMass },
                new[] { GameTags.Solid.ToString() },
                1600f,
                BuildLocationRule.NotInTiles,
                BUILDINGS.DECOR.NONE,
                NOISE_POLLUTION.NONE);
            def.Entombable = false;
            def.Floodable = false;
            def.Overheatable = false;
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.DefaultAnimState = "off";
            def.ObjectLayer = ObjectLayer.Backwall;
            def.SceneLayer = Grid.SceneLayer.Backwall;
            def.PermittedRotations = PermittedRotations.R360;
            def.ReplacementLayer = ObjectLayer.ReplacementBackwall;
            def.ReplacementCandidateLayers = new List<ObjectLayer>
            {
                ObjectLayer.FoundationTile,
                ObjectLayer.Backwall
            };
            def.ReplacementTags = new List<Tag>
            {
                GameTags.FloorTiles,
                GameTags.Backwall
            };
            def.AddSearchTerms(global::STRINGS.SEARCH_TERMS.TILE);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.Backwall;
            go.AddOrGet<ZoneTile>();
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(
                typeof(RequiresFoundation), prefabTag);
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.AddOrGet<NaturalConstructionMassController>();
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            KBatchedAnimController controller =
                go.GetComponent<KBatchedAnimController>();
            if (controller != null)
                controller.initialBlendParameters = 0;
            go.AddOrGet<NaturalBackwallSpawner>();
        }
    }
}
