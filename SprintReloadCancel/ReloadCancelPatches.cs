using GameData;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;

namespace SprintReloadCancel
{
    [HarmonyPatch]
    internal static class ReloadCancelPatches
    {
        [HarmonyPatch(typeof(ItemEquippable), nameof(ItemEquippable.TryTriggerReloadAnimationSequence))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void TrackReloadTime(ItemEquippable __instance, bool __result)
        {
            if (!__result || __instance.Owner == null || !__instance.Owner.IsLocallyOwned) return;

            // We know a list exists since the function returned true
            int num = __instance.GearPartHolder.FrontData?.ReloadSequence?.Count ?? 0;
            List<WeaponAnimSequenceItem> list = num > 0 ? __instance.GearPartHolder.FrontData!.ReloadSequence 
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

            ReloadCancelHandler.Instance.OnReloadStart(__instance, Clock.Time + largest * timeScale);
        }
    }
}
