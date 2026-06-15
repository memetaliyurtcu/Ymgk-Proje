using ARFishing.Core;
using ARFishing.Creatures;
using UnityEngine;

namespace ARFishing.Marker
{
    public class MarkerStateBridge : MonoBehaviour
    {
        [SerializeField] FocusResolver m_Resolver;
        [SerializeField] ActivityController m_Controller;

        void Awake()
        {
            if (m_Resolver == null) ServiceLocator.TryGet(out m_Resolver);
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);
        }

        void OnEnable()
        {
            if (m_Resolver != null) m_Resolver.FocusChanged += HandleFocusChanged;
        }

        void OnDisable()
        {
            if (m_Resolver != null) m_Resolver.FocusChanged -= HandleFocusChanged;
        }

        void HandleFocusChanged(CreatureDefinition previous, CreatureDefinition next)
        {
            if (m_Controller == null) return;

            if (next != null && m_Controller.Current == ActivityState.Scanning)
            {
                m_Controller.TryTransition(ActivityState.Viewing);
            }
            else if (next == null && m_Controller.Current == ActivityState.Viewing)
            {
                m_Controller.TryTransition(ActivityState.Scanning);
            }
        }
    }
}
