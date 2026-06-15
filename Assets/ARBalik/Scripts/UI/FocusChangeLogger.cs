using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using ARFishing.Narration;
using UnityEngine;

namespace ARFishing.UI
{
    /// <summary>
    /// Diagnostic helper: logs FocusResolver.FocusChanged + NarrationPlayer events to the
    /// console. Drop on any Services GameObject for testing. Useful when InfoPanel texts
    /// don't update — confirms whether FocusChanged is firing at all and what the values are.
    /// Remove after debugging.
    /// </summary>
    public class FocusChangeLogger : MonoBehaviour
    {
        [SerializeField] FocusResolver m_FocusResolver;
        [SerializeField] NarrationPlayer m_Narration;

        void Awake()
        {
            if (m_FocusResolver == null) ServiceLocator.TryGet(out m_FocusResolver);
            if (m_Narration == null) ServiceLocator.TryGet(out m_Narration);
        }

        void OnEnable()
        {
            if (m_FocusResolver != null)
            {
                m_FocusResolver.FocusChanged += LogFocusChanged;
                Debug.Log($"[FocusLogger] Subscribed to FocusResolver. Current focused = {Name(m_FocusResolver.Focused)}");
            }
            else
            {
                Debug.LogWarning("[FocusLogger] FocusResolver not wired and not in ServiceLocator.");
            }
            if (m_Narration != null) m_Narration.NarrationStarted += LogNarrationStarted;
        }

        void OnDisable()
        {
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged -= LogFocusChanged;
            if (m_Narration != null) m_Narration.NarrationStarted -= LogNarrationStarted;
        }

        void LogFocusChanged(CreatureDefinition previous, CreatureDefinition next)
        {
            Debug.Log($"[FocusLogger] FocusChanged: {Name(previous)} -> {Name(next)}");
        }

        void LogNarrationStarted(CreatureDefinition def)
        {
            Debug.Log($"[FocusLogger] NarrationStarted: {Name(def)}");
        }

        static string Name(CreatureDefinition def)
        {
            return def == null ? "<null>" : $"{def.DisplayName} ({def.CreatureId})";
        }
    }
}
