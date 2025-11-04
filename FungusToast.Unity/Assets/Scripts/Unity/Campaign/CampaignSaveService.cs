using System;
using System.IO;
using UnityEngine;

namespace FungusToast.Unity.Campaign
{
    /// <summary>
    /// Handles persistence of the single active campaign to disk (JSON in persistentDataPath).
    /// </summary>
    public static class CampaignSaveService
    {
        private const string FileName = "campaign_save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(SavePath);

        public static CampaignState Load()
        {
            try
            {
                if (!Exists()) return null;
                var json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<CampaignState>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CampaignSaveService] Failed to load campaign save: {ex.Message}\n{ex}");
                return null;
            }
        }

        public static void Save(CampaignState state)
        {
            if (state == null) return;
            try
            {
                var json = JsonUtility.ToJson(state, prettyPrint: true);
                var tmpPath = SavePath + ".tmp";
                File.WriteAllText(tmpPath, json);
                // Replace existing atomically
                if (Exists()) File.Delete(SavePath);
                File.Move(tmpPath, SavePath);
                Debug.Log($"[CampaignSaveService] Saved campaign to {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CampaignSaveService] Failed to save campaign: {ex.Message}\n{ex}");
            }
        }

        public static void Delete()
        {
            try
            {
                if (Exists()) File.Delete(SavePath);
                Debug.Log("[CampaignSaveService] Deleted campaign save.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CampaignSaveService] Failed to delete campaign save: {ex.Message}\n{ex}");
            }
        }
    }
}
