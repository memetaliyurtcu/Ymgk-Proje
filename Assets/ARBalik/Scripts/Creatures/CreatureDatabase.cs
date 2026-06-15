using System.Collections.Generic;
using UnityEngine;

namespace ARFishing.Creatures
{
    [CreateAssetMenu(menuName = "ARFishing/Creature Database", fileName = "CreatureDatabase")]
    public class CreatureDatabase : ScriptableObject
    {
        [SerializeField] CreatureDefinition[] m_Creatures;

        Dictionary<string, CreatureDefinition> m_Lookup;

        public IReadOnlyList<CreatureDefinition> All => m_Creatures;

        public int Count => m_Creatures?.Length ?? 0;

        public bool TryGet(string creatureId, out CreatureDefinition definition)
        {
            EnsureLookup();
            return m_Lookup.TryGetValue(creatureId ?? string.Empty, out definition);
        }

        public CreatureDefinition GetOrNull(string creatureId)
        {
            EnsureLookup();
            return m_Lookup.TryGetValue(creatureId ?? string.Empty, out var def) ? def : null;
        }

        void OnEnable()
        {
            m_Lookup = null;
        }

        void EnsureLookup()
        {
            if (m_Lookup != null) return;
            var capacity = m_Creatures?.Length ?? 0;
            m_Lookup = new Dictionary<string, CreatureDefinition>(capacity);
            if (m_Creatures == null) return;
            foreach (var def in m_Creatures)
            {
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                m_Lookup[def.CreatureId] = def;
            }
        }
    }
}
