﻿using GameData;
using HarmonyLib;
using Player;
using System.Collections;
using Il2CppSystem.Collections.Generic;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace SprintReloadCancel
{
    internal static class ReloadCancelPatches
    {
        private const float SwapTime = 0.2f;
        private static readonly System.Collections.Generic.Dictionary<InputAction, InventorySlot> SwapActions = new() {
            { InputAction.SelectStandard, InventorySlot.GearStandard },
            { InputAction.SelectSpecial, InventorySlot.GearSpecial },
            { InputAction.SelectTool, InventorySlot.GearClass },
            { InputAction.SelectMelee, InventorySlot.GearMelee },
            { InputAction.SelectConsumable, InventorySlot.Consumable },
            { InputAction.SelectHackingTool, InventorySlot.HackingTool },
            { InputAction.SelectResourcePack, InventorySlot.ResourcePack },
        };
        private static float _reloadEndTime = 0;
        private static bool _aimWasDown = false;

        [HarmonyPatch(typeof(PLOC_Stand), nameof(PLOC_Stand.Update))]
        [HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Update))]
        [HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.Update))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void ReloadCancelStand(PLOC_Stand __instance)
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
            _reloadEndTime = Clock.Time + largest * timeScale;
        }

        private static void AttemptReloadCancel(PLOC_Base ploc)
        {
            PlayerAgent owner = ploc.m_owner;
            if (!owner.IsLocallyOwned)
                return;

            bool aimDown = InputMapper.GetButton.Invoke(InputAction.Aim, owner.InputFilter);
            bool aimResult = aimDown && !_aimWasDown;
            _aimWasDown = aimDown;
            if ((!owner.FPItemHolder?.WieldedItem?.IsReloading ?? true) || !ShouldCancel(owner, aimResult))
                return;
            
            InventorySlot slot = owner.FPItemHolder.m_inventoryLocal.WieldedSlot;
            owner.FPItemHolder.MeleeAttackShortcut();
            CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(SwapBack(owner, slot)));
        }

        private static bool ShouldCancel(PlayerAgent owner, bool aimDown)
        {
            return (Configuration.sprintCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Run, owner.InputFilter) && owner.Locomotion.InputIsForwardEnoughForRun())
                || (_reloadEndTime - SwapTime > Clock.Time && ( // Avoid canceling reloads that are almost done (e.g. spamming shoot as it finishes)
                      (Configuration.aimCancelEnabled && aimDown)
                   || (Configuration.shootCancelEnabled && InputMapper.GetButtonDown.Invoke(InputAction.Fire, owner.InputFilter))
                   ));
        }

        private static IEnumerator SwapBack(PlayerAgent owner, InventorySlot slot)
        {
            yield return null;
            owner?.Sync.WantsToWieldSlot(slot);

            if (Configuration.swapBuffer)
                CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(SwapBuffer(owner)));
        }

        private static IEnumerator SwapBuffer(PlayerAgent? owner)
        {
            // Buffer mechanic. Wait until the buffer time is long enough to actually catch swaps
            float endTime = Clock.Time + SwapTime;
            InventorySlot bufferedSlot = InventorySlot.None;
            bool bufferedPush = false;
            // Check for swap attempts until we can swap again
            while(Clock.Time < endTime && owner != null)
            {
                foreach (var pair in SwapActions)
                {
                    if (InputMapper.GetButtonDown.Invoke(pair.Key, owner.InputFilter))
                    {
                        bufferedSlot = pair.Value;
                        bufferedPush = false;
                    }
                }
                    
                if (InputMapper.GetButtonDown.Invoke(InputAction.Melee, owner.InputFilter))
                    bufferedPush = true;

                yield return null;
            }

            if (owner != null && owner.FPItemHolder.m_inventoryLocal.WieldedSlot != InventorySlot.InLevelCarry)
            {
                if (bufferedPush)
                    owner.FPItemHolder.MeleeAttackShortcut();
                else if (bufferedSlot != InventorySlot.None)
                    owner.Sync.WantsToWieldSlot(bufferedSlot);
            }
        }
    }
}
