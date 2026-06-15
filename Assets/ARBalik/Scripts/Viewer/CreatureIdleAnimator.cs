using UnityEngine;

namespace ARFishing.Viewer
{
    /// <summary>
    /// Adds gentle life to a stylized creature prefab: slow Y-axis rotation + small vertical bob.
    /// Phase-randomized so multiple instances on the table don't tick in lock-step.
    /// </summary>
    public class CreatureIdleAnimator : MonoBehaviour
    {
        [SerializeField, Tooltip("Degrees per second around the local Y axis.")]
        float m_RotationSpeed = 18f;

        [SerializeField, Tooltip("Vertical bob amplitude in local units. Card-scaled by CreatureViewer.")]
        float m_BobAmplitude = 0.02f;

        [SerializeField, Tooltip("Bob cycles per second.")]
        float m_BobFrequency = 0.6f;

        Vector3 m_BasePosition;
        float m_PhaseOffset;

        void Awake()
        {
            m_BasePosition = transform.localPosition;
            m_PhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            transform.Rotate(0f, m_RotationSpeed * Time.deltaTime, 0f, Space.Self);
            float bob = Mathf.Sin(Time.time * m_BobFrequency * Mathf.PI * 2f + m_PhaseOffset) * m_BobAmplitude;
            var p = m_BasePosition;
            p.y += bob;
            transform.localPosition = p;
        }
    }
}
