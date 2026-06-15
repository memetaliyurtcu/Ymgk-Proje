using System.Collections.Generic;
using System.IO;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreateMvpContentMenu
    {
        const string ContentDir = "Assets/ARBalık/Content";
        const string CreaturesDir = "Assets/ARBalık/Content/Creatures";

        [MenuItem("ARFishing/Create MVP Content (5 creatures)")]
        public static void CreateAll()
        {
            EnsureDir(CreaturesDir);

            var creatures = new List<CreatureDefinition>();

            creatures.Add(MakeCreature("octopus", "Ahtapot",
                CreatureCategory.Invertebrate, Habitat.Reef, DietType.Carnivore, EcosystemRole.Predator,
                new[] { "avlanma", "habitat kaybı" },
                "Üç kalbi ve mavi renkli kanı vardır; tehlike anında mürekkep fışkırtarak kaçar."));

            creatures.Add(MakeCreature("clownfish", "Palyaço Balığı",
                CreatureCategory.Fish, Habitat.Reef, DietType.Omnivore, EcosystemRole.Prey,
                new[] { "mercan beyazlaması", "habitat kaybı" },
                "Tüm palyaço balıkları erkek doğar; grup lideri dişi ölünce en güçlü erkek dişiye dönüşür."));

            creatures.Add(MakeCreature("great-white-shark", "Beyaz Köpekbalığı",
                CreatureCategory.Fish, Habitat.OpenSea, DietType.Carnivore, EcosystemRole.Predator,
                new[] { "avlanma", "plastik kirliliği" },
                "Dişleri ömrü boyunca sürekli yenilenir; hayatı boyunca on binlerce diş üretebilir."));

            creatures.Add(MakeCreature("dolphin", "Yunus",
                CreatureCategory.Mammal, Habitat.OpenSea, DietType.Carnivore, EcosystemRole.Predator,
                new[] { "ağa takılma", "plastik kirliliği", "gürültü kirliliği" },
                "Uyurken beyninin yalnızca yarısını kapatır; diğer yarısı uyanık kalarak nefes almayı unutmaz."));

            creatures.Add(MakeCreature("sea-turtle", "Deniz Kaplumbağası",
                CreatureCategory.Reptile, Habitat.OpenSea, DietType.Omnivore, EcosystemRole.Prey,
                new[] { "plastik kirliliği", "avlanma", "kıyı kaybı" },
                "Dünya'nın manyetik alanını GPS gibi kullanarak doğduğu plaja geri döner ve yumurtalar."));

            var creatureDb = CreateOrLoad<CreatureDatabase>($"{ContentDir}/CreatureDatabase.asset");
            SetCreaturesArray(creatureDb, creatures);

            foreach (var c in creatures) EditorUtility.SetDirty(c);
            EditorUtility.SetDirty(creatureDb);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = creatureDb;
            EditorGUIUtility.PingObject(creatureDb);
            Debug.Log($"[ARFishing] MVP content scaffolded: {creatures.Count} creatures.");
        }

        static CreatureDefinition MakeCreature(string id, string displayName,
            CreatureCategory category, Habitat habitat, DietType diet, EcosystemRole role,
            string[] threats, string trait)
        {
            var path = $"{CreaturesDir}/{id}.asset";
            var def = CreateOrLoad<CreatureDefinition>(path);
            var so = new SerializedObject(def);
            so.FindProperty("m_CreatureId").stringValue = id;
            so.FindProperty("m_DisplayName").stringValue = displayName;
            so.FindProperty("m_Category").intValue = (int)category;
            so.FindProperty("m_Habitat").intValue = (int)habitat;
            so.FindProperty("m_Diet").intValue = (int)diet;
            so.FindProperty("m_EcosystemRole").intValue = (int)role;

            var threatsArr = so.FindProperty("m_Threats");
            threatsArr.arraySize = threats?.Length ?? 0;
            if (threats != null)
            {
                for (int i = 0; i < threats.Length; i++)
                    threatsArr.GetArrayElementAtIndex(i).stringValue = threats[i];
            }

            so.FindProperty("m_InterestingTrait").stringValue = trait;
            so.FindProperty("m_ReferenceImageName").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        static void SetCreaturesArray(CreatureDatabase database, List<CreatureDefinition> creatures)
        {
            var so = new SerializedObject(database);
            var arr = so.FindProperty("m_Creatures");
            arr.arraySize = creatures.Count;
            for (int i = 0; i < creatures.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = creatures[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        static void EnsureDir(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            var parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
