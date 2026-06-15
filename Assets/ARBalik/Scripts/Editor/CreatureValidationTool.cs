using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ARFishing.Creatures;
using UnityEditor;
using UnityEngine;

namespace ARFishing.Editor
{
    public static class CreatureValidationTool
    {
        static readonly Regex s_KebabCase = new(@"^[a-z0-9]+(-[a-z0-9]+)*$");

        const float MaxNarrationSeconds = 25f;

        [MenuItem("ARFishing/Validate All Creature Definitions")]
        public static void ValidateAll()
        {
            var guids = AssetDatabase.FindAssets("t:CreatureDefinition");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[ARFishing] No CreatureDefinition assets found.");
                return;
            }

            var problems = new List<string>();
            var seenIds = new Dictionary<string, string>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(path);
                if (def == null) continue;

                if (string.IsNullOrEmpty(def.CreatureId))
                {
                    problems.Add($"{path}: empty CreatureId");
                }
                else if (!s_KebabCase.IsMatch(def.CreatureId))
                {
                    problems.Add($"{path}: CreatureId '{def.CreatureId}' is not kebab-case");
                }
                else if (seenIds.TryGetValue(def.CreatureId, out var otherPath))
                {
                    problems.Add($"{path}: duplicate CreatureId '{def.CreatureId}' (also in {otherPath})");
                }
                else
                {
                    seenIds[def.CreatureId] = path;
                }

                if (string.IsNullOrEmpty(def.DisplayName))
                    problems.Add($"{path}: empty DisplayName");
                if (def.Category == CreatureCategory.Unknown)
                    problems.Add($"{path}: Category is Unknown");
                if (def.Habitat == Habitat.Unknown)
                    problems.Add($"{path}: Habitat is Unknown");
                if (def.Diet == DietType.Unknown)
                    problems.Add($"{path}: Diet is Unknown");
                if (def.EcosystemRole == EcosystemRole.Unknown)
                    problems.Add($"{path}: EcosystemRole is Unknown");
                if (def.ModelPrefab == null)
                    problems.Add($"{path}: ModelPrefab is null");
                if (def.NarrationClip == null)
                    problems.Add($"{path}: NarrationClip is null");
                else if (def.NarrationClip.length > MaxNarrationSeconds)
                    problems.Add($"{path}: NarrationClip is {def.NarrationClip.length:F1}s, exceeds {MaxNarrationSeconds}s cap");
                if (string.IsNullOrEmpty(def.ReferenceImageName))
                    problems.Add($"{path}: empty ReferenceImageName");
            }

            if (problems.Count == 0)
            {
                Debug.Log($"[ARFishing] All {guids.Length} CreatureDefinitions valid.");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine($"[ARFishing] Found {problems.Count} problems in {guids.Length} CreatureDefinitions:");
            foreach (var p in problems) report.AppendLine("  " + p);
            Debug.LogError(report.ToString());
        }
    }
}
