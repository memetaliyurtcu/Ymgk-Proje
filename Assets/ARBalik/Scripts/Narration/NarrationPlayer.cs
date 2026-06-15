using System;
using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using UnityEngine;

namespace ARFishing.Narration
{
    [RequireComponent(typeof(AudioSource))]
    public class NarrationPlayer : MonoBehaviour
    {
        [SerializeField] FocusResolver m_FocusResolver;
        [SerializeField] AudioSource m_AudioSource;

        CreatureDefinition m_CurrentClipFor;

        public event Action<CreatureDefinition> NarrationStarted;
        public event Action<CreatureDefinition> NarrationFinished;

        public bool IsPlaying => m_AudioSource != null && m_AudioSource.isPlaying;
        public CreatureDefinition CurrentClipFor => m_CurrentClipFor;

        void Awake()
        {
            if (m_AudioSource == null) m_AudioSource = GetComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
            m_AudioSource.loop = false;

            if (m_FocusResolver == null) ServiceLocator.TryGet(out m_FocusResolver);
            ServiceLocator.Register(this);
        }

        void OnEnable()
        {
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged += HandleFocusChanged;
        }

        void OnDisable()
        {
            if (m_FocusResolver != null) m_FocusResolver.FocusChanged -= HandleFocusChanged;
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        void Update()
        {
            if (m_CurrentClipFor == null) return;
            if (m_AudioSource == null) return;
            if (m_AudioSource.isPlaying) return;

            var finished = m_CurrentClipFor;
            m_CurrentClipFor = null;
            NarrationFinished?.Invoke(finished);
        }

        void HandleFocusChanged(CreatureDefinition previous, CreatureDefinition next)
        {
            Stop();
            if (next != null) Play(next);
        }

        public void Play(CreatureDefinition def)
        {
            if (def == null || def.NarrationClip == null || m_AudioSource == null) return;

            m_AudioSource.Stop();
            m_AudioSource.clip = def.NarrationClip;
            m_AudioSource.Play();
            m_CurrentClipFor = def;
            NarrationStarted?.Invoke(def);
        }

        public void Replay()
        {
            if (m_CurrentClipFor != null) Play(m_CurrentClipFor);
        }

        public void Stop()
        {
            if (m_AudioSource != null) m_AudioSource.Stop();
            m_CurrentClipFor = null;
        }
    }
}
