using ARFishing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.UI
{
    /// <summary>
    /// Generic button helper that triggers an ActivityController state transition on click.
    /// Drop on any UI Button GameObject, pick a target state in the Inspector, and wire
    /// the controller (or rely on ServiceLocator). Useful for testing flows before the
    /// TeacherPanel (F6) is wired — e.g. "Mini quiz başlat" or "Etkinliği başlat".
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TransitionButton : MonoBehaviour
    {
        [SerializeField] ActivityController m_Controller;
        [SerializeField] ActivityState m_TargetState = ActivityState.Scanning;
        [SerializeField] Button m_Button;

        void Awake()
        {
            if (m_Button == null) m_Button = GetComponent<Button>();
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);
        }

        void OnEnable()
        {
            if (m_Button != null) m_Button.onClick.AddListener(HandleClick);
        }

        void OnDisable()
        {
            if (m_Button != null) m_Button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick()
        {
            if (m_Controller == null) return;
            m_Controller.TryTransition(m_TargetState);
        }
    }
}
