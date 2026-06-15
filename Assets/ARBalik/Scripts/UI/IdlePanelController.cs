using ARFishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.UI
{
    public class IdlePanelController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] ActivityController m_Controller;

        [Header("Panel root")]
        [SerializeField] GameObject m_PanelRoot;
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] PanelTween m_Tween;

        [Header("Text")]
        [SerializeField] Text m_TitleText;
        [SerializeField] Text m_SubtitleText;

        const string DefaultTitle = "Görünenin Ötesinde Bir Deniz";
        const string DefaultSubtitle = "Etkinliği başlatması için öğretmeni bekleyelim.";

        void Awake()
        {
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);

            if (m_TitleText != null && string.IsNullOrEmpty(m_TitleText.text))
                m_TitleText.text = DefaultTitle;
            if (m_SubtitleText != null && string.IsNullOrEmpty(m_SubtitleText.text))
                m_SubtitleText.text = DefaultSubtitle;

            SetVisible(false);
        }

        void OnEnable()
        {
            if (m_Controller != null)
            {
                m_Controller.StateChanged += HandleStateChanged;
                ApplyState(m_Controller.Current);
            }
        }

        void OnDisable()
        {
            if (m_Controller != null) m_Controller.StateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(ActivityState previous, ActivityState next)
        {
            ApplyState(next);
        }

        void ApplyState(ActivityState state)
        {
            SetVisible(state == ActivityState.Idle);
        }

        void SetVisible(bool visible)
        {
            if (m_Tween != null)
            {
                if (visible) m_Tween.Show();
                else m_Tween.Hide();
                return;
            }

            if (visible && m_PanelRoot != null && !m_PanelRoot.activeSelf)
                m_PanelRoot.SetActive(true);

            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = visible ? 1f : 0f;
                m_CanvasGroup.interactable = visible;
                m_CanvasGroup.blocksRaycasts = visible;
            }
        }
    }
}
