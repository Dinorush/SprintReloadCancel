using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SprintReloadCancel
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.0.0")]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "SprintReloadCancel";

        public override void Load()
        {
            Log.LogMessage("Loading " + MODNAME);

            new Harmony(MODNAME).PatchAll(typeof(ReloadCancelPatch));

            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}