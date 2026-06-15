using System;
using System.IO;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARFishing.Editor
{
    /// <summary>
    /// Generates a unique high-feature procedural PNG for each creature in CreatureDatabase,
    /// overlays a computed AR tracking quality score, and auto-populates CardLibrary.asset.
    /// Run "ARFishing/Create Test Marker Set" after "Create MVP Content".
    /// </summary>
    public static class CreateTestMarkerSetMenu
    {
        const string MarkersDir = "Assets/ARBalık/Content/CardImages";
        const string CardLibraryPath = "Assets/ARBalık/Content/CardLibrary.asset";
        const int Size = 1024;

        [MenuItem("ARFishing/Create Test Marker Set (5 creatures + CardLibrary)")]
        public static void Create()
        {
            var db = LoadCreatureDatabase();
            if (db == null) return;

            EnsureDir(MarkersDir);

            int generated = 0;
            foreach (var def in db.All)
            {
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                var path = $"{MarkersDir}/marker_{def.CreatureId}.png";
                WriteMarkerPng(path, def.CreatureId);
                generated++;
            }

            AssetDatabase.Refresh();

            foreach (var def in db.All)
            {
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                ConfigureImporter($"{MarkersDir}/marker_{def.CreatureId}.png");
            }

            Debug.Log($"[ARFishing] Marker textures generated: {generated} with AR score overlays.");

            int populated = TryPopulateCardLibrary(db);
            if (populated < 0)
                Debug.LogWarning("[ARFishing] Could not auto-populate CardLibrary. " +
                    "Open CardLibrary.asset manually: Add Image per creature, Name=creatureId, " +
                    "Specify Size=on, width=0.105m, drag the marker PNG into Texture.");
            else
                Debug.Log($"[ARFishing] CardLibrary populated: {populated} entries.");
        }

        // ── texture synthesis ─────────────────────────────────────────────────

        static void WriteMarkerPng(string assetPath, string creatureId)
        {
            var pixels = SynthesizeForCreature(creatureId);
            int score = ComputeArScore(pixels);
            OverlayArScore(pixels, score, creatureId);

            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static Color32[] SynthesizeForCreature(string creatureId)
        {
            int seed = StableHash(creatureId);
            var rng = new System.Random(seed);
            var pixels = new Color32[Size * Size];

            // Background: low-amplitude gray noise
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)(70 + rng.Next(0, 140));
                pixels[i] = new Color32(v, v, v, 255);
            }

            // Large colored circles — global structure
            for (int i = 0; i < 24; i++)
                DrawCircle(pixels, rng.Next(60, Size - 60), rng.Next(60, Size - 60),
                    rng.Next(40, 140), RandomVividColor(rng));

            // Medium rectangles — hard edges for keypoints
            for (int i = 0; i < 60; i++)
                DrawRect(pixels, rng.Next(0, Size - 30), rng.Next(0, Size - 30),
                    rng.Next(15, 70), rng.Next(15, 70), RandomVividColor(rng));

            // Small accent circles — keypoint density
            for (int i = 0; i < 120; i++)
                DrawCircle(pixels, rng.Next(10, Size - 10), rng.Next(10, Size - 10),
                    rng.Next(4, 18), RandomVividColor(rng));

            // High-contrast lines — additional edge features
            for (int i = 0; i < 40; i++)
            {
                var c = (i % 2 == 0) ? new Color32(20, 20, 20, 255) : new Color32(235, 235, 235, 255);
                DrawLine(pixels, rng.Next(0, Size), rng.Next(0, Size),
                    rng.Next(0, Size), rng.Next(0, Size), c, 2);
            }

            // Asymmetry bias — cluster extra shapes in one corner per creature
            BiasClusterToCorner(pixels, seed & 3, rng);

            // Unique signature — a high-contrast block at a creature-specific grid position
            DrawCreatureSignature(pixels, seed);

            return pixels;
        }

        // ── AR score computation ──────────────────────────────────────────────

        // Edge pixel ratio (horizontal + vertical contrast > threshold) is a proxy
        // for ORB/FAST keypoint density, which is what ARCore uses for its quality score.
        static int ComputeArScore(Color32[] pixels)
        {
            const int threshold = 30;
            long edgeCount = 0;
            long total = (Size - 2) * (Size - 2);

            for (int y = 1; y < Size - 1; y++)
            {
                for (int x = 1; x < Size - 1; x++)
                {
                    int g  = Gray(pixels[y * Size + x]);
                    int gR = Gray(pixels[y * Size + x + 1]);
                    int gU = Gray(pixels[(y + 1) * Size + x]);
                    if (Math.Abs(g - gR) > threshold || Math.Abs(g - gU) > threshold)
                        edgeCount++;
                }
            }

            // ~30-50% edge ratio = excellent quality marker; calibrated so typical
            // procedural images land 70-90%.
            double ratio = (double)edgeCount / total;
            return (int)Math.Min(100, ratio * 200.0);
        }

        static int Gray(Color32 c) => (c.r + c.g + c.b) / 3;

        // ── score overlay ─────────────────────────────────────────────────────

        // Note: Unity Texture2D y=0 is bottom; EncodeToPNG flips to PNG top-down.
        // Drawing at low y values places content at the bottom of the saved PNG.

        static void OverlayArScore(Color32[] pixels, int score, string creatureId)
        {
            const int bannerH = 110;

            // Dark banner background at bottom of PNG (y=0..bannerH-1 in Unity coords)
            var bg = new Color32(18, 18, 24, 255);
            DrawRect(pixels, 0, 0, Size, bannerH, bg);

            // Score bar (y=12..31) — colored progress fill + gray track
            const int barMargin = 40;
            const int barY = 12, barH = 20;
            int fillW = (int)(score / 100.0 * (Size - barMargin * 2));
            int trackW = Size - barMargin * 2 - fillW;

            Color32 barColor = score >= 75
                ? new Color32(48, 210, 80, 255)
                : score >= 45
                    ? new Color32(255, 195, 30, 255)
                    : new Color32(215, 55, 45, 255);

            DrawRect(pixels, barMargin, barY, fillW, barH, barColor);
            DrawRect(pixels, barMargin + fillW, barY, trackW, barH, new Color32(55, 55, 60, 255));

            // Text label: "AR: XX%" drawn above the bar (y=48 baseline = bottom of chars)
            // Scale=8 → each char is 3*8=24px wide, 5*8=40px tall
            const int scale = 8;
            string label = $"AR: {score}%";
            DrawText(pixels, label, barMargin, 48, scale, new Color32(235, 235, 235, 255));

            // Creature ID in smaller font (scale=5) to the right
            const int idScale = 5;
            int idX = barMargin + label.Length * (3 * scale + scale) + 20;
            DrawText(pixels, creatureId.ToUpper(), idX, 48, idScale, barColor);
        }

        // ── bitmap font (3×5 per glyph, drawn with DrawRect blocks) ──────────

        // Patterns are stored top-row-first. DrawChar compensates for Unity's
        // y-flip so glyphs appear correct in the saved PNG.
        static readonly int[][] s_Digits =
        {
            new[] {7,5,5,5,7}, // 0: 111 101 101 101 111
            new[] {2,6,2,2,7}, // 1: 010 110 010 010 111
            new[] {7,1,7,4,7}, // 2: 111 001 111 100 111
            new[] {7,1,7,1,7}, // 3: 111 001 111 001 111
            new[] {5,5,7,1,1}, // 4: 101 101 111 001 001
            new[] {7,4,7,1,7}, // 5: 111 100 111 001 111
            new[] {7,4,7,5,7}, // 6: 111 100 111 101 111
            new[] {7,1,2,2,2}, // 7: 111 001 010 010 010
            new[] {7,5,7,5,7}, // 8: 111 101 111 101 111
            new[] {7,5,7,1,7}, // 9: 111 101 111 001 111
        };

        static int[] GlyphFor(char c)
        {
            if (c >= '0' && c <= '9') return s_Digits[c - '0'];
            switch (char.ToUpper(c))
            {
                case 'A': return new[] {2,5,7,5,5}; // 010 101 111 101 101
                case 'B': return new[] {6,5,6,5,6}; // 110 101 110 101 110
                case 'C': return new[] {7,4,4,4,7}; // 111 100 100 100 111
                case 'D': return new[] {6,5,5,5,6}; // 110 101 101 101 110
                case 'E': return new[] {7,4,7,4,7}; // 111 100 111 100 111
                case 'F': return new[] {7,4,7,4,4}; // 111 100 111 100 100
                case 'G': return new[] {7,4,5,5,7}; // 111 100 101 101 111
                case 'H': return new[] {5,5,7,5,5}; // 101 101 111 101 101
                case 'I': return new[] {7,2,2,2,7}; // 111 010 010 010 111
                case 'K': return new[] {5,5,6,5,5}; // 101 101 110 101 101
                case 'L': return new[] {4,4,4,4,7}; // 100 100 100 100 111
                case 'M': return new[] {5,7,5,5,5}; // 101 111 101 101 101
                case 'N': return new[] {5,7,7,5,5}; // 101 111 111 101 101
                case 'O': return new[] {2,5,5,5,2}; // 010 101 101 101 010
                case 'P': return new[] {6,5,6,4,4}; // 110 101 110 100 100
                case 'R': return new[] {6,5,6,5,5}; // 110 101 110 101 101
                case 'S': return new[] {7,4,7,1,7}; // 111 100 111 001 111
                case 'T': return new[] {7,2,2,2,2}; // 111 010 010 010 010
                case 'U': return new[] {5,5,5,5,7}; // 101 101 101 101 111
                case 'W': return new[] {5,5,7,7,5}; // 101 101 111 111 101
                case 'Y': return new[] {5,5,2,2,2}; // 101 101 010 010 010
                case ':': return new[] {0,2,0,2,0}; // 000 010 000 010 000
                case '-': return new[] {0,0,7,0,0}; // 000 000 111 000 000
                case '%': return new[] {5,1,2,4,5}; // 101 001 010 100 101
                case '_': return new[] {0,0,0,0,7}; // 000 000 000 000 111
                default:  return new[] {0,0,0,0,0}; // space
            }
        }

        // startY = bottom of character in Unity texture space.
        // Rows are drawn top-to-bottom in PNG by reversing the y direction,
        // compensating for Unity's EncodeToPNG vertical flip.
        static void DrawText(Color32[] pixels, string text, int startX, int startY, int scale, Color32 color)
        {
            int x = startX;
            foreach (char c in text)
            {
                DrawChar(pixels, GlyphFor(c), x, startY, scale, color);
                x += 3 * scale + scale;
            }
        }

        static void DrawChar(Color32[] pixels, int[] pattern, int x, int y, int scale, Color32 color)
        {
            int numRows = pattern.Length;
            for (int row = 0; row < numRows; row++)
            {
                int bits = pattern[row];
                // row 0 = top of glyph in PNG → highest Unity y; row (n-1) = bottom → y
                int unityY = y + (numRows - 1 - row) * scale;
                for (int col = 0; col < 3; col++)
                {
                    if (((bits >> (2 - col)) & 1) == 0) continue;
                    DrawRect(pixels, x + col * scale, unityY, scale, scale, color);
                }
            }
        }

        // ── creature signature ────────────────────────────────────────────────

        static void DrawCreatureSignature(Color32[] pixels, int seed)
        {
            uint us = (uint)seed;
            int posIdx = (int)(us % 20);
            int cellW = Size / 5, cellH = Size / 4;
            int cx = cellW * (posIdx % 5) + cellW / 2;
            int cy = cellH * (posIdx / 5) + cellH / 2;

            bool dark = ((us >> 1) & 1) == 0;
            bool square = ((us >> 2) & 1) == 0;
            Color32 outer = dark ? new Color32(15, 15, 15, 255) : new Color32(245, 245, 245, 255);
            Color32 inner = dark ? new Color32(245, 245, 245, 255) : new Color32(15, 15, 15, 255);

            if (square)
            {
                DrawRect(pixels, cx - 110, cy - 110, 220, 220, outer);
                DrawRect(pixels, cx - 55,  cy - 55,  110, 110, inner);
            }
            else
            {
                DrawCircle(pixels, cx, cy, 110, outer);
                DrawCircle(pixels, cx, cy, 55,  inner);
            }
            DrawCircle(pixels, cx, cy, 12, outer);
        }

        static void BiasClusterToCorner(Color32[] pixels, int corner, System.Random rng)
        {
            int xMin, xMax, yMin, yMax;
            switch (corner)
            {
                case 0: xMin=30; xMax=Size/2-30; yMin=30; yMax=Size/2-30; break;
                case 1: xMin=Size/2+30; xMax=Size-30; yMin=30; yMax=Size/2-30; break;
                case 2: xMin=30; xMax=Size/2-30; yMin=Size/2+30; yMax=Size-30; break;
                default: xMin=Size/2+30; xMax=Size-30; yMin=Size/2+30; yMax=Size-30; break;
            }
            for (int i = 0; i < 30; i++)
                DrawCircle(pixels, rng.Next(xMin, xMax), rng.Next(yMin, yMax),
                    rng.Next(8, 24), RandomVividColor(rng));
        }

        // ── drawing primitives ────────────────────────────────────────────────

        static Color32 RandomVividColor(System.Random rng)
        {
            byte high = (byte)rng.Next(200, 256);
            byte mid  = (byte)rng.Next(60, 200);
            byte low  = (byte)rng.Next(0, 60);
            return rng.Next(0, 6) switch
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
            for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(Size - 1, cy + r); y++)
            {
                int dy = y - cy, row = y * Size;
                for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(Size - 1, cx + r); x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= r2) pixels[row + x] = color;
                }
            }
        }

        static void DrawRect(Color32[] pixels, int x, int y, int w, int h, Color32 color)
        {
            for (int py = Mathf.Max(0, y); py <= Mathf.Min(Size - 1, y + h); py++)
            {
                int row = py * Size;
                for (int px = Mathf.Max(0, x); px <= Mathf.Min(Size - 1, x + w); px++)
                    pixels[row + px] = color;
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
                    int rowBase = py * Size;
                    for (int ox = -thickness; ox <= thickness; ox++)
                    {
                        int px = x0 + ox;
                        if (px >= 0 && px < Size) pixels[rowBase + px] = color;
                    }
                }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // ── CardLibrary auto-population ───────────────────────────────────────

        static int TryPopulateCardLibrary(CreatureDatabase db)
        {
            try
            {
                var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(CardLibraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                    AssetDatabase.CreateAsset(library, CardLibraryPath);
                }

                var so = new SerializedObject(library);
                var imagesProp = so.FindProperty("m_Images");
                if (imagesProp == null) return -1;

                int updated = 0;
                foreach (var def in db.All)
                {
                    if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                    var texPath = $"{MarkersDir}/marker_{def.CreatureId}.png";
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    if (tex == null) continue;

                    // Find existing entry or append
                    int existIdx = -1;
                    for (int i = 0; i < imagesProp.arraySize; i++)
                    {
                        var np = imagesProp.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name");
                        if (np != null && np.stringValue == def.CreatureId) { existIdx = i; break; }
                    }

                    SerializedProperty entry;
                    if (existIdx >= 0)
                        entry = imagesProp.GetArrayElementAtIndex(existIdx);
                    else
                    {
                        imagesProp.arraySize++;
                        entry = imagesProp.GetArrayElementAtIndex(imagesProp.arraySize - 1);
                    }

                    var nameP    = entry.FindPropertyRelative("m_Name");
                    var sizeP    = entry.FindPropertyRelative("m_Size");
                    var specifyP = entry.FindPropertyRelative("m_SpecifySize");
                    var texP     = entry.FindPropertyRelative("m_Texture");
                    var guidP    = entry.FindPropertyRelative("m_TextureGuid");

                    if (nameP == null || sizeP == null || specifyP == null) return -1;

                    nameP.stringValue      = def.CreatureId;
                    specifyP.boolValue     = true;
                    sizeP.vector2Value     = new Vector2(0.105f, 0.105f);
                    if (texP != null) texP.objectReferenceValue = tex;
                    if (guidP != null) WriteGuidToProperty(guidP, AssetDatabase.AssetPathToGUID(texPath));

                    updated++;
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return updated;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ARFishing] CardLibrary auto-populate failed: {e.Message}");
                return -1;
            }
        }

        static void WriteGuidToProperty(SerializedProperty guidProp, string guid)
        {
            if (string.IsNullOrEmpty(guid) || guid.Length < 32) return;
            var lowP  = guidProp.FindPropertyRelative("m_GuidLow");
            var highP = guidProp.FindPropertyRelative("m_GuidHigh");
            if (lowP == null || highP == null) return;
            if (ulong.TryParse(guid.Substring(0, 16),  System.Globalization.NumberStyles.HexNumber, null, out ulong high) &&
                ulong.TryParse(guid.Substring(16, 16), System.Globalization.NumberStyles.HexNumber, null, out ulong low))
            {
                lowP.longValue  = unchecked((long)low);
                highP.longValue = unchecked((long)high);
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────

        static CreatureDatabase LoadCreatureDatabase()
        {
            var guids = AssetDatabase.FindAssets("t:CreatureDatabase");
            if (guids.Length == 0)
            {
                Debug.LogError("[ARFishing] CreatureDatabase not found. Run 'Create MVP Content' first.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<CreatureDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static void ConfigureImporter(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Default) { imp.textureType = TextureImporterType.Default; changed = true; }
            if (!imp.isReadable)   { imp.isReadable    = true;  changed = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; changed = true; }
            if (changed) imp.SaveAndReimport();
        }

        static int StableHash(string s)
        {
            int h = 17;
            foreach (char c in s) h = unchecked(h * 31 + c);
            return h;
        }

        static void EnsureDir(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            var parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var leaf   = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
