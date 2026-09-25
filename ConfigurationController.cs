using System;
using BepInEx.Configuration;

namespace RepoAPI
{
    public class ConfigurationController
    {
        private static ConfigFile ConfigFile { get; set; }
        private static ConfigEntry<bool> Enabled { get; set; }

        public static void Initialize(ConfigFile config)
        {
            ConfigFile = config;

            Enabled = ConfigFile.Bind("General", nameof(Enabled), true, "Enable or disable this mod.");

            ConfigFile.Save();
        }

        public static void Reload()
        {
            ConfigFile.Reload();
            ConfigFile.Save();
        }

    }
}
