// Based on code by TheOriginOfAllEvil and Pr0SkyNesis
// https://github.com/TheOriginOfAllEvil/Shattered-Seas-Cleaner
//
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;

namespace SaveCleaner
{
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.nandbrew.savecleaner";
        public const string PLUGIN_NAME = "Save Cleaner";
        public const string PLUGIN_VERSION = "0.3.1";

        //--settings--
        internal static ConfigEntry<bool> overWrite;

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_ID);

            overWrite = Config.Bind("", "Overwrite file", false, new ConfigDescription("Overwrite save file. (else cleans loaded data, must be saved normally)"));
        }
    }
}
