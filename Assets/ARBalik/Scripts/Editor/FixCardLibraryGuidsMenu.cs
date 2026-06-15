using ARFishing.Creatures;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARFishing.Editor
{
    /// <summary>
    /// Rebuilds CardLibrary entries via AR Foundation's proper Add() API so each entry
    /// gets a unique GUID. The previous auto-populate path wrote entries through
    /// SerializedObject without calling Add(), leaving every m_SerializedGuid = 0/0.
    /// arcoreimg then collapsed all 20 same-keyed entries into one at build time and
    /// ARCore matched every physical card to the last surviving entry (dolphin).
    /// </summary>
    public static class FixCardLibraryGuidsMenu
    {
        const string CardLibraryPath = "Assets/ARBalık/Content/CardLibrary.asset";
        const string MarkersDir = "Assets/ARBalık/Content/CardImages";

        [MenuItem("ARFishing/Fix CardLibrary (assign unique GUIDs)")]
        public static void Fix()
        {
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(CardLibraryPath);
            if (library == null)
            {
                Debug.LogError($"[ARFishing] CardLibrary not found at {CardLibraryPath}");
                return;
            }

            var dbGuids = AssetDatabase.FindAssets("t:CreatureDatabase");
            if (dbGuids.Length == 0)
            {
                Debug.LogError("[ARFishing] CreatureDatabase asset not found. Run 'Create MVP Content' first.");
                return;
            }
            var db = AssetDatabase.LoadAssetAtPath<CreatureDatabase>(AssetDatabase.GUIDToAssetPath(dbGuids[0]));

            while (library.count > 0)
            {
                library.RemoveAt(0);
            }

            int added = 0;
            foreach (var def in db.All)
            {
                if (def == null || string.IsNullOrEmpty(def.CreatureId)) continue;
                var texPath = $"{MarkersDir}/marker_{def.CreatureId}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null)
                {
                    Debug.LogWarning($"[ARFishing] missing texture for {def.CreatureId} at {texPath}");
                    continue;
                }

                library.Add();
                int idx = library.count - 1;
                library.SetName(idx, def.CreatureId);
                library.SetTexture(idx, tex, keepTexture: true);
                library.SetSpecifySize(idx, true);
                library.SetSize(idx, new Vector2(0.105f, 0.105f));
                added++;
            }

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ARFishing] CardLibrary rebuilt with {added} entries, each with a unique GUID. Now do: right-click CardLibrary -> Reimport, then Build.");

            for (int i = 0; i < library.count; i++)
            {
                var img = library[i];
                Debug.Log($"[ARFishing]   [{i}] name='{img.name}' guid={img.guid:N} textureGuid={img.textureGuid:N}");
            }
        }
    }
}
