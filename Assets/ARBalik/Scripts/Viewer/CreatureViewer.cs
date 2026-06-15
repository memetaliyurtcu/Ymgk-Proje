using System.Collections.Generic;
using ARFishing.Core;
using ARFishing.Creatures;
using ARFishing.Marker;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARFishing.Viewer
{
    public class CreatureViewer : MonoBehaviour
    {
        [SerializeField] MarkerTracker m_Tracker;

        [SerializeField, Tooltip("Fallback card width in meters if ARTrackedImage.size is zero.")]
        float m_FallbackCardWidth = 0.105f;

        readonly Dictionary<TrackableId, GameObject> m_Spawned = new();

        bool m_ShowModels = true;

        public bool ShowModels
        {
            get => m_ShowModels;
            set
            {
                if (m_ShowModels == value) return;
                m_ShowModels = value;
                foreach (var go in m_Spawned.Values)
                {
                    if (go != null) go.SetActive(value);
                }
            }
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
            if (def == null || def.ModelPrefab == null || image == null) return;
            if (m_Spawned.ContainsKey(image.trackableId)) return;

            var instance = Instantiate(def.ModelPrefab, image.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            ApplyScale(instance, image);
            instance.SetActive(m_ShowModels && image.trackingState == TrackingState.Tracking);
            m_Spawned[image.trackableId] = instance;
        }

        void HandleUpdated(CreatureDefinition def, ARTrackedImage image)
        {
            if (!m_Spawned.TryGetValue(image.trackableId, out var instance))
            {
                HandleSpotted(def, image);
                return;
            }
            if (instance == null) return;

            instance.SetActive(m_ShowModels && image.trackingState == TrackingState.Tracking);
            ApplyScale(instance, image);
        }

        void HandleGone(CreatureDefinition def, ARTrackedImage image)
        {
            if (!m_Spawned.TryGetValue(image.trackableId, out var instance)) return;
            if (instance != null) Destroy(instance);
            m_Spawned.Remove(image.trackableId);
        }

        void ApplyScale(GameObject instance, ARTrackedImage image)
        {
            var width = image.size.x > 0f ? image.size.x : m_FallbackCardWidth;
            instance.transform.localScale = Vector3.one * width;
        }
    }
}
