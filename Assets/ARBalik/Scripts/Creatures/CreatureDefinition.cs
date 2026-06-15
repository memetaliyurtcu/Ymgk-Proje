using UnityEngine;

namespace ARFishing.Creatures
{
    [CreateAssetMenu(menuName = "ARFishing/Creature Definition", fileName = "creature-id")]
    public class CreatureDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable kebab-case ID. Must match the XRReferenceImageLibrary entry name.")]
        string m_CreatureId;

        [SerializeField] string m_DisplayName;
        [SerializeField] Sprite m_Icon;

        [Header("Classification")]
        [SerializeField] CreatureCategory m_Category;
        [SerializeField] Habitat m_Habitat;
        [SerializeField] DietType m_Diet;
        [SerializeField] EcosystemRole m_EcosystemRole;

        [Header("Descriptive")]
        [SerializeField] string[] m_Threats;
        [SerializeField, TextArea(2, 4)] string m_InterestingTrait;

        [Header("Presentation")]
        [SerializeField] GameObject m_ModelPrefab;
        [SerializeField] AudioClip m_NarrationClip;

        [Header("Tracking")]
        [SerializeField, Tooltip("Reference image name in XRReferenceImageLibrary. Usually equals CreatureId.")]
        string m_ReferenceImageName;

        public string CreatureId => m_CreatureId;
        public string DisplayName => m_DisplayName;
        public Sprite Icon => m_Icon;
        public CreatureCategory Category => m_Category;
        public Habitat Habitat => m_Habitat;
        public DietType Diet => m_Diet;
        public EcosystemRole EcosystemRole => m_EcosystemRole;
        public string[] Threats => m_Threats;
        public string InterestingTrait => m_InterestingTrait;
        public GameObject ModelPrefab => m_ModelPrefab;
        public AudioClip NarrationClip => m_NarrationClip;
        public string ReferenceImageName => m_ReferenceImageName;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ReferenceImageName) && !string.IsNullOrEmpty(m_CreatureId))
            {
                m_ReferenceImageName = m_CreatureId;
            }
        }
#endif
    }
}
