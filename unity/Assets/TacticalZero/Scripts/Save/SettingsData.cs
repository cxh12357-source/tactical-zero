using System;

namespace TacticalZero
{
    [Serializable]
    public sealed class SettingsData
    {
        public int version = 1;
        public float ambientVolume = .25f;
        public float masterVolume = .75f, sfxVolume = .8f, mouseSensitivity = 1.6f, touchSensitivity = 2, adsSensitivity = .65f, aimAssist = .5f;
        public bool cameraShake = true, weaponSway = true, invertY = false;
        public bool fullscreen = true;
        public bool toggleAim = false;
        public int quality = 1, frameLimit = 60;
        public void Validate()
        {
            ambientVolume = Clamp(ambientVolume,0,1,.25f);
            masterVolume = Clamp(masterVolume, 0, 1, .75f);
            sfxVolume = Clamp(sfxVolume, 0, 1, .8f);
            mouseSensitivity = Clamp(mouseSensitivity, .2f, 5, 1.6f);
            touchSensitivity = Clamp(touchSensitivity, .2f, 5, 2);
            adsSensitivity = Clamp(adsSensitivity, .2f, 1.5f, .65f);
            aimAssist = Clamp(aimAssist, 0, 1, .5f);
            quality = Math.Max(0, Math.Min(2, quality));
            frameLimit = frameLimit == 30 || frameLimit == 60 || frameLimit == 120 || frameLimit == 144 ? frameLimit : 60;
            version = 1;
        }

        private static float Clamp(float x, float min, float max, float fallback)
        {
            return float.IsNaN(x) || float.IsInfinity(x) ? fallback : Math.Max(min, Math.Min(max, x));
        }
    }
}
