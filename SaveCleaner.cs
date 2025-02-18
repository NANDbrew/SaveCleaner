using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SaveCleaner
{
    internal static class SaveCleaner
    {
        public static SaveContainer CleanSave(SaveContainer saveContainer)
        {
            saveContainer.savedPrefabs.RemoveAll(prefab => !IsValidPrefab(prefab.prefabIndex) || (prefab.itemParentObject > -1 && !IsValidSaveableObject(prefab.itemParentObject)));
            saveContainer.savedObjects.RemoveAll(obj => !IsValidSaveableObject(obj.sceneIndex));
            saveContainer.loggedMissions.Do(mission => { if (!Enum.IsDefined(typeof(PortRegion), mission.repRegion) || !IsValidPrefab(mission.goodIndex)) mission.goodIndex = 0; });
            saveContainer.traderBoatData = CleanTraderBoatData(saveContainer.traderBoatData);
            if (saveContainer.savedMissions.Length > 0)
            {
                saveContainer.savedMissions = CleanMissionData(saveContainer.savedMissions);
            }
            foreach (SaveObjectData objectData in saveContainer.savedObjects)
            {
                if (objectData.customization != null)
                {
                    objectData.customization = CleanCustomizationData(objectData.customization, objectData.sceneIndex);
                }
            }
            return saveContainer;
        }

        public static SaveBoatCustomizationData CleanCustomizationData(SaveBoatCustomizationData data, int index)
        {
            BoatCustomParts parts = SaveLoadManager.instance.GetCurrentObjects()[index].GetComponent<BoatCustomParts>();
            BoatRefs refs = parts.GetComponent<BoatRefs>();
            if (data.masts.Length > refs.masts.Length)
            {
                Array.Resize(ref data.masts, refs.masts.Length);
            }
            if (data.partActiveOptions.Count > parts.availableParts.Count)
            {
                data.partActiveOptions.RemoveRange(parts.availableParts.Count, data.partActiveOptions.Count - parts.availableParts.Count);
            }
            for (int i = 0; i < data.partActiveOptions.Count; i++)
            {
                if (data.partActiveOptions[i] >= parts.availableParts[i].partOptions.Count)
                {
                    data.partActiveOptions[i] = parts.availableParts[i].activeOption;
                }
            }
            for (int i = 0; i < data.sails.Count;)
            {
                var sail = data.sails[i];
                if (sail.sailColor >= PrefabsDirectory.instance.sailColors.Length)
                {
                    sail.sailColor = 0;
                }
                if (sail.mastIndex >= refs.masts.Length || refs.masts[sail.mastIndex] == null || sail.prefabIndex >= PrefabsDirectory.instance.sails.Length || PrefabsDirectory.instance.sails[sail.prefabIndex] == null)
                {
                    data.sails.Remove(sail);
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
                /*if (traderBoatData.carriedPriceReports.Length > Port.ports.Length)
                {
                    Array.Resize(ref traderBoatData.carriedPriceReports, Port.ports.Length);
                }*/
                //traderBoatData.carriedPriceReports = new PriceReport[0];
                bool brokenIndex = false;
                foreach (int index in traderBoatData.carriedGoods)
                {
                    if (!IsValidPrefab(index))
                    {
                        brokenIndex = true;
                        break;
                    }
                }
                if (!brokenIndex && IsValidPort(traderBoatData.currentDestination) && IsValidPort(traderBoatData.lastIslandMarket) && IsValidPort(traderBoatData.currentIslandMarket))
                {
                    outgoingData.Add(traderBoatData);
                }
            }
            return outgoingData.ToArray();
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
    }
}