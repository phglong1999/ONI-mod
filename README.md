# ONI Utility Tweaks

Simple Oxygen Not Included mod that:

- Adds a PLib-based Saved Schedules dialog to the in-game Schedule screen.
- Saves all schedules or selected schedules into named templates.
- Loads or removes saved schedule templates from another colony.
- Mirrors schedule templates into ONI's `cloud_save_files` folder when available.
- Optionally unlocks all Biome Remix selections.
- Configures a quantity multiplier for all custom Printing Pod Care Packages.
- Integrates configurable Care Package Mod rosters, package lists, rerolls, and Duplicant generation.
- Optionally adds material separation recipes to the existing Crafting Station.
- Optionally keeps manual and mechanized airlocks sealed against gas and liquid while open.
- Integrates Natural Tile and Natural Backwall construction with configurable mass.
- Keeps integrated Natural Tiles intact when liquid pipes, gas pipes, or other utilities are built through them.
- Uses PLib Options for the Mods screen settings dialog.

Saved Schedules is enabled by default. All other optional tweaks are disabled
until enabled from the mod's Options dialog.

## Use Schedule Templates

1. Open a colony and go to the Schedule screen.
2. Click `Saved`.
3. Leave all schedules checked, or uncheck schedules you do not want to save.
4. Enter a template name.
5. Click `Save All` or `Save Selected`.
6. In another colony, open `Saved`, select the saved template, then click `Load`.

## Build

This project bundles PLib from Peter Han's ONIMods source tree under
`tools/ONIMods-src`. The build script compiles `PLibCore`, `PLibUI`, and
`PLibOptions` directly into `ONIUtilityTweaks.dll`, so players do not need to
install PLib separately.

```powershell
.\build.ps1
```

The build output is written to `dist`.

## Install locally

Copy the built files from `dist` into a local ONI mod folder, for example:

```text
Documents\Klei\OxygenNotIncluded\mods\local\ONIUtilityTweaks
```

Then enable the mod in-game.

## Third-party work

The Natural Construction logic and animation assets are adapted from
[Natural Construction](https://github.com/Sgt-Imalas/Sgt_Imalas-Oni-Mods/tree/master/NaturalConstruction)
by SGT_Imalas under the MIT License. See `THIRD_PARTY_NOTICES.md`.
