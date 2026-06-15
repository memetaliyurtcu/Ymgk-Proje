using UnityEngine;

namespace ARFishing.Teacher
{
    [RequireComponent(typeof(Canvas))]
    public class ProjectionCanvasRouter : MonoBehaviour
    {
        [SerializeField] Canvas m_Canvas;
        [SerializeField, Min(1)] int m_PreferredDisplay = 1;
        [SerializeField] bool m_HideIfNoExternalDisplay = true;

        [SerializeField, Min(0f), Tooltip("Seconds between polls for display count changes. 0 disables runtime detection.")]
        float m_RecheckInterval = 2f;

        public bool ExternalDisplayActive { get; private set; }

        int m_LastDisplayCount = -1;
        float m_NextCheckTime;

        void Awake()
        {
            if (m_Canvas == null) m_Canvas = GetComponent<Canvas>();
        }

        void Start()
        {
            ApplyRouting();
        }

        void Update()
        {
            if (m_RecheckInterval <= 0f) return;
            if (Time.unscaledTime < m_NextCheckTime) return;
            m_NextCheckTime = Time.unscaledTime + m_RecheckInterval;

            int count = Display.displays.Length;
            if (count == m_LastDisplayCount) return;
            ApplyRouting();
        }

        void ApplyRouting()
        {
            if (m_Canvas == null) return;

            m_LastDisplayCount = Display.displays.Length;
            bool hasExternal = m_LastDisplayCount > m_PreferredDisplay;
            if (hasExternal)
            {
                var target = Display.displays[m_PreferredDisplay];
                if (!target.active) target.Activate();
                m_Canvas.targetDisplay = m_PreferredDisplay;
                m_Canvas.gameObject.SetActive(true);
                ExternalDisplayActive = true;
            }
            else
            {
                m_Canvas.targetDisplay = 0;
                if (m_HideIfNoExternalDisplay)
                {
                    m_Canvas.gameObject.SetActive(false);
                }
                ExternalDisplayActive = false;
            }
        }
    }
}
