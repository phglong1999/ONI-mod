using System.Collections.Generic;

namespace ONIUtilityTweaks.CarePackages
{
    internal static class CarePackageDefaults
    {
        internal const string PrimoGarbId = "CustomClothing";

        internal static CarePackageDefinition[] Create()
        {
            var packages = new List<CarePackageDefinition>
            {
                P("Niobium", 5),
                P("Isoresin", 35),
                P("Fullerene", 1),
                P("Katairite", 1000),
                P("Diamond", 500),
                CreateFossilPackage(),
                P("GeneShufflerRecharge", 1),
                P("SandStone", 1000),
                P("Dirt", 500),
                P("Algae", 500),
                P("OxyRock", 100),
                P("Water", 2000),
                P("Sand", 3000),
                P("Carbon", 3000),
                P("Fertilizer", 3000),
                P("Ice", 4000),
                P("Brine", 2000),
                P("SaltWater", 2000),
                P("Rust", 1000),
                P("Cuprite", 2000),
                P("GoldAmalgam", 2000),
                P("Copper", 400),
                P("Iron", 400),
                P("Lime", 150),
                P("Polypropylene", 500),
                P("Glass", 200),
                P("Steel", 100),
                P("Ethanol", 100),
                P("AluminumOre", 100),
                P("PrickleGrassSeed", 3),
                P("LeafyPlantSeed", 3),
                P("CactusPlantSeed", 3),
                P("MushroomSeed", DlcManager.IsExpansion1Active() ? 3 : 1),
                P("PrickleFlowerSeed", DlcManager.IsExpansion1Active() ? 3 : 2),
                P("OxyfernSeed", 1),
                P("ForestTreeSeed", 1),
                P("BasicFabricMaterialPlantSeed", 3),
                P("SwampLilySeed", 1),
                P("ColdBreatherSeed", 1),
                P("SpiceVineSeed", 1),
                P("FieldRation", 5),
                P("BasicForagePlant", 6),
                P("CookedEgg", 3),
                P("PrickleFruit", 3),
                P("FriedMushroom", 3),
                P("CookedMeat", 3),
                P("SpicyTofu", 3),
                P("LightBugBaby", 1),
                P("HatchBaby", 1),
                P("PuftBaby", 1),
                P("SquirrelBaby", 1),
                P("CrabBaby", 1),
                P("DreckoBaby", 1),
                P("Pacu", 8),
                P("MoleBaby", 1),
                P("OilfloaterBaby", 1),
                P("LightBugEgg", 3),
                P("HatchEgg", 3),
                P("PuftEgg", 3),
                P("OilfloaterEgg", 3),
                P("MoleEgg", 3),
                P("DreckoEgg", 3),
                P("SquirrelEgg", 2),
                P("BasicCure", 3),
                P("Funky_Vest", 1),
                P(PrimoGarbId, 1)
            };
            AddMissing(packages, CreateMetalPackages());

            if (DlcManager.IsExpansion1Active())
            {
                packages.Add(P("ForestForagePlant", 2));
                packages.Add(P("SwampForagePlant", 2));
                packages.Add(P("WormSuperFood", 2));
                packages.Add(P("DivergentBeetleBaby", 1));
                packages.Add(P("StaterpillarBaby", 1));
                packages.Add(P("DivergentBeetleEgg", 2));
                packages.Add(P("StaterpillarEgg", 2));
            }

            return packages.ToArray();
        }

        internal static CarePackageDefinition CreateFossilPackage()
        {
            return P("Fossil", 1000);
        }

        internal static CarePackageDefinition[] CreateMetalPackages()
        {
            return new[]
            {
                // Metal ores
                P("AluminumOre", 100),
                P("Cuprite", 2000),
                P("FoolsGold", 2000),
                P("GoldAmalgam", 2000),
                P("IronOre", 2000),
                P("Cobaltite", 2000),
                P("UraniumOre", 2000),
                P("Wolframite", 2000),
                P("Cinnabar", 2000),
                P("NickelOre", 2000),
                P("ZincOre", 2000),
                P("Galena", 2000),

                // Refined metals
                P("Aluminum", 400),
                P("Copper", 400),
                P("DepletedUranium", 400),
                P("Gold", 400),
                P("Iron", 400),
                P("Cobalt", 400),
                P("Lead", 400),
                P("Niobium", 5),
                P("SolidMercury", 400),
                P("Steel", 100),
                P("TempConductorSolid", 100),
                P("Tungsten", 400),
                P("Nickel", 400),
                P("Iridium", 400),
                P("Zinc", 400)
            };
        }

        internal static void AddMissing(
            List<CarePackageDefinition> packages,
            IEnumerable<CarePackageDefinition> additions)
        {
            var existing = new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);
            foreach (CarePackageDefinition package in packages)
                if (package != null && !string.IsNullOrWhiteSpace(package.Id))
                    existing.Add(package.Id);

            foreach (CarePackageDefinition addition in additions)
                if (addition != null && !string.IsNullOrWhiteSpace(addition.Id) &&
                    existing.Add(addition.Id))
                    packages.Add(addition);
        }

        private static CarePackageDefinition P(string id, float amount)
        {
            return new CarePackageDefinition(id, amount);
        }

        internal static bool IsPrimoGarb(string prefabId)
        {
            return string.Equals(prefabId, PrimoGarbId,
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
