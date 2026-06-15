using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARFishing.Localization
{
    [CreateAssetMenu(menuName = "ARFishing/Localization Table", fileName = "LocalizationTable")]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Stable string key — e.g. 'ui.button.start_activity'.")]
            public string Key;

            public string Turkish;
            public string English;
            public string Arabic;
        }

        [SerializeField] Entry[] m_Entries;

        Dictionary<string, Entry> m_Lookup;

        public int Count => m_Entries?.Length ?? 0;

        public bool TryGet(string key, Locale locale, out string value)
        {
            EnsureLookup();
            value = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (!m_Lookup.TryGetValue(key, out var entry)) return false;

            value = locale switch
            {
                Locale.Turkish => entry.Turkish,
                Locale.English => entry.English,
                Locale.Arabic => entry.Arabic,
                _ => entry.Turkish,
            };

            if (string.IsNullOrEmpty(value)) value = entry.Turkish;
            return !string.IsNullOrEmpty(value);
        }

        public string Get(string key, Locale locale, string fallback = null)
        {
            return TryGet(key, locale, out var value) ? value : (fallback ?? key);
        }

        void OnEnable() { m_Lookup = null; }

        void EnsureLookup()
        {
            if (m_Lookup != null) return;
            m_Lookup = new Dictionary<string, Entry>(m_Entries?.Length ?? 0);
            if (m_Entries == null) return;
            foreach (var entry in m_Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key)) continue;
                m_Lookup[entry.Key] = entry;
            }
        }
    }
}
