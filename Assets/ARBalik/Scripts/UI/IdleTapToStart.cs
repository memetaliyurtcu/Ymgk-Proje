using ARFishing.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ARFishing.UI
{
    /// <summary>
    /// Drops on the IdlePanel (or any UI Graphic with raycastTarget=true) to let a tap
    /// on the panel advance the FSM from Idle to Scanning. Useful before TeacherPanel
    /// (F6) is wired — gives a child-facing way to start the activity. Can be removed
    /// once the Teacher long-press flow is in place.
    /// </summary>
    public class IdleTapToStart : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] ActivityController m_Controller;

        void Awake()
        {
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_Controller == null) return;
            if (m_Controller.Current != ActivityState.Idle) return;
            m_Controller.TryTransition(ActivityState.Scanning);
        }
    }
}
