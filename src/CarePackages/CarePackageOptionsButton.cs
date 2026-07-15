using ONIUtilityTweaks.Settings;
using PeterHan.PLib.Options;
using UnityEngine;

namespace ONIUtilityTweaks.CarePackages
{
    internal sealed class CarePackageOptionsButton : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenu);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenu);
            base.OnCleanUp();
        }

        private void OnRefreshUserMenu(object _)
        {
            Game.Instance.userMenu?.AddButton(gameObject,
                new KIconButtonMenu.ButtonInfo(
                    "action_building_disabled",
                    "Care Package Options",
                    OpenOptions,
                    Action.NumActions,
                    null,
                    null,
                    null,
                    "Configure Printing Pod rosters, packages, and Duplicant generation."));
        }

        private static void OpenOptions()
        {
            POptions.ShowDialog(typeof(ModSettingsData), _ => CarePackageManager.Invalidate());
        }
    }
}
