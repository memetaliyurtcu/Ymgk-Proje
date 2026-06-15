using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreateUISpritesMenu
    {
        const string SpritesDir = "Assets/ARBalık/Content/UI/Sprites";
        const int Size = 128;

        [MenuItem("ARFishing/Create UI Sprites")]
        public static void CreateAll()
        {
            EnsureDir(SpritesDir);

            var bubblePath = $"{SpritesDir}/bubble.png";
            if (!File.Exists(Path.GetFullPath(bubblePath)))
            {
                WriteBubblePng(bubblePath);
            }

            AssetDatabase.Refresh();
            ConfigureAsSprite(bubblePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ARFishing] UI sprites created at " + SpritesDir);
        }

        static void WriteBubblePng(string assetPath)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            float center = Size * 0.5f;
            float outerRadius = Size * 0.46f;
            float softEdge = 6f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int idx = y * Size + x;
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > outerRadius)
                    {
                        pixels[idx] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float t = Mathf.InverseLerp(outerRadius, outerRadius - softEdge, dist);
                    byte alpha = (byte)(Mathf.Clamp01(t) * 255);
                    pixels[idx] = new Color32(255, 255, 255, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void ConfigureAsSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (Mathf.Abs(importer.spritePixelsPerUnit - Size) > 0.01f)
            {
                importer.spritePixelsPerUnit = Size;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
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
