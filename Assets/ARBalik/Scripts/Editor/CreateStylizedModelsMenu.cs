using System.Collections.Generic;
using System.IO;
using ARFishing.Creatures;
using ARFishing.Viewer;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    /// <summary>
    /// Builds stylized procedural prefabs (recognizable silhouettes from Unity primitives)
    /// for each MVP creature and wires them into CreatureDefinition.m_ModelPrefab.
    /// Overwrites existing prefabs at the same paths used by the simpler placeholder menu,
    /// so re-running upgrades from "blob shapes" to "looks like an octopus / dolphin / shark".
    /// Each prefab also gets a CreatureIdleAnimator so the model rotates + bobs in-place.
    /// </summary>
    public static class CreateStylizedModelsMenu
    {
        const string MaterialsDir = "Assets/ARBalık/Content/Materials";
        const string PrefabsDir = "Assets/ARBalık/Content/Models";
        const string MeshesDir = "Assets/ARBalık/Content/Models/Meshes";

        delegate void Builder(Transform root, Material primary, Material accent);

        [MenuItem("ARFishing/Create Stylized Procedural Models (overwrite)")]
        public static void CreateAll()
        {
            EnsureDir(MaterialsDir);
            EnsureDir(PrefabsDir);
            EnsureDir(MeshesDir);

            var builders = GetBuilders();
            var palette = GetPalette();
            var guids = AssetDatabase.FindAssets("t:CreatureDefinition");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ARFishing] No CreatureDefinition assets found. Run 'Create MVP Content' first.");
                return;
            }

            int built = 0;
            int unhandled = 0;
            foreach (var guid in guids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(defPath);
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;

                if (!builders.TryGetValue(def.CreatureId, out var builder))
                {
                    Debug.LogWarning($"[ARFishing] No builder for '{def.CreatureId}', skipping.");
                    unhandled++;
                    continue;
                }

                var (primaryColor, accentColor) = palette.TryGetValue(def.CreatureId, out var p)
                    ? p : (new Color(0.55f, 0.55f, 0.55f), new Color(0.85f, 0.85f, 0.85f));

                var primary = EnsureMaterial($"{MaterialsDir}/mat_{def.CreatureId}.mat", primaryColor);
                var accent = EnsureMaterial($"{MaterialsDir}/mat_{def.CreatureId}_accent.mat", accentColor);

                var prefabPath = $"{PrefabsDir}/creature_{def.CreatureId}.prefab";
                var prefab = BuildAndSavePrefab(prefabPath, def.CreatureId, builder, primary, accent);

                var so = new SerializedObject(def);
                so.FindProperty("m_ModelPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ARFishing] Stylized models: built {built}, unhandled {unhandled}.");
        }

        // ---------- prefab assembly ----------

        static GameObject BuildAndSavePrefab(string path, string id, Builder builder, Material primary, Material accent)
        {
            var root = new GameObject($"creature_{id}");
            builder(root.transform, primary, accent);
            root.AddComponent<CreatureIdleAnimator>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Transform AddPart(Transform parent, PrimitiveType primitive, Vector3 pos, Vector3 euler, Vector3 scale, Material mat, string name = null)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name ?? primitive.ToString();
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            return go.transform;
        }

        static Transform AddMeshPart(Transform parent, Mesh mesh, Vector3 pos, Vector3 euler, Vector3 scale, Material mat, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
        }

        static Material EnsureMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------- palette ----------

        static Dictionary<string, (Color primary, Color accent)> GetPalette()
        {
            Color C(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f);
            return new Dictionary<string, (Color, Color)>
            {
                { "octopus",            (C(180,  90, 150), C(120,  60, 100)) },
                { "moon-jellyfish",     (C(200, 220, 240), C(150, 180, 220)) },
                { "clownfish",          (C(230, 100,  40), C(245, 245, 245)) },
                { "great-white-shark",  (C(120, 130, 140), C(220, 220, 220)) },
                { "seahorse",           (C(220, 170,  60), C(180, 130,  40)) },
                { "moray-eel",          (C( 90, 110,  70), C(200, 200,  90)) },
                { "stingray",           (C(100, 100, 110), C( 70,  70,  80)) },
                { "anglerfish",         (C( 40,  40,  55), C(255, 220,  90)) },
                { "parrotfish",         (C( 60, 180, 180), C(230, 140, 200)) },
                { "crab",               (C(200,  70,  50), C(140,  40,  30)) },
                { "starfish",           (C(230, 130,  90), C(180,  80,  50)) },
                { "mussel",             (C( 60,  50,  70), C(120, 100, 130)) },
                { "squid",              (C(230, 180, 190), C(200, 130, 140)) },
                { "sea-urchin",         (C( 80,  50, 110), C( 40,  20,  60)) },
                { "coral",              (C(240, 130, 130), C(220, 100, 110)) },
                { "green-algae",        (C( 90, 180,  90), C( 60, 140,  70)) },
                { "seagrass",           (C( 80, 160,  90), C( 60, 130,  70)) },
                { "plankton",           (C(160, 230, 240), C(110, 200, 220)) },
                { "sea-turtle",         (C( 80, 130,  90), C(140, 180, 130)) },
                { "dolphin",            (C(140, 160, 180), C(220, 230, 240)) },
            };
        }

        // ---------- builder registry ----------

        static Dictionary<string, Builder> GetBuilders() => new Dictionary<string, Builder>
        {
            { "octopus", BuildOctopus },
            { "moon-jellyfish", BuildJellyfish },
            { "clownfish", BuildClownfish },
            { "great-white-shark", BuildShark },
            { "seahorse", BuildSeahorse },
            { "moray-eel", BuildMorayEel },
            { "stingray", BuildStingray },
            { "anglerfish", BuildAnglerfish },
            { "parrotfish", BuildParrotfish },
            { "crab", BuildCrab },
            { "starfish", BuildStarfish },
            { "mussel", BuildMussel },
            { "squid", BuildSquid },
            { "sea-urchin", BuildSeaUrchin },
            { "coral", BuildCoral },
            { "green-algae", BuildGreenAlgae },
            { "seagrass", BuildSeagrass },
            { "plankton", BuildPlankton },
            { "sea-turtle", BuildSeaTurtle },
            { "dolphin", BuildDolphin },
        };

        // ---------- individual creatures ----------

        static void BuildOctopus(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.35f, 0), Vector3.zero, new Vector3(0.55f, 0.45f, 0.55f), primary, "Body");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.1f, 0.45f, 0.2f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f), accent, "Eye_L");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.1f, 0.45f, 0.2f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.08f), accent, "Eye_R");
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                float radius = 0.22f;
                AddPart(root, PrimitiveType.Capsule,
                    new Vector3(Mathf.Sin(a) * radius, 0.1f, Mathf.Cos(a) * radius),
                    new Vector3(20f, i * 45f, 0f),
                    new Vector3(0.06f, 0.18f, 0.06f),
                    primary, $"Tentacle_{i}");
            }
        }

        static void BuildJellyfish(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.35f, 0), Vector3.zero, new Vector3(0.7f, 0.4f, 0.7f), primary, "Bell");
            var offsets = new[] { new Vector2(0.18f, 0.18f), new Vector2(-0.18f, 0.18f), new Vector2(0.18f, -0.18f), new Vector2(-0.18f, -0.18f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                AddPart(root, PrimitiveType.Capsule,
                    new Vector3(offsets[i].x, 0.0f, offsets[i].y),
                    Vector3.zero,
                    new Vector3(0.035f, 0.25f, 0.035f),
                    accent, $"Tentacle_{i}");
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                AddPart(root, PrimitiveType.Capsule,
                    new Vector3(Mathf.Sin(a) * 0.25f, 0.15f, Mathf.Cos(a) * 0.25f),
                    Vector3.zero,
                    new Vector3(0.02f, 0.18f, 0.02f),
                    accent, $"Wisp_{i}");
            }
        }

        static void BuildClownfish(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.25f, 0), Vector3.zero, new Vector3(0.32f, 0.28f, 0.55f), primary, "Body");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.25f, -0.35f), new Vector3(0, 0, 0), new Vector3(0.05f, 0.3f, 0.18f), primary, "TailFin");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), Vector3.zero, new Vector3(0.04f, 0.18f, 0.2f), primary, "Dorsal");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.25f,  0.1f), Vector3.zero, new Vector3(0.36f, 0.32f, 0.08f), accent, "Stripe_F");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.25f, -0.1f), Vector3.zero, new Vector3(0.36f, 0.32f, 0.08f), accent, "Stripe_B");
            AddPart(root, PrimitiveType.Sphere, new Vector3(0.13f, 0.32f, 0.2f), Vector3.zero, new Vector3(0.04f, 0.04f, 0.04f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.13f, 0.32f, 0.2f), Vector3.zero, new Vector3(0.04f, 0.04f, 0.04f), accent, "Eye_L");
        }

        static void BuildShark(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.3f, 0), new Vector3(90, 0, 0), new Vector3(0.28f, 0.45f, 0.28f), primary, "Body");
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.22f, 0), new Vector3(90, 0, 0), new Vector3(0.27f, 0.44f, 0.18f), accent, "Belly");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.6f, 0.05f), new Vector3(0, 0, 0), new Vector3(0.05f, 0.25f, 0.18f), primary, "Dorsal");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.35f, -0.5f), new Vector3(0, 0, 0), new Vector3(0.04f, 0.35f, 0.22f), primary, "TailVert");
            AddPart(root, PrimitiveType.Cube, new Vector3( 0.3f, 0.22f, 0.05f), new Vector3(0, 0, -25f), new Vector3(0.25f, 0.04f, 0.15f), primary, "PectoralR");
            AddPart(root, PrimitiveType.Cube, new Vector3(-0.3f, 0.22f, 0.05f), new Vector3(0, 0,  25f), new Vector3(0.25f, 0.04f, 0.15f), primary, "PectoralL");
        }

        static void BuildSeahorse(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.45f, 0), Vector3.zero, new Vector3(0.16f, 0.25f, 0.18f), primary, "Body");
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.75f, 0.05f), new Vector3(20, 0, 0), new Vector3(0.13f, 0.12f, 0.13f), primary, "Head");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.78f, 0.18f), Vector3.zero, new Vector3(0.07f, 0.06f, 0.18f), accent, "Snout");
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.15f, -0.1f), new Vector3(-40, 0, 0), new Vector3(0.1f, 0.2f, 0.1f), primary, "TailUpper");
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.0f, -0.05f), new Vector3(-80, 0, 0), new Vector3(0.08f, 0.15f, 0.08f), primary, "TailCurl");
            AddPart(root, PrimitiveType.Cube, new Vector3(0.13f, 0.4f, -0.05f), new Vector3(0, 0, 0), new Vector3(0.05f, 0.08f, 0.15f), accent, "DorsalFin");
            AddPart(root, PrimitiveType.Sphere, new Vector3(0.07f, 0.78f, 0.12f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), accent, "Eye");
        }

        static void BuildMorayEel(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.2f, 0), new Vector3(90, 0, 0), new Vector3(0.1f, 0.5f, 0.1f), primary, "Body");
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.2f, 0.45f), Vector3.zero, new Vector3(0.14f, 0.13f, 0.16f), primary, "Head");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.2f, 0.55f), new Vector3(0, 0, 0), new Vector3(0.12f, 0.04f, 0.05f), accent, "Mouth");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.07f, 0.25f, 0.5f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.07f, 0.25f, 0.5f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), accent, "Eye_L");
        }

        static void BuildStingray(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.08f, 0), Vector3.zero, new Vector3(0.75f, 0.1f, 0.6f), primary, "Body");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.08f, -0.45f), new Vector3(0, 0, 0), new Vector3(0.05f, 0.04f, 0.6f), primary, "Tail");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.1f, 0.13f, 0.25f), Vector3.zero, new Vector3(0.04f, 0.04f, 0.04f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.1f, 0.13f, 0.25f), Vector3.zero, new Vector3(0.04f, 0.04f, 0.04f), accent, "Eye_L");
        }

        static void BuildAnglerfish(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.32f, 0), Vector3.zero, new Vector3(0.6f, 0.5f, 0.55f), primary, "Body");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.18f, 0.3f), new Vector3(0, 0, 0), new Vector3(0.5f, 0.18f, 0.18f), primary, "Mouth");
            for (int i = 0; i < 6; i++)
            {
                float x = -0.18f + i * 0.075f;
                AddPart(root, PrimitiveType.Cube, new Vector3(x, 0.21f, 0.39f), new Vector3(0, 0, 45), new Vector3(0.03f, 0.05f, 0.03f), accent, $"Tooth_{i}");
            }
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.7f, 0.15f), new Vector3(20, 0, 0), new Vector3(0.025f, 0.18f, 0.025f), primary, "LureStalk");
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 1.0f, 0.27f), Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f), accent, "LureBulb");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.18f, 0.4f, 0.3f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.18f, 0.4f, 0.3f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), accent, "Eye_L");
        }

        static void BuildParrotfish(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.28f, 0), Vector3.zero, new Vector3(0.4f, 0.32f, 0.55f), primary, "Body");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.28f, -0.35f), Vector3.zero, new Vector3(0.05f, 0.32f, 0.22f), accent, "TailFin");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), Vector3.zero, new Vector3(0.05f, 0.2f, 0.35f), accent, "Dorsal");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.05f, 0.05f), Vector3.zero, new Vector3(0.32f, 0.04f, 0.4f), accent, "Belly");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.28f, 0.27f), Vector3.zero, new Vector3(0.18f, 0.08f, 0.06f), accent, "Beak");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.17f, 0.34f, 0.18f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), primary, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.17f, 0.34f, 0.18f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), primary, "Eye_L");
        }

        static void BuildCrab(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.18f, 0), Vector3.zero, new Vector3(0.55f, 0.2f, 0.4f), primary, "Body");
            for (int i = 0; i < 4; i++)
            {
                float z = 0.15f - i * 0.1f;
                AddPart(root, PrimitiveType.Cube, new Vector3( 0.35f, 0.16f, z), new Vector3(0, 0, -25f), new Vector3(0.35f, 0.04f, 0.05f), primary, $"LegR_{i}");
                AddPart(root, PrimitiveType.Cube, new Vector3(-0.35f, 0.16f, z), new Vector3(0, 0,  25f), new Vector3(0.35f, 0.04f, 0.05f), primary, $"LegL_{i}");
            }
            AddPart(root, PrimitiveType.Cube, new Vector3( 0.3f, 0.22f, 0.3f), new Vector3(0, 30, -15), new Vector3(0.18f, 0.1f, 0.08f), primary, "ClawR");
            AddPart(root, PrimitiveType.Cube, new Vector3(-0.3f, 0.22f, 0.3f), new Vector3(0, -30, 15), new Vector3(0.18f, 0.1f, 0.08f), primary, "ClawL");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.1f, 0.3f, 0.18f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.1f, 0.3f, 0.18f), Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), accent, "Eye_L");
        }

        static void BuildStarfish(Transform root, Material primary, Material accent)
        {
            var starMesh = CreateOrLoadStarMesh($"{MeshesDir}/star_5point.asset", points: 5, outerR: 0.5f, innerR: 0.18f, thickness: 0.08f);
            AddMeshPart(root, starMesh, new Vector3(0, 0.1f, 0), new Vector3(0, 0, 0), Vector3.one, primary, "StarBody");
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                AddPart(root, PrimitiveType.Sphere,
                    new Vector3(Mathf.Sin(a) * 0.42f, 0.14f, Mathf.Cos(a) * 0.42f),
                    Vector3.zero,
                    new Vector3(0.04f, 0.04f, 0.04f),
                    accent, $"TipDot_{i}");
            }
        }

        static void BuildMussel(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.12f, 0), new Vector3(0, 0, 0), new Vector3(0.4f, 0.22f, 0.55f), primary, "ShellBottom");
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.28f, 0), new Vector3(180, 0, 0), new Vector3(0.4f, 0.22f, 0.55f), primary, "ShellTop");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.2f, 0.02f), Vector3.zero, new Vector3(0.42f, 0.02f, 0.55f), accent, "Hinge");
            for (int i = 0; i < 5; i++)
            {
                float z = -0.2f + i * 0.1f;
                AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.21f, z), Vector3.zero, new Vector3(0.45f, 0.005f, 0.01f), accent, $"Rib_{i}");
            }
        }

        static void BuildSquid(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.55f, 0), Vector3.zero, new Vector3(0.22f, 0.4f, 0.22f), primary, "Mantle");
            AddPart(root, PrimitiveType.Cube, new Vector3( 0.15f, 0.85f, 0), new Vector3(0, 0, -30), new Vector3(0.18f, 0.04f, 0.12f), accent, "FinR");
            AddPart(root, PrimitiveType.Cube, new Vector3(-0.15f, 0.85f, 0), new Vector3(0, 0,  30), new Vector3(0.18f, 0.04f, 0.12f), accent, "FinL");
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                AddPart(root, PrimitiveType.Cylinder,
                    new Vector3(Mathf.Sin(a) * 0.07f, 0.12f, Mathf.Cos(a) * 0.07f),
                    new Vector3(0, i * 45f, 0),
                    new Vector3(0.025f, 0.16f, 0.025f),
                    primary, $"Arm_{i}");
            }
            AddPart(root, PrimitiveType.Cylinder, new Vector3( 0.05f, 0.08f, 0.0f), Vector3.zero, new Vector3(0.025f, 0.26f, 0.025f), accent, "LongTentacleR");
            AddPart(root, PrimitiveType.Cylinder, new Vector3(-0.05f, 0.08f, 0.0f), Vector3.zero, new Vector3(0.025f, 0.26f, 0.025f), accent, "LongTentacleL");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.12f, 0.45f, 0.15f), Vector3.zero, new Vector3(0.06f, 0.06f, 0.06f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.12f, 0.45f, 0.15f), Vector3.zero, new Vector3(0.06f, 0.06f, 0.06f), accent, "Eye_L");
        }

        static void BuildSeaUrchin(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.3f, 0), Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f), primary, "Body");
            int spikes = 28;
            for (int i = 0; i < spikes; i++)
            {
                float u = i / (float)spikes;
                float theta = u * Mathf.PI * 2f * 6f;
                float phi = Mathf.Acos(1f - 2f * u);
                var dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                var pos = new Vector3(0, 0.3f, 0) + dir * 0.28f;
                AddPart(root, PrimitiveType.Cylinder,
                    pos,
                    Quaternion.FromToRotation(Vector3.up, dir).eulerAngles,
                    new Vector3(0.015f, 0.16f, 0.015f),
                    accent, $"Spike_{i}");
            }
        }

        static void BuildCoral(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.18f, 0), Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), primary, "Base");
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), Vector3.zero, new Vector3(0.13f, 0.2f, 0.13f), primary, "Trunk");
            var branches = new (Vector3 pos, Vector3 euler, Vector3 scale)[]
            {
                (new Vector3( 0.12f, 0.6f,  0.0f), new Vector3(0, 0, -35), new Vector3(0.08f, 0.18f, 0.08f)),
                (new Vector3(-0.12f, 0.62f, 0.05f), new Vector3(0, 0,  35), new Vector3(0.08f, 0.18f, 0.08f)),
                (new Vector3( 0.0f,  0.7f, -0.15f), new Vector3(-30, 0, 0), new Vector3(0.08f, 0.18f, 0.08f)),
                (new Vector3( 0.0f,  0.72f,0.15f), new Vector3( 30, 0, 0), new Vector3(0.08f, 0.18f, 0.08f)),
                (new Vector3( 0.18f, 0.85f, 0.0f), new Vector3(0, 0, -45), new Vector3(0.06f, 0.14f, 0.06f)),
                (new Vector3(-0.18f, 0.88f, 0.0f), new Vector3(0, 0,  45), new Vector3(0.06f, 0.14f, 0.06f)),
            };
            foreach (var (p, e, s) in branches) AddPart(root, PrimitiveType.Cylinder, p, e, s, primary, "Branch");
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                AddPart(root, PrimitiveType.Sphere,
                    new Vector3(Mathf.Sin(a) * 0.2f, 1.05f, Mathf.Cos(a) * 0.2f),
                    Vector3.zero,
                    new Vector3(0.05f, 0.05f, 0.05f),
                    accent, $"Polyp_{i}");
            }
        }

        static void BuildGreenAlgae(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.05f, 0), Vector3.zero, new Vector3(0.15f, 0.08f, 0.15f), accent, "Holdfast");
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                AddPart(root, PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(a) * 0.1f, 0.32f, Mathf.Cos(a) * 0.1f),
                    new Vector3(Mathf.Cos(a) * 18f, i * 72f, Mathf.Sin(a) * 18f),
                    new Vector3(0.32f, 0.5f, 0.04f),
                    primary, $"Blade_{i}");
            }
        }

        static void BuildSeagrass(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.04f, 0), Vector3.zero, new Vector3(0.2f, 0.06f, 0.2f), accent, "Roots");
            int blades = 8;
            for (int i = 0; i < blades; i++)
            {
                float a = i * (360f / blades) * Mathf.Deg2Rad;
                float r = 0.05f + (i % 2) * 0.04f;
                float tilt = 5f + (i % 3) * 4f;
                AddPart(root, PrimitiveType.Cube,
                    new Vector3(Mathf.Sin(a) * r, 0.35f, Mathf.Cos(a) * r),
                    new Vector3(Mathf.Cos(a) * tilt, i * 30f, -Mathf.Sin(a) * tilt),
                    new Vector3(0.03f, 0.65f, 0.06f),
                    primary, $"Blade_{i}");
            }
        }

        static void BuildPlankton(Transform root, Material primary, Material accent)
        {
            var rng = new System.Random(42);
            int count = 18;
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rng.NextDouble() - 0.5) * 0.5f;
                float y = 0.15f + (float)rng.NextDouble() * 0.4f;
                float z = (float)(rng.NextDouble() - 0.5) * 0.5f;
                float s = 0.05f + (float)rng.NextDouble() * 0.06f;
                var mat = (i % 3 == 0) ? accent : primary;
                AddPart(root, PrimitiveType.Sphere, new Vector3(x, y, z), Vector3.zero, new Vector3(s, s, s), mat, $"Speck_{i}");
            }
        }

        static void BuildSeaTurtle(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.22f, 0), Vector3.zero, new Vector3(0.6f, 0.22f, 0.55f), primary, "Shell");
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;
                AddPart(root, PrimitiveType.Cube,
                    new Vector3(x * 0.15f, 0.34f, z * 0.15f),
                    Vector3.zero,
                    new Vector3(0.1f, 0.02f, 0.1f),
                    accent, $"Scute_{x}{z}");
            }
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.22f, 0.35f), Vector3.zero, new Vector3(0.18f, 0.14f, 0.2f), accent, "Head");
            AddPart(root, PrimitiveType.Cube, new Vector3( 0.32f, 0.2f,  0.18f), new Vector3(0, 35, -15), new Vector3(0.3f, 0.04f, 0.18f), accent, "FlipperFR");
            AddPart(root, PrimitiveType.Cube, new Vector3(-0.32f, 0.2f,  0.18f), new Vector3(0, -35, 15), new Vector3(0.3f, 0.04f, 0.18f), accent, "FlipperFL");
            AddPart(root, PrimitiveType.Cube, new Vector3( 0.28f, 0.2f, -0.2f), new Vector3(0, -25, -10), new Vector3(0.22f, 0.04f, 0.14f), accent, "FlipperBR");
            AddPart(root, PrimitiveType.Cube, new Vector3(-0.28f, 0.2f, -0.2f), new Vector3(0, 25, 10), new Vector3(0.22f, 0.04f, 0.14f), accent, "FlipperBL");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.22f, -0.35f), Vector3.zero, new Vector3(0.06f, 0.04f, 0.1f), accent, "Tail");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.07f, 0.27f, 0.42f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), primary, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.07f, 0.27f, 0.42f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), primary, "Eye_L");
        }

        static void BuildDolphin(Transform root, Material primary, Material accent)
        {
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.3f, 0), new Vector3(90, 0, 0), new Vector3(0.25f, 0.4f, 0.25f), primary, "Body");
            AddPart(root, PrimitiveType.Capsule, new Vector3(0, 0.22f, 0), new Vector3(90, 0, 0), new Vector3(0.24f, 0.38f, 0.16f), accent, "Belly");
            AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.32f, 0.4f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.22f), primary, "Head");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.3f, 0.55f), Vector3.zero, new Vector3(0.1f, 0.06f, 0.1f), primary, "Rostrum");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.55f, 0.05f), new Vector3(0, 0, 0), new Vector3(0.04f, 0.22f, 0.18f), primary, "Dorsal");
            AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.3f, -0.5f), new Vector3(0, 0, 0), new Vector3(0.45f, 0.04f, 0.14f), primary, "TailFluke");
            AddPart(root, PrimitiveType.Cube, new Vector3( 0.25f, 0.22f, 0.1f), new Vector3(0, 25, -15), new Vector3(0.22f, 0.04f, 0.12f), primary, "PectoralR");
            AddPart(root, PrimitiveType.Cube, new Vector3(-0.25f, 0.22f, 0.1f), new Vector3(0, -25, 15), new Vector3(0.22f, 0.04f, 0.12f), primary, "PectoralL");
            AddPart(root, PrimitiveType.Sphere, new Vector3( 0.13f, 0.35f, 0.45f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), accent, "Eye_R");
            AddPart(root, PrimitiveType.Sphere, new Vector3(-0.13f, 0.35f, 0.45f), Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), accent, "Eye_L");
        }

        // ---------- custom meshes ----------

        static Mesh CreateOrLoadStarMesh(string assetPath, int points, float outerR, float innerR, float thickness)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null) return existing;

            var verts = new List<Vector3>();
            var tris = new List<int>();

            int ring = points * 2;
            float half = thickness * 0.5f;
            verts.Add(new Vector3(0, half, 0));
            verts.Add(new Vector3(0, -half, 0));
            int topCenter = 0;
            int bottomCenter = 1;

            for (int i = 0; i < ring; i++)
            {
                float a = (i / (float)ring) * Mathf.PI * 2f;
                float r = (i % 2 == 0) ? outerR : innerR;
                verts.Add(new Vector3(Mathf.Sin(a) * r, half, Mathf.Cos(a) * r));
                verts.Add(new Vector3(Mathf.Sin(a) * r, -half, Mathf.Cos(a) * r));
            }

            for (int i = 0; i < ring; i++)
            {
                int next = (i + 1) % ring;
                int topI = 2 + i * 2;
                int topN = 2 + next * 2;
                int botI = 2 + i * 2 + 1;
                int botN = 2 + next * 2 + 1;

                tris.Add(topCenter); tris.Add(topN); tris.Add(topI);
                tris.Add(bottomCenter); tris.Add(botI); tris.Add(botN);
                tris.Add(topI); tris.Add(topN); tris.Add(botI);
                tris.Add(botI); tris.Add(topN); tris.Add(botN);
            }

            var mesh = new Mesh { name = "Star_5Point" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        // ---------- helpers ----------

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
