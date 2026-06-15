using System;

namespace ARFishing.Core
{
    public static class AccessibilityState
    {
        static AccessibilitySettings s_Settings;

        public static event Action Changed;

        public static AccessibilitySettings Current
        {
            get => s_Settings;
            set
            {
                if (s_Settings == value) return;
                s_Settings = value;
                Changed?.Invoke();
            }
        }

        public static bool ReducedMotion => s_Settings != null && s_Settings.ReducedMotion;
        public static bool NarrationCaptions => s_Settings != null && s_Settings.NarrationCaptions;
        public static float UiScaleMultiplier => s_Settings != null ? s_Settings.UiScaleMultiplier : 1f;
    }
}
