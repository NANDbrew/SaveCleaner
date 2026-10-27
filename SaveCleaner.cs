using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SaveCleaner
{
    internal static class SaveCleaner
    {
#if DEBUG
        static SaveContainer referenceData;
        static SaveContainer outputData;
#endif
        public static int changes = 0;
        public static SaveContainer CleanSave(SaveContainer saveContainer)
        {
#if DEBUG
            referenceData = saveContainer;
#endif
            Plugin.logSource.LogInfo("Running Save Cleaner");

            changes = 0;
            saveContainer.savedPrefabs = CleanPrefabs(saveContainer.savedPrefabs);
            saveContainer.savedObjects = CleanObjects(saveContainer.savedObjects);
            saveContainer.loggedMissions = CleanLoggedMissions(saveContainer.loggedMissions);
            saveContainer.traderBoatData = CleanTraderBoatData(saveContainer.traderBoatData);
            saveContainer.savedMissions = CleanMissionData(saveContainer.savedMissions);
            saveContainer.marketsSupply = CleanMarketSupply(saveContainer.marketsSupply);
            saveContainer.marketsKnownPrices = CleanMarketKnownPrices(saveContainer.marketsKnownPrices);
            //Array.Resize(ref saveContainer.playerKnownPrices, Port.ports.Length);
#if DEBUG
            outputData = saveContainer;
#endif
            Plugin.logSource.LogInfo($"Queueing message: Save Cleaned. fixed {changes} issues");
            NotificationUi.instance.StartCoroutine(ShowNotificationAfterDelay($"Save Cleaned\n\nfixed {changes} issues"));
            return saveContainer;
        }

        public static float[][] CleanMarketSupply(float[][] inputArray)
        {
            int problemCount = 0;
            List<float[]> output = new List<float[]>();

            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking market supply");
            for (int i = 0; i < inputArray.GetLength(0); i++)
            {
                if (i > Port.ports.Length)
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("resized market supply array " + i);
                    problemCount++;
                    break;
                }
                if (inputArray[i] == null)
                {
                    //if (Plugin.verbose.Value) Plugin.logSource.LogWarning("skipping null market " + i);
                   
                    // do nothing
                }
                output.Add(inputArray[i]);
            }
            if (problemCount > 0)
            {
                changes++;
                Plugin.logSource.LogWarning($"fixed {problemCount} market supply issues");
            }
            return output.ToArray();
        }

        public static PriceReport[][] CleanMarketKnownPrices(PriceReport[][] priceReports)
        {
            int problemCount = 0;
            List<PriceReport[]> output = new List<PriceReport[]>();

            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking market price reports");
            for (int i = 0; i < priceReports.GetLength(0); i++)
            {
                if (i > Port.ports.Length)
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("cutting market PriceReports array");
                    problemCount++;
                    break;
                }
                if (priceReports[i] == null)
                {
                    // do nothing. (used to skip, but that breaks the array index

                }
                else if (priceReports[i].Length > Port.ports.Length)
                {
                    Array.Resize(ref priceReports[i], Port.ports.Length);
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("cutting PriceReport array for market " + i);
                    problemCount++;
                }
                /*                for (int j = 0; j < inputArray[i].Length; j++)
                                {
                                    if (inputArray[i][j] != null && inputArray[i][j].buyPrices.Length >)
                                    {

                                    }
                                }*/
                output.Add(priceReports[i]);
            }
            if (problemCount > 0)
            {
                Plugin.logSource.LogWarning($"fixed {problemCount} market PriceReport issues");
                changes++;
            }

            return output.ToArray();
        }

        public static SaveBoatCustomizationData CleanCustomizationData(SaveBoatCustomizationData data, int index)
        {
            int problemCount = 0;
            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking customization data for object " + index);
            BoatCustomParts parts = SaveLoadManager.instance.GetCurrentObjects()[index].GetComponent<BoatCustomParts>();
            BoatRefs refs = parts.GetComponent<BoatRefs>();
            if (data.masts.Length > refs.masts.Length)
            {
                if (Plugin.verbose.Value) Plugin.logSource.LogWarning("cutting mast array for object " + index);
                Array.Resize(ref data.masts, refs.masts.Length);
                problemCount++;
            }
            if (data.partActiveOptions.Count > parts.availableParts.Count)
            {
                if (Plugin.verbose.Value) Plugin.logSource.LogWarning("cutting PartActiveOptions for object " + index);
                data.partActiveOptions.RemoveRange(parts.availableParts.Count, data.partActiveOptions.Count - parts.availableParts.Count);
                problemCount++;
            }
            //if (Plugin.verbose.Value) Debug.Log("updating PartActiveOptions for object " + index);

            for (int i = 0; i < data.partActiveOptions.Count; i++)
            {
                if (data.partActiveOptions[i] >= parts.availableParts[i].partOptions.Count)
                {
                    data.partActiveOptions[i] = parts.availableParts[i].activeOption;
                    if (Plugin.verbose.Value) Plugin.logSource.LogInfo($"padded part options for part {i} on object {index}");
                    //changes++;
                }
            }
            if (problemCount > 0)
            {
                Plugin.logSource.LogWarning($"fixed {problemCount} issues with parts for object {index}");
            }
            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking sails and sail colors for object " + index);

            problemCount = 0;
            for (int i = 0; i < data.sails.Count;)
            {
                var sail = data.sails[i];
                if (sail.sailColor >= PrefabsDirectory.instance.sailColors.Length)
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning($"fixed invalid sail color ({sail.sailColor})");
                    sail.sailColor = 0;
                    problemCount++;
                }
                if (sail.mastIndex >= refs.masts.Length || refs.masts[sail.mastIndex] == null || sail.prefabIndex >= PrefabsDirectory.instance.sails.Length || PrefabsDirectory.instance.sails[sail.prefabIndex] == null)
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("removed sail with prefab index " + sail.prefabIndex);
                    data.sails.Remove(sail);
                    problemCount++;
                    continue;
                }
                i++;
            }
            if (problemCount > 0)
            {
                Plugin.logSource.LogInfo($"fixed {problemCount} issues with sails for object {index}");
            }
            return data;
        }

        public static TraderBoatData[] CleanTraderBoatData(TraderBoatData[] incomingData)
        {
            int problemCount = 0;
            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking trader boat data");
            List<TraderBoatData> outgoingData = new List<TraderBoatData>();
            for (int i = 0; i < incomingData.Length; i++)
            {
                TraderBoatData traderBoatData = incomingData[i];
                for (int j = 0; j < traderBoatData.carriedGoods.Length; j++)
                {
                    int index = traderBoatData.carriedGoods[j];
                    if (index > 0 && !IsValidPrefab(PrefabsDirectory.GoodToItemIndex(index)))
                    {
                        traderBoatData.carriedGoods[j] = 0;
                        problemCount++;

                        if (Plugin.verbose.Value) Plugin.logSource.LogWarning("removed trader boat carried good " + j);
                    }
                }

                if (traderBoatData.carriedPriceReports.Length > Port.ports.Length)
                {
                    Array.Resize(ref traderBoatData.carriedPriceReports, Port.ports.Length);
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("resized trader boat price reports");
                    problemCount++;
                }

                if (!IsValidPort(traderBoatData.currentIslandMarket))
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("reset trader boat current island market from " + traderBoatData.currentIslandMarket);
                    traderBoatData.currentIslandMarket = -1;
                    problemCount++;
                }
                if (!IsValidPort(traderBoatData.currentDestination))
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("reset trader boat destination from " + traderBoatData.currentDestination);
                    traderBoatData.currentDestination = -1;
                    problemCount++;
                }
                if (!IsValidPort(traderBoatData.lastIslandMarket))
                {
                    problemCount++;
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning($"removed trader boat data {i} due to invalid last market");
                }
                else
                {
                    outgoingData.Add(traderBoatData);
                }

            }
            if (problemCount > 0)
            {
                changes++;
                Plugin.logSource.LogWarning($"fixed {problemCount} issues with trader boats");
            }
            return outgoingData.ToArray();
        }

        public static LoggedMission[] CleanLoggedMissions(LoggedMission[] loggedMissions)
        {
            int problemCount = 0;
            for (int i = 0; i < loggedMissions.Length; i++)
            {
                if (loggedMissions[i] == null) continue;

                LoggedMission mission = loggedMissions[i];
                if (mission.goodIndex != 0 && !IsValidPrefab(mission.goodIndex))
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("Removed loggedMission goodIndex " + mission.goodIndex);
                    mission.goodIndex = 0;
                    problemCount++;

                }
                if (!Enum.IsDefined(typeof(PortRegion), mission.repRegion))
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning("Removed loggedMission reputation region " + mission.repRegion);
                    mission.repRegion = (int)PortRegion.none;
                    problemCount++;

                }
            }
            if (problemCount > 0)
            {
                changes++;
                Plugin.logSource.LogWarning($"Fixed {problemCount} issues in logged missions");
            }
            return loggedMissions;
        }


        public static SaveMissionData[] CleanMissionData(SaveMissionData[] savedMissions)
        {
            int problemCount = 0;
            for (int i = 0; i < savedMissions.Length; i++)
            {
                if (savedMissions[i] == null) continue;
                SaveMissionData missionData = savedMissions[i];
                if (missionData != null && IsValidPrefab(missionData.goodPrefabIndex) && IsValidPort(missionData.originPort) && IsValidPort(missionData.destinationPort))
                {
                    continue;
                }
                savedMissions[i] = null;
                //changes++;
                problemCount++;
                if (Plugin.verbose.Value) Plugin.logSource.LogWarning("removed savedMission");

            }

            if (problemCount > 0)
            {
                changes++;
                Plugin.logSource.LogWarning($"Fixed {problemCount} missions");
            }
            return savedMissions;
        }

        public static List<SavePrefabData> CleanPrefabs(List<SavePrefabData> savedPrefabs)
        {
            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking saved prefabs");
            int problemCount = 0;
            List<int> invalidPrefabIDs = new List<int>();
            for (int i = 0; i < savedPrefabs.Count;)
            {
                var prefab = savedPrefabs[i];
                bool shouldRemove = false;
                if (!IsValidPrefab(prefab.prefabIndex))
                {
                    if (!invalidPrefabIDs.Contains(prefab.prefabIndex)) invalidPrefabIDs.Add(prefab.prefabIndex);
                    shouldRemove = true;
                }

                if (shouldRemove || (prefab.itemParentObject > -1 && !IsValidSaveableObject(prefab.itemParentObject)))
                {
                    if (Plugin.verbose.Value) Plugin.logSource.LogWarning($"removed saved item: {prefab.prefabIndex}, parent id was {prefab.itemParentObject}");
                    savedPrefabs.RemoveAt(i);
                    //changes++;
                    problemCount++;
                }
                else i++;
            }
            if (problemCount > 0)
            {
                Plugin.logSource.LogWarning($"removed {problemCount} item instances");
                changes += invalidPrefabIDs.Count;
            }
            return savedPrefabs;
        }

        public static List<SaveObjectData> CleanObjects(List<SaveObjectData> savedObjects)
        {
            if (Plugin.verbose.Value) Plugin.logSource.LogDebug("checking saved objects");
            for (int i = 0; i < savedObjects.Count;)
            {
                var obj = savedObjects[i];
                if (!IsValidSaveableObject(obj.sceneIndex))
                {
                    Plugin.logSource.LogWarning($"removed object with index {obj.sceneIndex}");
                    savedObjects.RemoveAt(i);
                    changes++;
                    continue;
                }

                if (obj.customization != null)
                {
                    obj.customization = CleanCustomizationData(obj.customization, obj.sceneIndex);
                }
                i++;
            }
            return savedObjects;
        }

        public static bool IsValidPrefab(int prefabIndex)
        {
            return prefabIndex < 0 || (prefabIndex < PrefabsDirectory.instance.directory.Length && PrefabsDirectory.instance.directory[prefabIndex] != null);
        }
        public static bool IsValidSaveableObject(int sceneIndex)
        {
            return sceneIndex < 0 || (sceneIndex < SaveLoadManager.instance.GetCurrentObjects().Length && SaveLoadManager.instance.GetCurrentObjects()[sceneIndex] != null);
        }
        public static bool IsValidPort(int portIndex)
        {
            return portIndex < 0 || (portIndex < Port.ports.Length && Port.ports[portIndex] != null);
        }

        public static IEnumerator ShowNotificationAfterDelay(string text)
        {
            yield return new WaitUntil(() => GameState.playing && !GameState.justStarted);
            NotificationUi.instance.ShowNotification(text);
            //Debug.Log("displayed notification?");
        }
    }
}