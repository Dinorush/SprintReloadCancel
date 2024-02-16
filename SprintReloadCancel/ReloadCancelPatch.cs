using HarmonyLib;
using Player;

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
            AttemptCancelSprint(__instance);
        }

        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void SprintCancelCrouch(PLOC_Crouch __instance)
        {
            AttemptCancelSprint(__instance);
        }

        [HarmonyPatch(typeof(PLOC_Run), nameof(PLOC_Run.Exit))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void SprintWasCanceled(PLOC_Run __instance)
        {
            // If we exit sprinting with a reload, we don't want to cancel it later. But if we exit with a jump,
            // we want to make sure we can still cancel a reload that we started mid-air.
            wasSprinting = __instance.m_owner.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Jump;
        }

        private static bool SprintableItemIsReloading(FirstPersonItemHolder holder)
        {
            return holder != null && holder.WieldedItem != null && holder.WieldedItem.IsReloading && holder.ItemCanMoveQuick();
        }

        private static void AttemptCancelSprint(PLOC_Base ploc)
        {
            PlayerAgent owner = ploc.m_owner;
            if (!PlayerLocomotion.RunInput(owner))
                wasSprinting = false;

            if (wasSprinting || !SprintableItemIsReloading(owner.FPItemHolder) || owner.Locomotion.HasSlowdown || !owner.PlayerCharacterController.IsGrounded)
                return;

            if (PlayerLocomotion.RunInput(owner))
                owner.FPItemHolder.MeleeAttackShortcut();
        }
    }
}
