using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SprintReloadCancel
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.3.4")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "SprintReloadCancel";

        public override void Load()
        {
            new Harmony(MODNAME).PatchAll();
            Configuration.Init();

            AssetAPI.OnStartupAssetsLoaded += () =>
            {
                ClassInjector.RegisterTypeInIl2Cpp<ReloadCancelHandler>();
                var go = new GameObject(MODNAME);
                GameObject.DontDestroyOnLoad(go);
                go.AddComponent<ReloadCancelHandler>();
            };

            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}