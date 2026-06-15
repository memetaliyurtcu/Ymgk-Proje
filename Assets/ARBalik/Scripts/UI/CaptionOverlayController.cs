using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Narration;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.UI
{
    public class CaptionOverlayController : MonoBehaviour
    {
        [SerializeField] NarrationPlayer m_Narration;
        [SerializeField] GameObject m_PanelRoot;
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] Text m_CaptionText;

        void Awake()
        {
            if (m_Narration == null) ServiceLocator.TryGet(out m_Narration);
            SetVisible(false);
        }

        void OnEnable()
        {
            if (m_Narration != null)
            {
                m_Narration.NarrationStarted += HandleNarrationStarted;
                m_Narration.NarrationFinished += HandleNarrationFinished;
            }
            AccessibilityState.Changed += HandleAccessibilityChanged;
        }

        void OnDisable()
        {
            if (m_Narration != null)
            {
                m_Narration.NarrationStarted -= HandleNarrationStarted;
                m_Narration.NarrationFinished -= HandleNarrationFinished;
            }
            AccessibilityState.Changed -= HandleAccessibilityChanged;
        }

        void HandleNarrationStarted(CreatureDefinition def)
        {
            if (!AccessibilityState.NarrationCaptions) { SetVisible(false); return; }
            if (def == null) { SetVisible(false); return; }

            if (m_CaptionText != null)
            {
                m_CaptionText.text = !string.IsNullOrEmpty(def.InterestingTrait)
                    ? def.InterestingTrait
                    : def.DisplayName;
            }
            SetVisible(true);
        }

        void HandleNarrationFinished(CreatureDefinition def)
        {
            SetVisible(false);
        }

        void HandleAccessibilityChanged()
        {
            if (!AccessibilityState.NarrationCaptions) SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (m_PanelRoot != null) m_PanelRoot.SetActive(visible);
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = visible ? 1f : 0f;
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
        }
    }
}
