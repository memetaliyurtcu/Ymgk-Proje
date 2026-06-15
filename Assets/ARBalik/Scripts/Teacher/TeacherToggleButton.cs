using UnityEngine;
using UnityEngine.EventSystems;

namespace ARFishing.Teacher
{
    public class TeacherToggleButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] TeacherPanelController m_Panel;
        [SerializeField, Min(0.25f)] float m_HoldDuration = 1f;

        float m_PressStartTime;
        bool m_Pressing;

        void Update()
        {
            if (!m_Pressing) return;
            if (Time.unscaledTime - m_PressStartTime < m_HoldDuration) return;

            m_Pressing = false;
            if (m_Panel != null) m_Panel.Toggle();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_PressStartTime = Time.unscaledTime;
            m_Pressing = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_Pressing = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_Pressing = false;
        }
    }
}
