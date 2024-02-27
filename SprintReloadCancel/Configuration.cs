using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;

namespace SprintReloadCancel
{
    internal static class Configuration
    {
        public static bool sprintCancelEnabled = true;
        public static bool aimCancelEnabled = false;
        public static bool shootCancelEnabled = false;
        public static bool swapBuffer = true;

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
            sprintCancelEnabled = (bool)configFile["Base Settings", "Sprint to Reload Cancel"].BoxedValue;
            aimCancelEnabled = (bool)configFile["Base Settings", "Aim to Reload Cancel"].BoxedValue;
            shootCancelEnabled = (bool)configFile["Base Settings", "Shoot to Reload Cancel"].BoxedValue;
            swapBuffer = (bool)configFile["Base Settings", "Reload Cancel Swap Buffer"].BoxedValue;
        }

        private static void BindAll(ConfigFile config)
        {
            sprintCancelEnabled = config.Bind("Base Settings", "Sprint to Reload Cancel", sprintCancelEnabled, "Sprinting will cancel reloads.").Value;
            aimCancelEnabled = config.Bind("Base Settings", "Aim to Reload Cancel", aimCancelEnabled, "Aiming will cancel reloads.").Value;
            shootCancelEnabled = config.Bind("Base Settings", "Shoot to Reload Cancel", shootCancelEnabled, "Shooting will cancel reloads.").Value;
            swapBuffer = config.Bind("Base Settings", "Reload Cancel Swap Buffer", swapBuffer, "After a reload cancel, buffers swap inputs until the next possible time.\nThis can mitigate missed inputs when you attempt to swap weapons right after reload canceling.").Value;
        }
    }
}
