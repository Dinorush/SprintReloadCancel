using GameData;
using HarmonyLib;
using Player;
using System.Collections;
using Il2CppSystem.Collections.Generic;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace SprintReloadCancel
{
    internal static class ReloadCancelPatches
    {
        private static float reloadEndTime = 0;

        [HarmonyPatch(typeof(PLOC_Stand), nameof(PLOC_Stand.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void ReloadCancelStand(PLOC_Stand __instance)
        {
            AttemptReloadCancel(__instance);
        }

        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void ReloadCancelCrouch(PLOC_Crouch __instance)
        {
            AttemptReloadCancel(__instance);
        }

        [HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void ReloadCancelJump(PLOC_Jump __instance)
        {
            AttemptReloadCancel(__instance);
        }

        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.TryTriggerReloadAnimationSequence))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void TrackReloadTime(ItemEquippable __instance, bool __result)
        {
            if (!__result || __instance.Owner == null || !__instance.Owner.IsLocallyOwned) return;

            // We know a list exists since the function returned true
            int num = __instance.GearPartHolder.FrontData?.ReloadSequence?.Count ?? 0;
            List<WeaponAnimSequenceItem>? list = num > 0 ? __instance.GearPartHolder.FrontData.ReloadSequence 
                                                             : __instance.GearPartHolder.StockData.ReloadSequence;
            // The last item in the list is not necessarily the largest, but it does scale the total reload time.
            float triggerTime = list[^1].TriggerTime;
            float timeScale = __instance.ReloadTime / triggerTime;
            float largest = 0;
            foreach(WeaponAnimSequenceItem item in list)
            {
                if (item.TriggerTime > largest)
                    largest = item.TriggerTime;
            }
            reloadEndTime = Clock.Time + largest * timeScale;
        }

        private static void AttemptReloadCancel(PLOC_Base ploc)
        {
            PlayerAgent owner = ploc.m_owner;
            if (!owner.IsLocallyOwned)
                return;

            if ((!owner.FPItemHolder?.WieldedItem?.IsReloading ?? true) || !ShouldCancel(owner))
                return;
            
            InventorySlot slot = owner.FPItemHolder.m_inventoryLocal.WieldedSlot;
            owner.FPItemHolder.MeleeAttackShortcut();
            CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(SwapBack(owner, slot)));
        }

        private static bool ShouldCancel(PlayerAgent owner)
        {
            return (Configuration.sprintCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Run, owner.InputFilter))
                || (reloadEndTime - 0.2f > Clock.Time && ( // Avoid canceling reloads that are almost done (e.g. spamming shoot as it finishes)
                      (Configuration.aimCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Aim, owner.InputFilter))
                   || (Configuration.shootCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Fire, owner.InputFilter))
                   ));
        }

        private static IEnumerator SwapBack(PlayerAgent owner, InventorySlot slot)
        {
            yield return null;
            owner?.Sync.WantsToWieldSlot(slot);
        }
    }
}
