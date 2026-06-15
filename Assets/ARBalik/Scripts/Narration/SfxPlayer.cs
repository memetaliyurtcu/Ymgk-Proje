using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARFishing.Narration
{
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource m_Source;

        [Header("Clips")]
        [SerializeField] AudioClip m_CardSpottedClip;

        [Header("Event sources")]
        [SerializeField] MarkerTracker m_Tracker;

        [Header("Tuning")]
        [SerializeField, Range(0f, 1f)] float m_CardSpottedVolume = 0.55f;

        void Awake()
        {
            if (m_Source == null) m_Source = GetComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.loop = false;

            if (m_Tracker == null) ServiceLocator.TryGet(out m_Tracker);
            ServiceLocator.Register(this);
        }

        void OnEnable()
        {
            if (m_Tracker != null) m_Tracker.Spotted += HandleSpotted;
        }

        void OnDisable()
        {
            if (m_Tracker != null) m_Tracker.Spotted -= HandleSpotted;
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        void HandleSpotted(CreatureDefinition def, ARTrackedImage image)
        {
            PlayOneShot(m_CardSpottedClip, m_CardSpottedVolume);
        }

        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null || m_Source == null) return;
            m_Source.PlayOneShot(clip, volume);
        }
    }
}
