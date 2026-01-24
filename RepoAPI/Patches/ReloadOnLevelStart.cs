using System;
using HarmonyLib;

namespace RepoAPI.Patches
{
    [HarmonyPatch(typeof(EnemyDirector), "Start")]
    internal class ReloadOnLevelStart
    {
        static void Postfix()
        {
            if (!SemiFunc.RunIsLevel()) return;
            ConfigurationController.Reload();
        }
    }
}
