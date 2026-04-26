using System;
using System.IO;
using System.Reflection;
using FungusToast.Core.Persistence;
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

namespace FungusToast.Unity.Save
{
    [Serializable]
    public sealed class RandomStateSnapshot
    {
        public int seed;
        public int inext;
        public int inextp;
        public int[] seedArray = Array.Empty<int>();
    }

    [Serializable]
    public sealed class SoloGameSaveState
    {
        public int boardWidth;
        public int boardHeight;
        public int playerCount;
        public int humanPlayerCount;
        public System.Collections.Generic.List<int> humanMoldIndices = new();
        public int gameplaySeed;
        public RoundStartRuntimeSnapshot runtimeSnapshot;
        public RandomStateSnapshot randomState;
    }

    /// <summary>
    /// Handles persistence of the single active non-campaign hotseat save to disk.
    /// </summary>
    public static class SoloGameSaveService
    {
        private const string FileName = "solo_save.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(SavePath);

        public static SoloGameSaveState Load()
        {
            try
            {
                if (!Exists())
                {
                    return null;
                }

                var json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonUtility.FromJson<SoloGameSaveState>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoloGameSaveService] Failed to load solo save: {ex.Message}\n{ex}");
                return null;
            }
        }

        public static void Save(SoloGameSaveState state)
        {
            if (state == null)
            {
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(state, prettyPrint: true);
                var tmpPath = SavePath + ".tmp";
                File.WriteAllText(tmpPath, json);
                if (Exists())
                {
                    File.Delete(SavePath);
                }

                File.Move(tmpPath, SavePath);
                Debug.Log($"[SoloGameSaveService] Saved solo game to {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoloGameSaveService] Failed to save solo game: {ex.Message}\n{ex}");
            }
        }

        public static void Delete()
        {
            try
            {
                if (Exists())
                {
                    File.Delete(SavePath);
                }

                Debug.Log("[SoloGameSaveService] Deleted solo save.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoloGameSaveService] Failed to delete solo save: {ex.Message}\n{ex}");
            }
        }
    }

    internal static class RandomStateSerialization
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public static RandomStateSnapshot Capture(System.Random random, int seed)
        {
            if (random == null)
            {
                return null;
            }

            var snapshot = new RandomStateSnapshot { seed = seed };

            if (TryCaptureCompatState(random, snapshot) || TryCaptureLegacyState(random, snapshot))
            {
                return snapshot;
            }

            Debug.LogWarning("[RandomStateSerialization] Falling back to seed-only random snapshot capture.");
            return snapshot;
        }

        public static System.Random Restore(RandomStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return new System.Random();
            }

            var restored = new System.Random(snapshot.seed);
            if (TryRestoreCompatState(restored, snapshot) || TryRestoreLegacyState(restored, snapshot))
            {
                return restored;
            }

            Debug.LogWarning("[RandomStateSerialization] Falling back to seed-only random restoration.");
            return restored;
        }

        private static bool TryCaptureCompatState(System.Random random, RandomStateSnapshot snapshot)
        {
            var implField = typeof(System.Random).GetField("_impl", InstanceFlags);
            if (implField == null)
            {
                return false;
            }

            var impl = implField.GetValue(random);
            if (impl == null)
            {
                return false;
            }

            var prngField = impl.GetType().GetField("_prng", InstanceFlags);
            if (prngField == null)
            {
                return false;
            }

            var prng = prngField.GetValue(impl);
            if (prng == null)
            {
                return false;
            }

            return TryCopyStateFromObject(prng, snapshot, "_seedArray", "_inext", "_inextp");
        }

        private static bool TryRestoreCompatState(System.Random random, RandomStateSnapshot snapshot)
        {
            var implField = typeof(System.Random).GetField("_impl", InstanceFlags);
            if (implField == null)
            {
                return false;
            }

            var impl = implField.GetValue(random);
            if (impl == null)
            {
                return false;
            }

            var prngField = impl.GetType().GetField("_prng", InstanceFlags);
            if (prngField == null)
            {
                return false;
            }

            var prng = prngField.GetValue(impl);
            if (prng == null)
            {
                return false;
            }

            if (!TryApplyStateToObject(prng, snapshot, "_seedArray", "_inext", "_inextp"))
            {
                return false;
            }

            prngField.SetValue(impl, prng);
            return true;
        }

        private static bool TryCaptureLegacyState(System.Random random, RandomStateSnapshot snapshot)
        {
            return TryCopyStateFromObject(random, snapshot, "SeedArray", "inext", "inextp")
                || TryCopyStateFromObject(random, snapshot, "_seedArray", "_inext", "_inextp");
        }

        private static bool TryRestoreLegacyState(System.Random random, RandomStateSnapshot snapshot)
        {
            return TryApplyStateToObject(random, snapshot, "SeedArray", "inext", "inextp")
                || TryApplyStateToObject(random, snapshot, "_seedArray", "_inext", "_inextp");
        }

        private static bool TryCopyStateFromObject(object source, RandomStateSnapshot snapshot, string seedArrayFieldName, string inextFieldName, string inextpFieldName)
        {
            var sourceType = source.GetType();
            var seedArrayField = sourceType.GetField(seedArrayFieldName, InstanceFlags);
            var inextField = sourceType.GetField(inextFieldName, InstanceFlags);
            var inextpField = sourceType.GetField(inextpFieldName, InstanceFlags);
            if (seedArrayField == null || inextField == null || inextpField == null)
            {
                return false;
            }

            var seedArray = seedArrayField.GetValue(source) as int[];
            if (seedArray == null)
            {
                return false;
            }

            snapshot.seedArray = (int[])seedArray.Clone();
            snapshot.inext = (int)inextField.GetValue(source);
            snapshot.inextp = (int)inextpField.GetValue(source);
            return true;
        }

        private static bool TryApplyStateToObject(object destination, RandomStateSnapshot snapshot, string seedArrayFieldName, string inextFieldName, string inextpFieldName)
        {
            if (snapshot.seedArray == null || snapshot.seedArray.Length == 0)
            {
                return false;
            }

            var destinationType = destination.GetType();
            var seedArrayField = destinationType.GetField(seedArrayFieldName, InstanceFlags);
            var inextField = destinationType.GetField(inextFieldName, InstanceFlags);
            var inextpField = destinationType.GetField(inextpFieldName, InstanceFlags);
            if (seedArrayField == null || inextField == null || inextpField == null)
            {
                return false;
            }

            seedArrayField.SetValue(destination, (int[])snapshot.seedArray.Clone());
            inextField.SetValue(destination, snapshot.inext);
            inextpField.SetValue(destination, snapshot.inextp);
            return true;
        }
    }
}
