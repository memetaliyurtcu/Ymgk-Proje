using System.IO;
using System.Text;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreatePlaceholderAudioMenu
    {
        const string AudioDir = "Assets/ARBalık/Content/Audio";

        [MenuItem("ARFishing/Create Placeholder Audio (skip existing)")]
        public static void CreateAll()
        {
            EnsureDir(AudioDir);

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

                if (def.NarrationClip != null)
                {
                    skipped++;
                    continue;
                }

                var clipPath = $"{AudioDir}/narration_{def.CreatureId}.wav";
                if (!File.Exists(Path.GetFullPath(clipPath)))
                {
                    int frequency = FrequencyFor(def.CreatureId);
                    CreateTone(clipPath, frequency, 1.5f);
                }
            }

            AssetDatabase.Refresh();

            // Second pass: now that AssetDatabase knows about the WAVs, assign them.
            foreach (var guid in guids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(defPath);
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                if (def.NarrationClip != null) continue;

                var clipPath = $"{AudioDir}/narration_{def.CreatureId}.wav";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null) continue;

                var so = new SerializedObject(def);
                so.FindProperty("m_NarrationClip").objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ARFishing] Placeholder audio: assigned {assigned}, skipped {skipped} (already had NarrationClip).");
        }

        static int FrequencyFor(string creatureId)
        {
            int hash = 17;
            foreach (var c in creatureId)
            {
                hash = unchecked(hash * 31 + c);
            }
            // Spread across a child-friendly mid range: 320–880 Hz.
            int span = 880 - 320;
            return 320 + ((hash & 0x7FFFFFFF) % span);
        }

        static void CreateTone(string assetPath, int frequency, float durationSeconds)
        {
            const int sampleRate = 22050;
            const float amplitude = 0.15f;
            int sampleCount = Mathf.RoundToInt(sampleRate * durationSeconds);

            var samples = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(Mathf.Min(t * 20f, (durationSeconds - t) * 20f));
                samples[i] = (short)(Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * envelope * short.MaxValue);
            }

            WriteWav(assetPath, samples, sampleRate);
        }

        static void WriteWav(string assetPath, short[] samples, int sampleRate)
        {
            var absolutePath = Path.GetFullPath(assetPath);
            using var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            int byteCount = samples.Length * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + byteCount);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(byteCount);
            foreach (var s in samples) writer.Write(s);
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
