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
        public static TraderBoatData[] referenceTraderData;
        public static int changes = 0;
        public static SaveContainer CleanSave(SaveContainer saveContainer)
        {
            referenceTraderData = new TraderBoatData[saveContainer.traderBoatData.Length];
            for (int i = 0; i < referenceTraderData.Length; i++)
            {
                referenceTraderData[i] = saveContainer.traderBoatData[i];
            }
            Debug.Log("trader boat data length = " + saveContainer.traderBoatData.Length);
            changes = 0;
            saveContainer.savedPrefabs.RemoveAll(prefab => !IsValidPrefab(prefab.prefabIndex) || (prefab.itemParentObject > -1 && !IsValidSaveableObject(prefab.itemParentObject)));
            saveContainer.savedObjects.RemoveAll(obj => !IsValidSaveableObject(obj.sceneIndex));
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
                //changes++;
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
                    //changes++;
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
                bool brokenIndex = false;

                for (int j = 0; j < traderBoatData.carriedGoods.Length; j++)
                {
                    int index = traderBoatData.carriedGoods[j];
                    if (index > 0 && !IsValidPrefab(index))
                    {
                        brokenIndex = true;
                        traderBoatData.carriedGoods[j] = 0;
                        //changes++;
                        //break;
                    }
                }
                if (brokenIndex) changes++;
                if (!IsValidPort(traderBoatData.currentIslandMarket))
                {
                    traderBoatData.currentIslandMarket = -1;
                    //changes++;
                }
                if (!IsValidPort(traderBoatData.currentDestination))
                {
                    traderBoatData.currentDestination = -1;
                    //changes++;
                }

                if (/*!brokenIndex && */IsValidPort(traderBoatData.lastIslandMarket))
                {
                    outgoingData.Add(traderBoatData);
                }
                else
                {
                    changes++;
                    Debug.LogError("removed trader boat " + i);
                }
            }
            return outgoingData.ToArray();
        }

        public static LoggedMission[] CleanLoggedMissions(LoggedMission[] savedMissions)
        {
            //bool changed = false;
            for (int i = 0; i < savedMissions.Length; i++)
            {
                LoggedMission mission = savedMissions[i];
                if (!IsValidPrefab(mission.goodIndex))
                {
                    mission.goodIndex = 0;
                    //changes++;
                    //changed = true;
                }
                if (!Enum.IsDefined(typeof(PortRegion), mission.repRegion))
                {
                    mission.repRegion = (int)PortRegion.none;
                    changes++;
                    //changed = true;
                }
            }
            //changes += changed ? 1 : 0;
            return savedMissions;
        }


        public static SaveMissionData[] CleanMissionData(SaveMissionData[] savedMissions)
        {
            for (int i = 0; i < savedMissions.Length; i++)
            {
                SaveMissionData missionData = savedMissions[i];
                if (missionData != null && IsValidPrefab(missionData.goodPrefabIndex) && IsValidPort(missionData.originPort) && IsValidPort(missionData.destinationPort))
                {
                    //outgoingData.Add(missionData);
                    continue;
                }
                savedMissions[i] = null;
                changes++;
            }
            return savedMissions;
        }

        public static bool IsValidPrefab(int prefabIndex)
        {
            return prefabIndex >= 0 && prefabIndex < PrefabsDirectory.instance.directory.Length && PrefabsDirectory.instance.directory[prefabIndex] != null;
        }
        public static bool IsValidSaveableObject(int sceneIndex)
        {
            return sceneIndex >= 0 && sceneIndex < SaveLoadManager.instance.GetCurrentObjects().Length && SaveLoadManager.instance.GetCurrentObjects()[sceneIndex] != null;
        }
        public static bool IsValidPort(int portIndex)
        {
            return portIndex >= 0 && portIndex < Port.ports.Length && Port.ports[portIndex] != null;
        }

        public static IEnumerator ShowNotificationAfterDelay(string text)
        {
            yield return new WaitUntil(() => GameState.playing && !GameState.justStarted);
            //yield return new WaitForSeconds(5);
            NotificationUi.instance.ShowNotification(text);
            //Debug.Log("displayed notification?");
        }
    }
}