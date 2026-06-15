using System.IO;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreateExampleContentMenu
    {
        const string ContentDir = "Assets/ARBalık/Content";
        const string CreaturesDir = "Assets/ARBalık/Content/Creatures";

        [MenuItem("ARFishing/Create Example Content")]
        public static void CreateExamples()
        {
            EnsureDir(ContentDir);
            EnsureDir(CreaturesDir);

            var octopus = CreateOrLoad<CreatureDefinition>($"{CreaturesDir}/octopus.asset");
            FillOctopus(octopus);

            var jellyfish = CreateOrLoad<CreatureDefinition>($"{CreaturesDir}/moon-jellyfish.asset");
            FillJellyfish(jellyfish);

            var database = CreateOrLoad<CreatureDatabase>($"{ContentDir}/CreatureDatabase.asset");
            SetDatabaseEntries(database, new[] { octopus, jellyfish });

            EditorUtility.SetDirty(octopus);
            EditorUtility.SetDirty(jellyfish);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
            Debug.Log("[ARFishing] Example content created: octopus, moon-jellyfish, CreatureDatabase.");
        }

        static void FillOctopus(CreatureDefinition def)
        {
            var so = new SerializedObject(def);
            so.FindProperty("m_CreatureId").stringValue = "octopus";
            so.FindProperty("m_DisplayName").stringValue = "Ahtapot";
            so.FindProperty("m_Category").intValue = (int)CreatureCategory.Invertebrate;
            so.FindProperty("m_Habitat").intValue = (int)Habitat.Reef;
            so.FindProperty("m_Diet").intValue = (int)DietType.Carnivore;
            so.FindProperty("m_EcosystemRole").intValue = (int)EcosystemRole.Predator;

            var threats = so.FindProperty("m_Threats");
            threats.arraySize = 2;
            threats.GetArrayElementAtIndex(0).stringValue = "avlanma";
            threats.GetArrayElementAtIndex(1).stringValue = "habitat kaybı";

            so.FindProperty("m_InterestingTrait").stringValue =
                "Renk değiştirerek kamufle olabilir ve dar yerlere sığabilir.";
            so.FindProperty("m_ReferenceImageName").stringValue = "octopus";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void FillJellyfish(CreatureDefinition def)
        {
            var so = new SerializedObject(def);
            so.FindProperty("m_CreatureId").stringValue = "moon-jellyfish";
            so.FindProperty("m_DisplayName").stringValue = "Ay Denizanası";
            so.FindProperty("m_Category").intValue = (int)CreatureCategory.Invertebrate;
            so.FindProperty("m_Habitat").intValue = (int)Habitat.OpenSea;
            so.FindProperty("m_Diet").intValue = (int)DietType.FilterFeeder;
            so.FindProperty("m_EcosystemRole").intValue = (int)EcosystemRole.Predator;

            var threats = so.FindProperty("m_Threats");
            threats.arraySize = 2;
            threats.GetArrayElementAtIndex(0).stringValue = "deniz kirliliği";
            threats.GetArrayElementAtIndex(1).stringValue = "iklim değişikliği";

            so.FindProperty("m_InterestingTrait").stringValue =
                "Bazı türleri ışık saçabilir; dokunaçları küçük canlıları yakalar.";
            so.FindProperty("m_ReferenceImageName").stringValue = "moon-jellyfish";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetDatabaseEntries(CreatureDatabase database, CreatureDefinition[] entries)
        {
            var so = new SerializedObject(database);
            var arr = so.FindProperty("m_Creatures");
            arr.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }
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
