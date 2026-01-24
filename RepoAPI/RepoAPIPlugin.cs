using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RepoAPI
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class RepoAPIPlugin : BaseUnityPlugin
    {
        // Se reemplaza osmarbriones por el parámetro AuthorId del template.json
        // y RepoAPI por el nombre del proyecto (sourceName).
        public const string PluginGuid = "osmarbriones.RepoAPI";
        public const string PluginName = "RepoAPI";
        public const string PluginVersion = "1.0.0";

        internal Harmony Harmony { get; set; }
        internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;

            // Prevent the plugin from being deleted
            this.gameObject.transform.parent = null;
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;

            ConfigurationController.Initialize(this.Config);

            Harmony = new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");
        }

        internal void Unpatch()
        {
            Harmony.UnpatchSelf();
        }
    }
}
