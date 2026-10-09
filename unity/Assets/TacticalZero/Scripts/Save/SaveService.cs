using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TacticalZero
{
    public static class SaveService
    {
        public const string SettingsKey = "TacticalZero3D.Settings.v1";
        public const string ProgressKey = "TacticalZero3D.Progress.v1";
        public static SettingsData Settings { get; private set; } = new SettingsData();
        public static string LastError { get; private set; } = "";

        public static event Action Applied;
        [Serializable]
        public sealed class Progress
        {
            public int runs, wins, bestScore, totalKills;
        }

        public static SettingsData Decode(string json)
        {
            try
            {
                var data = new SettingsData();
                JsonUtility.FromJsonOverwrite(json, data);
                if (data.version != 1)
                    return new SettingsData();
                data.Validate();
                return data;
            }
            catch
            {
                return new SettingsData();
            }
        }

        public static string Encode(SettingsData settings)
        {
            settings.Validate();
            return JsonUtility.ToJson(settings);
        }

        public static void Load()
        {
            Settings = Decode(PlayerPrefs.GetString(SettingsKey, "{}"));
        }

        public static bool Save()
        {
            try
            {
                PlayerPrefs.SetString(SettingsKey, Encode(Settings));
                PlayerPrefs.Save();
                LastError = "";
                return true;
            }
            catch (Exception e)
            {
                LastError = "设置仅在本次运行有效：" + e.GetType().Name;
                return false;
            }
        }

        public static void Apply()
        {
            Settings.Validate();
            QualitySettings.vSyncCount = 0;
            DisplayMode.Apply();
            Application.targetFrameRate = Settings.frameLimit;
            AudioListener.volume = Settings.masterVolume;
            QualitySettings.shadows = Settings.quality == 0 ? UnityEngine.ShadowQuality.Disable : Settings.quality == 1 ? UnityEngine.ShadowQuality.HardOnly : UnityEngine.ShadowQuality.All;
            QualitySettings.shadowDistance = Settings.quality == 0 ? 0 : Settings.quality == 1 ? 22 : 35;
            var pipeline = (QualitySettings.renderPipeline ? QualitySettings.renderPipeline : UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (pipeline)
            {
                pipeline.renderScale = Settings.quality == 0 ? .75f : Settings.quality == 1 ? 1 : 1;
                pipeline.shadowDistance = QualitySettings.shadowDistance;
                pipeline.msaaSampleCount=Settings.quality==0?1:Settings.quality==1?2:4;
            }

            Applied?.Invoke();
        }

        public static void Record(MatchStats stats)
        {
            try
            {
                Progress p;
                try
                {
                    p = JsonUtility.FromJson<Progress>(PlayerPrefs.GetString(ProgressKey, "{}")) ?? new Progress();
                }
                catch
                {
                    p = new Progress();
                }

                p.runs++;
                p.wins += stats.won ? 1 : 0;
                p.totalKills += stats.kills;
                p.bestScore = Math.Max(p.bestScore, stats.Score);
                PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(p));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                LastError = "本局统计无法保存：" + e.GetType().Name;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Applied = null;
            Settings = new SettingsData();
            LastError = "";
        }
    }
}
