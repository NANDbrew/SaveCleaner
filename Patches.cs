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

            Debug.LogWarning("Save Cleaner Running");
            Debug.LogWarning("Cleaning Slot " + SaveSlots.currentSlot);
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
            Debug.LogWarning("Save Cleaned");
        }

        [HarmonyPatch("LoadNeeds")]
        [HarmonyPostfix]
        public static void LoadNeedsPatch(ref SaveContainer save, SaveableObject[] ___currentObjects)
        {
            if (Plugin.overWrite.Value) return;
            Debug.LogWarning("Save Cleaner Running");
            save = SaveCleaner.CleanSave(save);

            Debug.LogWarning("Loaded Data Cleaned");
        }
    }
}
