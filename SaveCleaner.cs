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
        //static SaveContainer referenceData;
        public static int changes = 0;
        public static SaveContainer CleanSave(SaveContainer saveContainer)
        {
            //referenceData = saveContainer;
            //saveContainer.savedPrefabs.Add(new SavePrefabData(Vector3.zero, Quaternion.identity, 999, true, 100, 0, -1, -1, -1, 1, 9999));
            changes = 0;
            //changes += saveContainer.savedPrefabs.RemoveAll(prefab => !IsValidPrefab(prefab.prefabIndex) || (prefab.itemParentObject > -1 && !IsValidSaveableObject(prefab.itemParentObject)));
            //changes += saveContainer.savedObjects.RemoveAll(obj => !IsValidSaveableObject(obj.sceneIndex));
            saveContainer.savedPrefabs = CleanPrefabs(saveContainer.savedPrefabs);
            saveContainer.savedObjects = CleanObjects(saveContainer.savedObjects);
            saveContainer.loggedMissions = CleanLoggedMissions(saveContainer.loggedMissions);
            saveContainer.traderBoatData = CleanTraderBoatData(saveContainer.traderBoatData);
            saveContainer.savedMissions = CleanMissionData(saveContainer.savedMissions);

            foreach (SaveObjectData objectData in saveContainer.savedObjects)
            {
                if (objectData.customization != null)
                {
                    objectData.customization = CleanCustomizationData(objectData.customization, objectData.sceneIndex);
                }
            }
            NotificationUi.instance.StartCoroutine(ShowNotificationAfterDelay($"Save Cleaned\n\nfixed {changes} issues"));
            return saveContainer;
        }

        public static SaveBoatCustomizationData CleanCustomizationData(SaveBoatCustomizationData data, int index)
        {
            BoatCustomParts parts = SaveLoadManager.instance.GetCurrentObjects()[index].GetComponent<BoatCustomParts>();
            BoatRefs refs = parts.GetComponent<BoatRefs>();
            if (data.masts.Length > refs.masts.Length)
            {
                Array.Resize(ref data.masts, refs.masts.Length);
                //changes++;
            }
            if (data.partActiveOptions.Count > parts.availableParts.Count)
            {
                data.partActiveOptions.RemoveRange(parts.availableParts.Count, data.partActiveOptions.Count - parts.availableParts.Count);
                changes++;
            }
            for (int i = 0; i < data.partActiveOptions.Count; i++)
            {
                if (data.partActiveOptions[i] >= parts.availableParts[i].partOptions.Count)
                {
                    data.partActiveOptions[i] = parts.availableParts[i].activeOption;
                    //changes++;
                }
            }
            for (int i = 0; i < data.sails.Count;)
            {
                var sail = data.sails[i];
                if (sail.sailColor >= PrefabsDirectory.instance.sailColors.Length)
                {
                    sail.sailColor = 0;
                    changes++;
                }
                if (sail.mastIndex >= refs.masts.Length || refs.masts[sail.mastIndex] == null || sail.prefabIndex >= PrefabsDirectory.instance.sails.Length || PrefabsDirectory.instance.sails[sail.prefabIndex] == null)
                {
                    data.sails.Remove(sail);
                    changes++;
                    continue;
                }
                i++;
            }
            return data;
        }

        public static TraderBoatData[] CleanTraderBoatData(TraderBoatData[] incomingData)
        {
            List<TraderBoatData> outgoingData = new List<TraderBoatData>();
            for (int i = 0; i < incomingData.Length; i++)
            {
                TraderBoatData traderBoatData = incomingData[i];
                int problemCount = 0;
                for (int j = 0; j < traderBoatData.carriedGoods.Length; j++)
                {
                    int index = traderBoatData.carriedGoods[j];
                    if (index > 0 && !IsValidPrefab(index))
                    {
                        traderBoatData.carriedGoods[j] = 0;
                        problemCount++;
                        Debug.Log("removed trader boat carried good");
                    }
                }
                if (!IsValidPort(traderBoatData.currentIslandMarket))
                {
                    traderBoatData.currentIslandMarket = -1;
                    problemCount++;
                    Debug.Log("removed trader boat current island market");
                }
                if (!IsValidPort(traderBoatData.currentDestination))
                {
                    traderBoatData.currentDestination = -1;
                    problemCount++;
                    Debug.Log("removed trader boat destination");
                }

                if (IsValidPort(traderBoatData.lastIslandMarket))
                {
                    outgoingData.Add(traderBoatData);
                }
                else
                {
                    problemCount++;
                    Debug.LogError("Removed trader boat " + i);
                }
                if (problemCount > 0) changes++;
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
                    mission.goodIndex = 0;
                    problemCount++;
                    Debug.LogError("axed loggedMission goodIndex");

                }
                if (!Enum.IsDefined(typeof(PortRegion), mission.repRegion))
                {
                    mission.repRegion = (int)PortRegion.none;
                    problemCount++;
                    Debug.LogError("axed loggedMission repRegion");
                }
            }
            if (problemCount > 0) changes++;
            return loggedMissions;
        }


        public static SaveMissionData[] CleanMissionData(SaveMissionData[] savedMissions)
        {
            for (int i = 0; i < savedMissions.Length; i++)
            {
                if (savedMissions[i] == null) continue;
                SaveMissionData missionData = savedMissions[i];
                if (missionData != null && IsValidPrefab(missionData.goodPrefabIndex) && IsValidPort(missionData.originPort) && IsValidPort(missionData.destinationPort))
                {
                    //outgoingData.Add(missionData);
                    continue;
                }
                savedMissions[i] = null;
                changes++;
                Debug.LogError("axed savedMission");

            }
            return savedMissions;
        }

        public static List<SavePrefabData> CleanPrefabs(List<SavePrefabData> savedPrefabs)
        {
            for (int i = 0; i < savedPrefabs.Count;)
            {
                var prefab = savedPrefabs[i];
                if (!IsValidPrefab(prefab.prefabIndex) || (prefab.itemParentObject > -1 && !IsValidSaveableObject(prefab.itemParentObject)))
                {
                    Debug.LogError($"removed prefab: {prefab.prefabIndex}, parent id was {prefab.itemParentObject}");
                    savedPrefabs.RemoveAt(i);
                    changes++;
                }
                else i++;
            }
            return savedPrefabs;
        }

        public static List<SaveObjectData> CleanObjects(List<SaveObjectData> savedObjects)
        {
            for (int i = 0; i < savedObjects.Count;)
            {
                var obj = savedObjects[i];
                if (!IsValidSaveableObject(obj.sceneIndex))
                {
                    Debug.LogError($"removed object: {obj.sceneIndex}");
                    savedObjects.RemoveAt(i);
                    changes++;
                }
                else i++;
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