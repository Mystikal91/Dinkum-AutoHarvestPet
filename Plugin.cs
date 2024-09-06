using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace mystikal.dinkum.AutoHarvestPet
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private void Awake()
        {
            // Set global plugin logger
            Plugin.Log = base.Logger;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            // Config
            Config.Bind("!Developer",      // The section under which the option is shown
                    "NexusID",  // The key of the configuration option in the configuration file
                    349, // The default value
                    "Nexus Mod ID"); // Description of the option to show in the config file

            // We Postfix RandomDropFromAnimator.startDayDelay method
            // This method is the same one that handles drops at the start of the day like Chook's eggs
            // This method is called for every FarmAnimal
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            MethodInfo original = AccessTools.Method(typeof(RandomDropFromAnimator), "startDayDelay");
            MethodInfo patch = AccessTools.Method(typeof(Plugin), "startDayDelay_Patch");
            harmony.Patch(original, new HarmonyMethod(patch));
        }

        // We need to have the current RandomDropFromAnimator __instance to take the current FarmAnimal
        private static void startDayDelay_Patch(RandomDropFromAnimator __instance)
        {
            // If farmAnimal is null, just return and do nothing
            if (!(bool)__instance.farmAnimal)
                return;

            // We take FarmAnimal farmAnimal from __instance
            FarmAnimal farmAnimal = __instance.farmAnimal;

            // If this FarmAnimal doesn't have a getHarvestedItem, we return and do nothign
            if (!(bool)farmAnimal.canBeHarvested.getHarvestedItem())
                return;

            if (farmAnimal.canBeHarvested) // If this animal can be harvested...
            {
                farmAnimal.canBeHarvested.harvestFromServer();                  // We harvest the FarmAnimal
                NetworkMapSharer.Instance.spawnAServerDrop(                     // We spawn the item 
                    Inventory.Instance.getInvItemId(
                        farmAnimal.canBeHarvested.getHarvestedItem()),          // The item that should be spawned
                    1,                                                          // Amount
                    farmAnimal.canBeHarvested.transform.position + Vector3.up,  // Position
                    null,                                                       // Building in which the item spawn, in this case is null
                    false,                                                      // tryNotToStack
                    -1);                                                        // If this gives some kind of EXP
            }
        }
    }
}
