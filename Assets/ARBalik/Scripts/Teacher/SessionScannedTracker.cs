using System;
using System.Collections.Generic;
using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARFishing.Teacher
{
    public class SessionScannedTracker : MonoBehaviour
    {
        [SerializeField] MarkerTracker m_Tracker;
        [SerializeField] ActivityController m_Controller;

        readonly List<CreatureDefinition> m_Scanned = new();
        readonly HashSet<string> m_ScannedIds = new();

        public event Action<CreatureDefinition> ScannedAdded;
        public event Action SessionReset;

        public IReadOnlyList<CreatureDefinition> Scanned => m_Scanned;
        public int Count => m_Scanned.Count;

        void Awake()
        {
            if (m_Tracker == null) ServiceLocator.TryGet(out m_Tracker);
            if (m_Controller == null) ServiceLocator.TryGet(out m_Controller);
            ServiceLocator.Register(this);
        }

        void OnEnable()
        {
            if (m_Tracker != null) m_Tracker.Spotted += HandleSpotted;
            if (m_Controller != null) m_Controller.StateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            if (m_Tracker != null) m_Tracker.Spotted -= HandleSpotted;
            if (m_Controller != null) m_Controller.StateChanged -= HandleStateChanged;
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        void HandleSpotted(CreatureDefinition def, ARTrackedImage image)
        {
            if (def == null || string.IsNullOrEmpty(def.CreatureId)) return;
            if (!m_ScannedIds.Add(def.CreatureId)) return;
            m_Scanned.Add(def);
            ScannedAdded?.Invoke(def);
        }

        void HandleStateChanged(ActivityState previous, ActivityState next)
        {
            if (next == ActivityState.Idle && previous != ActivityState.Bootstrap)
            {
                ResetSession();
            }
        }

        public void ResetSession()
        {
            if (m_Scanned.Count == 0 && m_ScannedIds.Count == 0) return;
            m_Scanned.Clear();
            m_ScannedIds.Clear();
            SessionReset?.Invoke();
        }
    }
}
