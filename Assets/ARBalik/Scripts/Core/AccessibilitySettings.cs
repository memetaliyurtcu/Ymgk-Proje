using UnityEngine;

namespace ARFishing.Core
{
    [CreateAssetMenu(menuName = "ARFishing/Accessibility Settings", fileName = "AccessibilitySettings")]
    public class AccessibilitySettings : ScriptableObject
    {
        [Header("Motion")]
        [SerializeField, Tooltip("Disables panel tween animations and idle bubble drift; uses instant snap instead.")]
        bool m_ReducedMotion = false;

        [Header("Captions")]
        [SerializeField, Tooltip("Shows the focused creature's InterestingTrait as a text overlay during narration.")]
        bool m_NarrationCaptions = false;

        [Header("UI scale")]
        [SerializeField, Range(0.8f, 1.5f), Tooltip("Multiplier applied by AccessibilityState to opt-in components.")]
        float m_UiScaleMultiplier = 1f;

        public bool ReducedMotion => m_ReducedMotion;
        public bool NarrationCaptions => m_NarrationCaptions;
        public float UiScaleMultiplier => m_UiScaleMultiplier;
    }
}
