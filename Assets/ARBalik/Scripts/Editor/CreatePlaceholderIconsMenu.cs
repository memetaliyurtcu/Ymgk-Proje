using System.IO;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreatePlaceholderIconsMenu
    {
        const string IconsDir = "Assets/ARBalık/Content/Icons";
        const int IconSize = 256;

        [MenuItem("ARFishing/Create Placeholder Icons (skip existing)")]
        public static void CreateAll()
        {
            EnsureDir(IconsDir);

            var guids = AssetDatabase.FindAssets("t:CreatureDefinition");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ARFishing] No CreatureDefinition assets found. Run 'Create MVP Content' first.");
                return;
            }

            // First pass: write PNG files for creatures without an Icon.
            foreach (var guid in guids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(defPath);
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                if (def.Icon != null) continue;

                var iconPath = $"{IconsDir}/icon_{def.CreatureId}.png";
                if (File.Exists(Path.GetFullPath(iconPath))) continue;

                WriteIconPng(iconPath, def);
            }

            AssetDatabase.Refresh();

            // Second pass: set Sprite import settings + assign references.
            int assigned = 0, skipped = 0;
            foreach (var guid in guids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(defPath);
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;

                if (def.Icon != null) { skipped++; continue; }

                var iconPath = $"{IconsDir}/icon_{def.CreatureId}.png";
                var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                if (importer != null)
                {
                    if (importer.textureType != TextureImporterType.Sprite ||
                        Mathf.Abs(importer.spritePixelsPerUnit - IconSize) > 0.01f)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = IconSize;
                        importer.alphaIsTransparency = true;
                        importer.SaveAndReimport();
                    }
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sprite == null) continue;

                var so = new SerializedObject(def);
                so.FindProperty("m_Icon").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ARFishing] Placeholder icons: assigned {assigned}, skipped {skipped} (already had Icon).");
        }

        static void WriteIconPng(string assetPath, CreatureDefinition def)
        {
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[IconSize * IconSize];

            var bg = CategoryBgColor(def.Category);
            var fg = CreatureColor(def.CreatureId);

            const float cornerRadius = 32f;
            const float outerInset = 16f;
            const float innerCircleRadius = IconSize * 0.30f;
            const float innerRingThickness = 4f;

            float cx = IconSize * 0.5f;
            float cy = IconSize * 0.5f;

            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    int idx = y * IconSize + x;

                    bool insideRoundedRect = IsInRoundedRect(
                        x, y,
                        outerInset, outerInset,
                        IconSize - outerInset, IconSize - outerInset,
                        cornerRadius);

                    if (!insideRoundedRect)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= innerCircleRadius)
                    {
                        pixels[idx] = fg;
                    }
                    else if (dist <= innerCircleRadius + innerRingThickness)
                    {
                        pixels[idx] = Color.Lerp(fg, bg, 0.5f);
                    }
                    else
                    {
                        pixels[idx] = bg;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static bool IsInRoundedRect(int x, int y, float minX, float minY, float maxX, float maxY, float radius)
        {
            if (x < minX || x > maxX || y < minY || y > maxY) return false;
            if (x < minX + radius && y < minY + radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(minX + radius, minY + radius)) <= radius;
            if (x > maxX - radius && y < minY + radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(maxX - radius, minY + radius)) <= radius;
            if (x < minX + radius && y > maxY - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(minX + radius, maxY - radius)) <= radius;
            if (x > maxX - radius && y > maxY - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(maxX - radius, maxY - radius)) <= radius;
            return true;
        }

        static Color32 CategoryBgColor(CreatureCategory category) => category switch
        {
            CreatureCategory.Fish        => new Color32(214, 236, 247, 255),
            CreatureCategory.Invertebrate => new Color32(253, 230, 205, 255),
            CreatureCategory.Producer    => new Color32(217, 242, 220, 255),
            CreatureCategory.Coral       => new Color32(252, 214, 208, 255),
            CreatureCategory.DeepSea     => new Color32(213, 204, 230, 255),
            CreatureCategory.Endangered  => new Color32(240, 226, 192, 255),
            CreatureCategory.Mammal      => new Color32(223, 228, 235, 255),
            CreatureCategory.Reptile     => new Color32(228, 236, 204, 255),
            _                            => new Color32(220, 220, 220, 255),
        };

        static Color32 CreatureColor(string creatureId)
        {
            int hash = 17;
            foreach (var ch in creatureId) hash = unchecked(hash * 31 + ch);
            float h = (hash & 0xFFFF) / 65535f;
            var color = Color.HSVToRGB(h, 0.65f, 0.78f);
            return new Color32(
                (byte)(color.r * 255f),
                (byte)(color.g * 255f),
                (byte)(color.b * 255f),
                255);
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
