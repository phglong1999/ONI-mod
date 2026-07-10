# ONI Utility Tweaks

Simple Oxygen Not Included mod that:

- Adds a PLib-based Schedule Templates dialog to the in-game Schedule screen.
- Saves all schedules or selected schedules into named templates.
- Loads or removes saved schedule templates from another colony.
- Mirrors schedule templates into ONI's `cloud_save_files` folder when available.
- Uses PLib Options for the Mods screen settings dialog.

## Use Schedule Templates

1. Open a colony and go to the Schedule screen.
2. Click `Templates`.
3. Leave all schedules checked, or uncheck schedules you do not want to save.
4. Enter a template name.
5. Click `Save All` or `Save Selected`.
6. In another colony, open `Templates`, select the saved template, then click `Load`.

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
