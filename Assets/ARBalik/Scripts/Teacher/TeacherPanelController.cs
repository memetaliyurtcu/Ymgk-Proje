using ARFishing.Core;
using ARFishing.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.Teacher
{
    public class TeacherPanelController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] ActivityController m_Controller;

        [Header("Panel root")]
        [SerializeField] GameObject m_PanelRoot;
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] PanelTween m_Tween;

        [Header("Buttons")]
        [SerializeField] Button m_StartActivityButton;
        [SerializeField] Button m_RestartButton;
        [SerializeField] Button m_CloseButton;

        bool m_Open;

        void Awake()
        {
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);
            ServiceLocator.Register(this);
            Hide();
        }

        void OnEnable()
        {
            if (m_Controller != null)
            {
                m_Controller.StateChanged += HandleStateChanged;
                UpdateButtonVisibility(m_Controller.Current);
            }
            if (m_StartActivityButton != null) m_StartActivityButton.onClick.AddListener(OnStartActivity);
            if (m_RestartButton != null) m_RestartButton.onClick.AddListener(OnRestart);
            if (m_CloseButton != null) m_CloseButton.onClick.AddListener(Hide);
        }

        void OnDisable()
        {
            if (m_Controller != null) m_Controller.StateChanged -= HandleStateChanged;
            if (m_StartActivityButton != null) m_StartActivityButton.onClick.RemoveListener(OnStartActivity);
            if (m_RestartButton != null) m_RestartButton.onClick.RemoveListener(OnRestart);
            if (m_CloseButton != null) m_CloseButton.onClick.RemoveListener(Hide);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        public void Toggle()
        {
            if (m_Open) Hide();
            else Show();
        }

        public void Show()
        {
            m_Open = true;
            SetVisible(true);
            if (m_Controller != null) UpdateButtonVisibility(m_Controller.Current);
        }

        public void Hide()
        {
            m_Open = false;
            SetVisible(false);
        }

        void HandleStateChanged(ActivityState previous, ActivityState next)
        {
            UpdateButtonVisibility(next);
        }

        void UpdateButtonVisibility(ActivityState state)
        {
            if (m_StartActivityButton != null)
                m_StartActivityButton.gameObject.SetActive(state == ActivityState.Idle);

            if (m_RestartButton != null)
                m_RestartButton.gameObject.SetActive(
                    state != ActivityState.Idle && state != ActivityState.Bootstrap);
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

        void OnStartActivity()
        {
            if (m_Controller == null) return;
            if (m_Controller.Current == ActivityState.Idle)
                m_Controller.TryTransition(ActivityState.Scanning);
        }

        void OnRestart()
        {
            if (m_Controller == null) return;
            if (m_Controller.Current == ActivityState.Idle) return;
            if (m_Controller.Current == ActivityState.Bootstrap) return;
            m_Controller.TryTransition(ActivityState.Idle);
        }
    }
}
