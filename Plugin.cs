// Based on code by TheOriginOfAllEvil and Pr0SkyNesis
// https://github.com/TheOriginOfAllEvil/Shattered-Seas-Cleaner
//
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        public const string PLUGIN_VERSION = "0.3.5";

        //--settings--
        internal static ConfigEntry<bool> overWrite;
        internal static ConfigEntry<bool> verbose;

        internal static ManualLogSource logSource;

        private void Awake()
        {
            logSource = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_ID);

            overWrite = Config.Bind("", "Overwrite file", false, new ConfigDescription("Overwrite save file. (else cleans loaded data, must be saved normally)"));
            verbose = Config.Bind("", "Verbose logging", false, new ConfigDescription(""));
        }
    }
}
