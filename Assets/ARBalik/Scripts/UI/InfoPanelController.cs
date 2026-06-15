using System.Collections;
using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using ARFishing.Narration;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.UI
{
    public class InfoPanelController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] FocusResolver m_FocusResolver;
        [SerializeField] NarrationPlayer m_NarrationPlayer;
        [SerializeField] ActivityController m_ActivityController;

        [Header("Panel root")]
        [SerializeField] GameObject m_PanelRoot;
        [SerializeField] CanvasGroup m_PanelCanvasGroup;
        [SerializeField] PanelTween m_Tween;

        [Header("Text fields")]
        [SerializeField] Text m_DisplayName;
        [SerializeField] Text m_Category;
        [SerializeField] Text m_Habitat;
        [SerializeField] Text m_Diet;
        [SerializeField] Text m_EcosystemRole;
        [SerializeField] Text m_InterestingTrait;
        [SerializeField] Text m_Threats;

        [Header("Image")]
        [SerializeField] Image m_Icon;

        [Header("Buttons")]
        [SerializeField] Button m_ReplayButton;
        [SerializeField] Button m_CloseButton;

        [Header("Behavior")]
        [SerializeField, Min(0f), Tooltip("Seconds to keep panel visible after focus drops to null. Lets brief tracking interruptions skip the hide animation.")]
        float m_PassiveCloseSeconds = 2f;

        CreatureDefinition m_PendingFocus;
        Coroutine m_PassiveCloseCoroutine;

        void Awake()
        {
            if (m_FocusResolver == null) ServiceLocator.TryGet(out m_FocusResolver);
            if (m_NarrationPlayer == null) ServiceLocator.TryGet(out m_NarrationPlayer);
            if (m_ActivityController == null) ServiceLocator.TryGet(out m_ActivityController);

            SetVisible(false);
        }

        void OnEnable()
        {
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged += HandleFocusChanged;
            if (m_ActivityController != null) m_ActivityController.StateChanged += HandleStateChanged;
            if (m_ReplayButton != null) m_ReplayButton.onClick.AddListener(OnReplayClicked);
            if (m_CloseButton != null) m_CloseButton.onClick.AddListener(OnCloseClicked);
        }

        void OnDisable()
        {
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged -= HandleFocusChanged;
            if (m_ActivityController != null) m_ActivityController.StateChanged -= HandleStateChanged;
            if (m_ReplayButton != null) m_ReplayButton.onClick.RemoveListener(OnReplayClicked);
            if (m_CloseButton != null) m_CloseButton.onClick.RemoveListener(OnCloseClicked);
        }

        void HandleFocusChanged(CreatureDefinition previous, CreatureDefinition next)
        {
            CancelPassiveClose();

            if (next != null)
            {
                m_PendingFocus = next;
                Populate(next);
                UpdateVisibility();
                return;
            }

            if (IsAllowedState() && m_PendingFocus != null && gameObject.activeInHierarchy)
            {
                m_PassiveCloseCoroutine = StartCoroutine(PassiveCloseRoutine());
                return;
            }

            m_PendingFocus = null;
            UpdateVisibility();
        }

        void HandleStateChanged(ActivityState previous, ActivityState next)
        {
            CancelPassiveClose();
            if (!IsAllowedState()) m_PendingFocus = null;
            UpdateVisibility();
        }

        IEnumerator PassiveCloseRoutine()
        {
            yield return new WaitForSecondsRealtime(m_PassiveCloseSeconds);
            m_PendingFocus = null;
            m_PassiveCloseCoroutine = null;
            UpdateVisibility();
        }

        void CancelPassiveClose()
        {
            if (m_PassiveCloseCoroutine == null) return;
            StopCoroutine(m_PassiveCloseCoroutine);
            m_PassiveCloseCoroutine = null;
        }

        void UpdateVisibility()
        {
            SetVisible(IsAllowedState() && m_PendingFocus != null);
        }

        bool IsAllowedState()
        {
            if (m_ActivityController == null) return true;
            var s = m_ActivityController.Current;
            return s == ActivityState.Scanning || s == ActivityState.Viewing;
        }

        void Populate(CreatureDefinition def)
        {
            if (m_DisplayName != null) m_DisplayName.text = def.DisplayName;
            if (m_Category != null) m_Category.text = def.Category.ToTurkish();
            if (m_Habitat != null) m_Habitat.text = def.Habitat.ToTurkish();
            if (m_Diet != null) m_Diet.text = def.Diet.ToTurkish();
            if (m_EcosystemRole != null) m_EcosystemRole.text = def.EcosystemRole.ToTurkish();
            if (m_InterestingTrait != null) m_InterestingTrait.text = def.InterestingTrait;
            if (m_Threats != null)
            {
                m_Threats.text = def.Threats == null || def.Threats.Length == 0
                    ? "—"
                    : string.Join(" • ", def.Threats);
            }
            if (m_Icon != null)
            {
                m_Icon.sprite = def.Icon;
                m_Icon.enabled = def.Icon != null;
            }
        }

        void SetVisible(bool visible)
        {
            if (m_Tween != null)
            {
                if (visible) m_Tween.Show();
                else m_Tween.Hide();
                return;
            }

            // F10: only activate (never deactivate) the panel root, because
            // SetActive(false) on the same GameObject as this controller prevents
            // OnEnable from firing on first scene load → event subscriptions never
            // happen. Use CanvasGroup alpha for hide instead so the GameObject
            // stays active and subscriptions remain wired.
            if (visible && m_PanelRoot != null && !m_PanelRoot.activeSelf)
                m_PanelRoot.SetActive(true);

            if (m_PanelCanvasGroup != null)
            {
                m_PanelCanvasGroup.alpha = visible ? 1f : 0f;
                m_PanelCanvasGroup.interactable = visible;
                m_PanelCanvasGroup.blocksRaycasts = visible;
            }
        }

        void OnReplayClicked()
        {
            if (m_NarrationPlayer != null) m_NarrationPlayer.Replay();
        }

        void OnCloseClicked()
        {
            if (m_ActivityController != null && m_ActivityController.Current == ActivityState.Viewing)
            {
                m_ActivityController.TryTransition(ActivityState.Scanning);
            }
            SetVisible(false);
        }
    }
}
