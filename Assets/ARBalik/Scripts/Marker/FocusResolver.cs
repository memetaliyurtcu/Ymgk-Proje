using System;
using System.Collections.Generic;
using ARFishing.Core;
using ARFishing.Creatures;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARFishing.Marker
{
    public class FocusResolver : MonoBehaviour
    {
        [SerializeField] MarkerTracker m_Tracker;

        public CreatureDefinition Focused { get; private set; }
        public ARTrackedImage FocusedImage { get; private set; }

        public event Action<CreatureDefinition, CreatureDefinition> FocusChanged;

        readonly Dictionary<TrackableId, TrackedEntry> m_Tracking = new();

        struct TrackedEntry
        {
            public CreatureDefinition Definition;
            public ARTrackedImage Image;
        }

        void Awake()
        {
            if (m_Tracker == null) ServiceLocator.TryGet(out m_Tracker);
            ServiceLocator.Register(this);
        }

        void OnEnable()
        {
            if (m_Tracker == null) return;
            m_Tracker.Spotted += HandleSpotted;
            m_Tracker.Updated += HandleUpdated;
            m_Tracker.Gone += HandleGone;
        }

        void OnDisable()
        {
            if (m_Tracker == null) return;
            m_Tracker.Spotted -= HandleSpotted;
            m_Tracker.Updated -= HandleUpdated;
            m_Tracker.Gone -= HandleGone;
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        void HandleSpotted(CreatureDefinition def, ARTrackedImage image)
        {
            m_Tracking[image.trackableId] = new TrackedEntry { Definition = def, Image = image };
            SetFocus(def, image);
        }

        void HandleUpdated(CreatureDefinition def, ARTrackedImage image)
        {
            m_Tracking[image.trackableId] = new TrackedEntry { Definition = def, Image = image };
            if (Focused == null && image.trackingState == TrackingState.Tracking)
            {
                SetFocus(def, image);
            }
        }

        void HandleGone(CreatureDefinition def, ARTrackedImage image)
        {
            m_Tracking.Remove(image.trackableId);
            if (FocusedImage == null || FocusedImage.trackableId != image.trackableId) return;

            CreatureDefinition nextDef = null;
            ARTrackedImage nextImage = null;
            foreach (var entry in m_Tracking.Values)
            {
                if (entry.Image == null) continue;
                if (entry.Image.trackingState != TrackingState.Tracking) continue;
                nextDef = entry.Definition;
                nextImage = entry.Image;
                break;
            }
            SetFocus(nextDef, nextImage);
        }

        void SetFocus(CreatureDefinition def, ARTrackedImage image)
        {
            if (ReferenceEquals(Focused, def)) return;
            var previous = Focused;
            Focused = def;
            FocusedImage = image;
            FocusChanged?.Invoke(previous, def);
        }
    }
}
