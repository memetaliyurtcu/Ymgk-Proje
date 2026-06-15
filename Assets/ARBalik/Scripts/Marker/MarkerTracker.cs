using System;
using System.Collections.Generic;
using ARFishing.Core;
using ARFishing.Creatures;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARFishing.Marker
{
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class MarkerTracker : MonoBehaviour
    {
        [SerializeField] ARTrackedImageManager m_Manager;
        [SerializeField] CreatureDatabase m_Database;

        public event Action<CreatureDefinition, ARTrackedImage> Spotted;
        public event Action<CreatureDefinition, ARTrackedImage> Updated;
        public event Action<CreatureDefinition, ARTrackedImage> Gone;

        readonly Dictionary<TrackableId, CreatureDefinition> m_Active = new();

        void Awake()
        {
            if (m_Manager == null) m_Manager = GetComponent<ARTrackedImageManager>();
            if (m_Database == null) ServiceLocator.TryGet(out m_Database);
            ServiceLocator.Register(this);
        }

        void Start()
        {
            LogLibraryContents();
        }

        void LogLibraryContents()
        {
            if (m_Manager == null) { Debug.LogWarning("[MarkerTracker] m_Manager is null"); return; }
            var lib = m_Manager.referenceLibrary;
            if (lib == null) { Debug.LogWarning("[MarkerTracker] referenceLibrary is null"); return; }
            Debug.Log($"[MarkerTracker] Runtime image library has {lib.count} entries:");
            for (int i = 0; i < lib.count; i++)
            {
                var entry = lib[i];
                Debug.Log($"[MarkerTracker]   [{i}] name='{entry.name}'");
            }
            if (m_Database == null) Debug.LogWarning("[MarkerTracker] CreatureDatabase is null");
            else Debug.Log($"[MarkerTracker] CreatureDatabase has {m_Database.Count} entries");
        }

        void OnEnable()
        {
            if (m_Manager != null) m_Manager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        void OnDisable()
        {
            if (m_Manager != null) m_Manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        void OnDestroy()
        {
            ServiceLocator.Unregister(this);
        }

        void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var image in args.added)
            {
                if (TryResolve(image, out var def))
                {
                    m_Active[image.trackableId] = def;
                    Spotted?.Invoke(def, image);
                }
            }

            foreach (var image in args.updated)
            {
                if (m_Active.TryGetValue(image.trackableId, out var def))
                {
                    Updated?.Invoke(def, image);
                }
                else if (TryResolve(image, out def))
                {
                    m_Active[image.trackableId] = def;
                    Spotted?.Invoke(def, image);
                }
            }

            foreach (var pair in args.removed)
            {
                if (m_Active.TryGetValue(pair.Key, out var def))
                {
                    m_Active.Remove(pair.Key);
                    Gone?.Invoke(def, pair.Value);
                }
            }
        }

        bool TryResolve(ARTrackedImage image, out CreatureDefinition def)
        {
            def = null;
            if (image == null || m_Database == null) return false;
            var name = image.referenceImage.name;
            bool ok = !string.IsNullOrEmpty(name) && m_Database.TryGet(name, out def);
            Debug.Log($"[MarkerTracker] Resolve refName='{name}' found={ok} def='{def?.DisplayName ?? "null"}'");
            return ok;
        }
    }
}
