using System.IO;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    /// <summary>
    /// Generates a 1024x1024 high-feature PNG suitable for ARCore image tracking.
    /// Synthesizes noise + colored circles + lines so the texture scores well on
    /// arcoreimg's keypoint detector (the test that rejected the previous image).
    /// Drop the generated PNG into a CardLibrary entry as a placeholder tracker image
    /// until real card art is produced (see Docs/ArtistBrief.html §5).
    /// </summary>
    public static class CreateTestMarkerMenu
    {
        const string Dir = "Assets/ARBalık/Content/CardImages";
        const int Size = 1024;

        [MenuItem("ARFishing/Create Test Marker Image (high-feature)")]
        public static void Create()
        {
            EnsureDir(Dir);
            var path = $"{Dir}/test_marker_octopus.png";

            var pixels = SynthesizePixels(seed: 42);
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[ARFishing] Test marker image created at {path}. Drag it into CardLibrary 'octopus' entry's Texture slot.");
        }

        static Color32[] SynthesizePixels(int seed)
        {
            var rng = new System.Random(seed);
            var pixels = new Color32[Size * Size];

            // Background: low-amplitude noise so there's already plenty of micro-feature.
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)(70 + rng.Next(0, 140));
                pixels[i] = new Color32(v, v, v, 255);
            }

            // Layer 1: large colored circles for global structure.
            for (int i = 0; i < 24; i++)
            {
                int cx = rng.Next(60, Size - 60);
                int cy = rng.Next(60, Size - 60);
                int r = rng.Next(40, 140);
                var color = RandomVividColor(rng);
                DrawCircle(pixels, cx, cy, r, color);
            }

            // Layer 2: medium random rectangles for hard edges.
            for (int i = 0; i < 60; i++)
            {
                int x = rng.Next(0, Size - 30);
                int y = rng.Next(0, Size - 30);
                int w = rng.Next(15, 70);
                int h = rng.Next(15, 70);
                var color = RandomVividColor(rng);
                DrawRect(pixels, x, y, w, h, color);
            }

            // Layer 3: small accent circles for keypoint density.
            for (int i = 0; i < 120; i++)
            {
                int cx = rng.Next(10, Size - 10);
                int cy = rng.Next(10, Size - 10);
                int r = rng.Next(4, 18);
                var color = RandomVividColor(rng);
                DrawCircle(pixels, cx, cy, r, color);
            }

            // Layer 4: high-contrast crisscrossing lines.
            for (int i = 0; i < 40; i++)
            {
                int x0 = rng.Next(0, Size);
                int y0 = rng.Next(0, Size);
                int x1 = rng.Next(0, Size);
                int y1 = rng.Next(0, Size);
                var color = (i % 2 == 0) ? new Color32(20, 20, 20, 255) : new Color32(235, 235, 235, 255);
                DrawLine(pixels, x0, y0, x1, y1, color, thickness: 2);
            }

            // Asymmetry marker: bias more shapes in upper-left quadrant so the
            // image is rotationally unique (ARCore wants asymmetry).
            for (int i = 0; i < 30; i++)
            {
                int cx = rng.Next(30, Size / 2);
                int cy = rng.Next(30, Size / 2);
                int r = rng.Next(8, 24);
                var color = RandomVividColor(rng);
                DrawCircle(pixels, cx, cy, r, color);
            }

            return pixels;
        }

        static Color32 RandomVividColor(System.Random rng)
        {
            // Vivid: at least one channel close to 0 and one close to 255.
            int hue = rng.Next(0, 6);
            byte high = (byte)rng.Next(200, 256);
            byte mid = (byte)rng.Next(60, 200);
            byte low = (byte)rng.Next(0, 60);
            return hue switch
            {
                0 => new Color32(high, mid, low, 255),
                1 => new Color32(high, low, mid, 255),
                2 => new Color32(mid, high, low, 255),
                3 => new Color32(low, high, mid, 255),
                4 => new Color32(mid, low, high, 255),
                _ => new Color32(low, mid, high, 255),
            };
        }

        static void DrawCircle(Color32[] pixels, int cx, int cy, int r, Color32 color)
        {
            int r2 = r * r;
            int xMin = Mathf.Max(0, cx - r);
            int xMax = Mathf.Min(Size - 1, cx + r);
            int yMin = Mathf.Max(0, cy - r);
            int yMax = Mathf.Min(Size - 1, cy + r);
            for (int y = yMin; y <= yMax; y++)
            {
                int dy = y - cy;
                for (int x = xMin; x <= xMax; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= r2)
                    {
                        pixels[y * Size + x] = color;
                    }
                }
            }
        }

        static void DrawRect(Color32[] pixels, int x, int y, int w, int h, Color32 color)
        {
            int xMax = Mathf.Min(Size - 1, x + w);
            int yMax = Mathf.Min(Size - 1, y + h);
            for (int py = Mathf.Max(0, y); py <= yMax; py++)
            {
                int row = py * Size;
                for (int px = Mathf.Max(0, x); px <= xMax; px++)
                {
                    pixels[row + px] = color;
                }
            }
        }

        static void DrawLine(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                for (int oy = -thickness; oy <= thickness; oy++)
                {
                    int py = y0 + oy;
                    if (py < 0 || py >= Size) continue;
                    int row = py * Size;
                    for (int ox = -thickness; ox <= thickness; ox++)
                    {
                        int px = x0 + ox;
                        if (px < 0 || px >= Size) continue;
                        pixels[row + px] = color;
                    }
                }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
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
