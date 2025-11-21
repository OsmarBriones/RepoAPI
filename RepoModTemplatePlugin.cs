using BepInEx;
using UnityEngine;

namespace RepoModTemplate
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class RepoModTemplatePlugin : BaseUnityPlugin
    {
        // Se reemplaza AUTHOR_ID por el parámetro AuthorId del template.json
        // y RepoModTemplate por el nombre del proyecto (sourceName).
        public const string PluginGuid = "AUTHOR_ID.RepoModTemplate";
        public const string PluginName = "RepoModTemplate";
        public const string PluginVersion = "1.0.0";

        internal static new BepInEx.Logging.ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"{PluginName} {PluginVersion} loaded!");

            // TODO: aquí puedes poner código base común para tus mods
            // por ejemplo, inicializar Harmony, logs, config, etc.
        }
    }
}
