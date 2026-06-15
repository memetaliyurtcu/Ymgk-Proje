using System.Collections.Generic;
using ARFishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.UI
{
    public class IdleBackgroundAnimator : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] RectTransform m_Container;
        [SerializeField] Sprite m_BubbleSprite;

        [Header("Spawn")]
        [SerializeField, Min(1)] int m_BubbleCount = 14;
        [SerializeField] Vector2 m_SizeRange = new(24f, 80f);

        [Header("Motion")]
        [SerializeField] Vector2 m_SpeedRange = new(18f, 55f);
        [SerializeField] Vector2 m_DriftAmplitudeRange = new(12f, 36f);
        [SerializeField] Color m_BubbleColor = new(0.6f, 0.88f, 1f, 0.4f);

        readonly List<Bubble> m_Bubbles = new();

        struct Bubble
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public float Speed;
            public float DriftPhase;
            public float DriftAmplitude;
            public float MaxAlpha;
        }

        void Start()
        {
            if (m_Container == null) m_Container = transform as RectTransform;
            if (AccessibilityState.ReducedMotion) return;
            SpawnBubbles();
        }

        void Update()
        {
            if (m_Container == null) return;
            if (AccessibilityState.ReducedMotion) return;

            float width = m_Container.rect.width;
            float height = m_Container.rect.height;
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;

            for (int i = 0; i < m_Bubbles.Count; i++)
            {
                var b = m_Bubbles[i];
                if (b.Rect == null) continue;

                var pos = b.Rect.anchoredPosition;
                pos.y += b.Speed * dt;
                pos.x += Mathf.Sin(now + b.DriftPhase) * b.DriftAmplitude * dt;

                float topBoundary = height * 0.5f + 60f;
                float bottomBoundary = -height * 0.5f - 60f;

                if (pos.y > topBoundary)
                {
                    pos.y = bottomBoundary;
                    pos.x = Random.Range(-width * 0.5f, width * 0.5f);
                }

                b.Rect.anchoredPosition = pos;

                if (b.Group != null)
                {
                    float fadeT = Mathf.InverseLerp(bottomBoundary, topBoundary, pos.y);
                    float envelope = Mathf.Clamp01(Mathf.Sin(fadeT * Mathf.PI));
                    b.Group.alpha = envelope * b.MaxAlpha;
                }
            }
        }

        void SpawnBubbles()
        {
            if (m_Container == null) return;

            float width = m_Container.rect.width;
            float height = m_Container.rect.height;

            for (int i = 0; i < m_BubbleCount; i++)
            {
                var go = new GameObject($"Bubble_{i}", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.SetParent(m_Container, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                float size = Random.Range(m_SizeRange.x, m_SizeRange.y);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = new Vector2(
                    Random.Range(-width * 0.5f, width * 0.5f),
                    Random.Range(-height * 0.5f, height * 0.5f)
                );

                var image = go.AddComponent<Image>();
                image.sprite = m_BubbleSprite;
                image.color = m_BubbleColor;
                image.raycastTarget = false;
                image.preserveAspect = true;

                var group = go.AddComponent<CanvasGroup>();
                group.alpha = m_BubbleColor.a;
                group.blocksRaycasts = false;
                group.interactable = false;

                m_Bubbles.Add(new Bubble
                {
                    Rect = rect,
                    Group = group,
                    Speed = Random.Range(m_SpeedRange.x, m_SpeedRange.y),
                    DriftPhase = Random.Range(0f, Mathf.PI * 2f),
                    DriftAmplitude = Random.Range(m_DriftAmplitudeRange.x, m_DriftAmplitudeRange.y),
                    MaxAlpha = m_BubbleColor.a,
                });
            }
        }
    }
}
