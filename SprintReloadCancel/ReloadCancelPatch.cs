using HarmonyLib;
using Player;
using System.Collections;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace SprintReloadCancel
{
    internal static class ReloadCancelPatch
    {
        private static bool wasSprinting = false;

        [HarmonyPatch(typeof(PLOC_Stand), nameof(PLOC_Stand.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void SprintCancelStand(PLOC_Stand __instance)
        {
            AttemptReloadCancel(__instance);
        }

        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void SprintCancelCrouch(PLOC_Crouch __instance)
        {
            AttemptReloadCancel(__instance);
        }

        [HarmonyPatch(typeof(PLOC_Run), nameof(PLOC_Run.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SprintWasCanceled(PLOC_Run __instance)
        {
            // If we exit sprinting with a slide, we don't want to cancel a reload later. But if we exit with a jump,
            // we want to make sure we can still cancel a reload that we started mid-air.
            wasSprinting = __instance.m_owner.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Jump;
        }

        private static bool ItemIsReloading(FirstPersonItemHolder holder)
        {
            return holder != null && holder.WieldedItem != null && holder.WieldedItem.IsReloading;
        }

        private static void AttemptReloadCancel(PLOC_Base ploc)
        {
            PlayerAgent owner = ploc.m_owner;
            if (!owner.IsLocallyOwned)
                return;

            if (!PlayerLocomotion.RunInput(owner))
                wasSprinting = false;

            if ((!owner.FPItemHolder?.WieldedItem?.IsReloading ?? true) || !ShouldCancel(owner))
                return;
            
            InventorySlot slot = owner.FPItemHolder.m_inventoryLocal.WieldedSlot;
            owner.FPItemHolder.MeleeAttackShortcut();
            CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(SwapBack(owner, slot)));
        }

        private static bool CanSprint(PlayerAgent owner)
        {
            return owner.FPItemHolder.ItemCanMoveQuick() && !owner.Locomotion.HasSlowdown && owner.PlayerCharacterController.IsGrounded;
        }

        private static bool ShouldCancel(PlayerAgent owner)
        {
            return (Configuration.sprintCancelEnabled && !wasSprinting && CanSprint(owner) && PlayerLocomotion.RunInput(owner))
                || (Configuration.aimCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Aim, owner.InputFilter));
        }

        private static IEnumerator SwapBack(PlayerAgent owner, InventorySlot slot)
        {
            yield return null;
            owner?.Sync.WantsToWieldSlot(slot);
        }
    }
}
