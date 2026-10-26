using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace SaveCleaner
{
    [HarmonyPatch(typeof(SaveLoadManager))]
    internal static class Patches
    {
        [HarmonyPatch("LoadGame")]
        [HarmonyPrefix]
        private static void LoadGamePatch(int backupIndex)
        {   //removes all references to the modded boat before the game loads
            if (Plugin.overWrite.Value == false) return;

            //Plugin.logSource.LogInfo("Save Cleaner Running");
            Plugin.logSource.LogInfo("Cleaning Slot " + SaveSlots.currentSlot);
            string path = ((backupIndex != 0) ? SaveSlots.GetBackupPath(SaveSlots.currentSlot, backupIndex) : SaveSlots.GetCurrentSavePath());
            SaveContainer saveContainer;
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = File.Open(path, FileMode.Open))
            {   // Deserialize the save container from the file
                saveContainer = (SaveContainer)binaryFormatter.Deserialize(fileStream);
            }

            saveContainer = SaveCleaner.CleanSave(saveContainer);

            /*if (SaveCleaner.changes == 0)
            {
                Debug.LogWarning("Save didn't need cleaning");
                return;
            }*/
            using (FileStream fileStream = File.Open(path, FileMode.Create))
            {
                binaryFormatter.Serialize(fileStream, saveContainer);
            }
            Plugin.logSource.LogInfo($"Saved Cleaned data to slot{SaveSlots.currentSlot}.save");
        }

        [HarmonyPatch("LoadNeeds")]
        [HarmonyPostfix]
        public static void LoadNeedsPatch(ref SaveContainer save, SaveableObject[] ___currentObjects)
        {
            if (Plugin.overWrite.Value) return;
            //Plugin.logSource.LogInfo("Running Save Cleaner");
            save = SaveCleaner.CleanSave(save);

            Plugin.logSource.LogInfo("Loaded Data Cleaned");
        }
    }
}
