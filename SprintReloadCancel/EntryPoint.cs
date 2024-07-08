using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SprintReloadCancel
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.3.1")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "SprintReloadCancel";

        public override void Load()
        {
            Log.LogMessage("Loading " + MODNAME);

            new Harmony(MODNAME).PatchAll(typeof(ReloadCancelPatches));
            Configuration.Init();

            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}