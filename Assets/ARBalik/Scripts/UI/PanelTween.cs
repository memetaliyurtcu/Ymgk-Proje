using System.Collections;
using ARFishing.Core;
using UnityEngine;

namespace ARFishing.UI
{
    public class PanelTween : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_CanvasGroup;
        [SerializeField] RectTransform m_RectTransform;

        [Header("Tuning")]
        [SerializeField, Min(0.05f)] float m_Duration = 0.28f;
        [SerializeField, Tooltip("Anchored Y offset (px) applied while hidden. Negative slides down.")]
        float m_SlideOffset = -120f;

        Vector2 m_VisiblePosition;
        bool m_Initialized;
        Coroutine m_Active;
        bool m_IsVisible;

        public bool IsVisible => m_IsVisible;

        void Awake()
        {
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
        }

        void Initialize()
        {
            if (m_Initialized) return;
            // Defensive resolve: if our Awake hasn't run yet (script execution order race),
            // resolve refs here so Hide/Show called from another component's Awake still works.
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
            if (m_RectTransform == null) return;
            m_VisiblePosition = m_RectTransform.anchoredPosition;
            m_Initialized = true;
        }

        public void Show()
        {
            Initialize();
            if (m_Active != null) StopCoroutine(m_Active);
            m_IsVisible = true;
            gameObject.SetActive(true);

            if (AccessibilityState.ReducedMotion)
            {
                ApplyInstantShown();
                return;
            }

            m_Active = StartCoroutine(AnimateTo(1f, m_VisiblePosition, deactivateOnComplete: false));
        }

        public void Hide()
        {
            Initialize();
            if (m_Active != null) StopCoroutine(m_Active);

            // If this is the first Hide call (panel was never shown), apply instant hide.
            // Otherwise animate out. Prevents the "all panels flash visible for 0.28s at app
            // launch" problem when controllers call Hide() from Awake.
            bool wasVisible = m_IsVisible;
            m_IsVisible = false;

            if (!gameObject.activeInHierarchy || !wasVisible || AccessibilityState.ReducedMotion)
            {
                ApplyInstantHidden();
                return;
            }

            var hiddenPosition = m_VisiblePosition + new Vector2(0f, m_SlideOffset);
            m_Active = StartCoroutine(AnimateTo(0f, hiddenPosition, deactivateOnComplete: false));
        }

        void ApplyInstantShown()
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 1f;
                m_CanvasGroup.interactable = true;
                m_CanvasGroup.blocksRaycasts = true;
            }
            if (m_RectTransform != null) m_RectTransform.anchoredPosition = m_VisiblePosition;
        }

        public void HideInstant()
        {
            Initialize();
            if (m_Active != null) StopCoroutine(m_Active);
            m_IsVisible = false;
            ApplyInstantHidden();
        }

        void ApplyInstantHidden()
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 0f;
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
            if (m_RectTransform != null)
            {
                m_RectTransform.anchoredPosition = m_VisiblePosition + new Vector2(0f, m_SlideOffset);
            }
            // NOTE: NOT calling SetActive(false) — keeping GameObject active so OnEnable
            // subscriptions on the panel's controller stay live. (Same defensive pattern
            // applied earlier in F10 fix for SetVisible across all panel controllers.)
        }

        IEnumerator AnimateTo(float targetAlpha, Vector2 targetPosition, bool deactivateOnComplete)
        {
            if (m_CanvasGroup == null || m_RectTransform == null) yield break;

            float startAlpha = m_CanvasGroup.alpha;
            Vector2 startPosition = m_RectTransform.anchoredPosition;
            float t = 0f;

            bool interactiveTarget = targetAlpha > 0.5f;
            m_CanvasGroup.blocksRaycasts = interactiveTarget;
            m_CanvasGroup.interactable = interactiveTarget;

            while (t < m_Duration)
            {
                t += Time.unscaledDeltaTime;
                float u = EaseOutCubic(Mathf.Clamp01(t / m_Duration));
                m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, u);
                m_RectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, u);
                yield return null;
            }

            m_CanvasGroup.alpha = targetAlpha;
            m_RectTransform.anchoredPosition = targetPosition;
            m_Active = null;

            if (deactivateOnComplete) gameObject.SetActive(false);
        }

        static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    }
}
