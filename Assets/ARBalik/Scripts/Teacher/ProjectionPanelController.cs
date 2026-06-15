using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using UnityEngine;
using UnityEngine.UI;

namespace ARFishing.Teacher
{
    public class ProjectionPanelController : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] ActivityController m_Controller;
        [SerializeField] FocusResolver m_FocusResolver;
        [SerializeField] SessionScannedTracker m_SessionTracker;

        [Header("Sections")]
        [SerializeField] GameObject m_ScanningSection;
        [SerializeField] GameObject m_IdleSection;

        [Header("Scanning section")]
        [SerializeField] Image m_LargeIcon;
        [SerializeField] Text m_LargeName;
        [SerializeField] Text m_LargeCategory;

        void Awake()
        {
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);
            if (m_FocusResolver == null) ServiceLocator.TryGet(out m_FocusResolver);
            if (m_SessionTracker == null) ServiceLocator.TryGet(out m_SessionTracker);
        }

        void OnEnable()
        {
            if (m_Controller != null) m_Controller.StateChanged += HandleStateChanged;
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged += HandleFocusChanged;

            ApplyState(m_Controller != null ? m_Controller.Current : ActivityState.Bootstrap);
        }

        void OnDisable()
        {
            if (m_Controller != null) m_Controller.StateChanged -= HandleStateChanged;
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged -= HandleFocusChanged;
        }

        void HandleStateChanged(ActivityState previous, ActivityState next)
        {
            ApplyState(next);
        }

        void ApplyState(ActivityState state)
        {
            SetSection(m_IdleSection, state == ActivityState.Idle);
            SetSection(m_ScanningSection,
                state == ActivityState.Scanning || state == ActivityState.Viewing);
        }

        void HandleFocusChanged(CreatureDefinition previous, CreatureDefinition next)
        {
            PopulateFocus(next);
        }

        void PopulateFocus(CreatureDefinition def)
        {
            if (m_LargeIcon != null)
            {
                m_LargeIcon.sprite = def != null ? def.Icon : null;
                m_LargeIcon.enabled = def != null && def.Icon != null;
            }
            if (m_LargeName != null) m_LargeName.text = def != null ? def.DisplayName : "";
            if (m_LargeCategory != null) m_LargeCategory.text = def != null ? def.Category.ToTurkish() : "";
        }

        static void SetSection(GameObject section, bool visible)
        {
            if (section != null) section.SetActive(visible);
        }
    }
}
