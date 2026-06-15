using System.IO;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreatePlaceholderModelsMenu
    {
        const string MaterialsDir = "Assets/ARBalık/Content/Materials";
        const string PrefabsDir = "Assets/ARBalık/Content/Models";

        [MenuItem("ARFishing/Create Placeholder Models (skip existing)")]
        public static void CreateAll()
        {
            EnsureDir(MaterialsDir);
            EnsureDir(PrefabsDir);

            var guids = AssetDatabase.FindAssets("t:CreatureDefinition");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ARFishing] No CreatureDefinition assets found. Run 'Create MVP Content' first.");
                return;
            }

            int assigned = 0;
            int skipped = 0;
            foreach (var guid in guids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(defPath);
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;

                if (def.ModelPrefab != null)
                {
                    skipped++;
                    continue;
                }

                var prefab = EnsurePrefabFor(def);
                var so = new SerializedObject(def);
                so.FindProperty("m_ModelPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ARFishing] Placeholder models: assigned {assigned}, skipped {skipped} (already had ModelPrefab).");
        }

        static GameObject EnsurePrefabFor(CreatureDefinition def)
        {
            var prefabPath = $"{PrefabsDir}/creature_{def.CreatureId}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            var primitive = PickPrimitive(def.Category);
            var color = PickColor(def.CreatureId);
            var matPath = $"{MaterialsDir}/placeholder_{def.CreatureId}.mat";
            var mat = CreateOrLoadMaterial(matPath, color);
            return CreatePrimitivePrefab(prefabPath, primitive, mat);
        }

        static PrimitiveType PickPrimitive(CreatureCategory category) => category switch
        {
            CreatureCategory.Fish => PrimitiveType.Capsule,
            CreatureCategory.Invertebrate => PrimitiveType.Cube,
            CreatureCategory.Producer => PrimitiveType.Cylinder,
            CreatureCategory.Coral => PrimitiveType.Cube,
            CreatureCategory.DeepSea => PrimitiveType.Sphere,
            CreatureCategory.Endangered => PrimitiveType.Capsule,
            CreatureCategory.Mammal => PrimitiveType.Capsule,
            CreatureCategory.Reptile => PrimitiveType.Cube,
            _ => PrimitiveType.Cube,
        };

        static Color PickColor(string creatureId)
        {
            int hash = 17;
            foreach (var c in creatureId)
            {
                hash = unchecked(hash * 31 + c);
            }
            float h = (hash & 0xFFFF) / 65535f;
            return Color.HSVToRGB(h, 0.6f, 0.85f);
        }

        static Material CreateOrLoadMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject CreatePrimitivePrefab(string path, PrimitiveType primitive, Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var temp = GameObject.CreatePrimitive(primitive);
            var renderer = temp.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;

            var col = temp.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
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
