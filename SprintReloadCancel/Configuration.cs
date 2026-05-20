using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;

namespace SprintReloadCancel
{
    internal static class Configuration
    {
        public static bool SprintCancelEnabled = true;
        public static bool AimCancelEnabled = false;
        public static bool ShootCancelEnabled = false;
        public static bool SwapBuffer = true;

        private static ConfigFile configFile;

        internal static void Init()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            BindAll(configFile);
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private static void OnFileChanged(LiveEditEventArgs _)
        {
            configFile.Reload();
            SprintCancelEnabled = (bool)configFile["Base Settings", "Sprint to Reload Cancel"].BoxedValue;
            AimCancelEnabled = (bool)configFile["Base Settings", "Aim to Reload Cancel"].BoxedValue;
            ShootCancelEnabled = (bool)configFile["Base Settings", "Shoot to Reload Cancel"].BoxedValue;
            SwapBuffer = (bool)configFile["Base Settings", "Reload Cancel Swap Buffer"].BoxedValue;
        }

        private static void BindAll(ConfigFile config)
        {
            SprintCancelEnabled = config.Bind("Base Settings", "Sprint to Reload Cancel", SprintCancelEnabled, "Sprinting will cancel reloads.").Value;
            AimCancelEnabled = config.Bind("Base Settings", "Aim to Reload Cancel", AimCancelEnabled, "Aiming will cancel reloads.").Value;
            ShootCancelEnabled = config.Bind("Base Settings", "Shoot to Reload Cancel", ShootCancelEnabled, "Shooting will cancel reloads.").Value;
            SwapBuffer = config.Bind("Base Settings", "Reload Cancel Swap Buffer", SwapBuffer, "After a reload cancel, buffers swap inputs until the next possible time.\nThis can mitigate missed inputs when you attempt to swap weapons right after reload canceling.").Value;
        }
    }
}
