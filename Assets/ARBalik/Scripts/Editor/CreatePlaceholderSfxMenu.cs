using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreatePlaceholderSfxMenu
    {
        const string SfxDir = "Assets/ARBalık/Content/Audio/Sfx";

        [MenuItem("ARFishing/Create Placeholder SFX (skip existing)")]
        public static void CreateAll()
        {
            EnsureDir(SfxDir);

            var correctPath = $"{SfxDir}/sfx_correct.wav";
            var incorrectPath = $"{SfxDir}/sfx_incorrect.wav";
            var scanPath = $"{SfxDir}/sfx_scan.wav";

            int written = 0;
            if (!File.Exists(Path.GetFullPath(correctPath)))
            {
                WriteArpeggio(correctPath, new[] { 523, 659, 784 }, perNoteSeconds: 0.12f);
                written++;
            }
            if (!File.Exists(Path.GetFullPath(incorrectPath)))
            {
                WriteArpeggio(incorrectPath, new[] { 440, 349 }, perNoteSeconds: 0.15f);
                written++;
            }
            if (!File.Exists(Path.GetFullPath(scanPath)))
            {
                WriteArpeggio(scanPath, new[] { 1175 }, perNoteSeconds: 0.08f);
                written++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[ARFishing] Placeholder SFX: wrote {written} new clips in {SfxDir} (skipped existing).");
        }

        static void WriteArpeggio(string assetPath, int[] frequencies, float perNoteSeconds)
        {
            const int sampleRate = 22050;
            const float amplitude = 0.18f;
            int samplesPerNote = Mathf.RoundToInt(sampleRate * perNoteSeconds);
            int totalSamples = samplesPerNote * frequencies.Length;

            var samples = new short[totalSamples];
            for (int n = 0; n < frequencies.Length; n++)
            {
                int freq = frequencies[n];
                for (int i = 0; i < samplesPerNote; i++)
                {
                    int idx = n * samplesPerNote + i;
                    float t = i / (float)sampleRate;
                    float envelope = Mathf.Clamp01(Mathf.Min(t * 30f, (perNoteSeconds - t) * 30f));
                    samples[idx] = (short)(Mathf.Sin(2f * Mathf.PI * freq * t) * amplitude * envelope * short.MaxValue);
                }
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
