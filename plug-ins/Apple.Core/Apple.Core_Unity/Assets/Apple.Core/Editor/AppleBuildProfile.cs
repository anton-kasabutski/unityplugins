using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Apple.Core
{
    [CreateAssetMenu(menuName = "Apple/Build/Apple Build Profile")]
    public class AppleBuildProfile : ScriptableObject
    {
        public const string DefaultAsset = "DefaultAppleBuildProfile.asset";

        public Dictionary<string, AppleBuildStep> buildSteps = new Dictionary<string, AppleBuildStep>();

        public bool AutomateInfoPlist = true;
        public UnityEngine.Object DefaultInfoPlist;

        public string MinimumOSVersion_iOS = string.Empty;
        public string MinimumOSVersion_tvOS = string.Empty;
        public string MinimumOSVersion_macOS = string.Empty;

        public bool AppUsesNonExemptEncryption = false;

        public bool AutomateEntitlements = true;
        public UnityEngine.Object DefaultEntitlements;

        /// <summary>
        /// Accesses the default build profile, creating it if one isn't available.
        /// </summary>
        public static AppleBuildProfile DefaultProfile()
        {
            var folders = new List<string>() { "Assets", "Packages", "Apple.Core", "Editor" };
            var fullPath = Path.Combine(folders.ToArray());
            for (int i = 1; i < folders.Count; i++) {
                var subPathFolders = folders.GetRange(0, i);
                var subPath = Path.Combine(folders.GetRange(0, i).ToArray());
                if (!Directory.Exists(subPath)) {
                    Debug.Log($"Failed to locate path {subPath}. Creating");
                    AssetDatabase.CreateFolder(Path.Combine(subPathFolders.GetRange(0, subPathFolders.Count - 1).ToArray()), subPathFolders[subPathFolders.Count - 1]);
                }
            }

            AppleBuildProfile defaultProfile = null;
            var profs = Array.Empty<Object>();
            var defAssetPath = Path.Combine(fullPath, DefaultAsset);
            if (File.Exists(defAssetPath))
            {
                profs = AssetDatabase.LoadAllAssetsAtPath(defAssetPath);
                defaultProfile = (AppleBuildProfile)AssetDatabase.LoadMainAssetAtPath(defAssetPath);
            }

            if (defaultProfile is null)
            {
                Debug.Log("Failed to find previous default build profile. Creating a new one.");
                defaultProfile = CreateInstance<AppleBuildProfile>();

                AssetDatabase.CreateAsset(defaultProfile, defAssetPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                defaultProfile.ResolveBuildSteps();
                AssetDatabase.SaveAssets();

                AssetDatabase.SetMainObject(defaultProfile, defAssetPath);
                AssetDatabase.ImportAsset(defAssetPath);
            }
            else
            {
                foreach (var p in profs)
                {
                    if (p != defaultProfile && p is AppleBuildStep)
                    {
                        defaultProfile.buildSteps[p.name] = (AppleBuildStep)p;
                    }
                }
            }

            return defaultProfile;
        }

        /// <summary>
        /// Updates the currently known list of AppleBuildSteps
        /// </summary>
        public void ResolveBuildSteps()
        {
            var buildStepTypes = AppleBuildStep.ProjectAppleBuildStepTypes();

            // Add any newly added build steps
            foreach (var buildStepType in buildStepTypes)
            {
                if (!buildSteps.ContainsKey(buildStepType.Name))
                {
                    var buildStep = (AppleBuildStep)CreateInstance(buildStepType);
                    buildStep.name = buildStepType.Name;

                    buildSteps[buildStepType.Name] = buildStep;

                    AssetDatabase.AddObjectToAsset(buildStep, this);
                }
            }

            // Remove build steps that are no longer found
            var buildStepTypeNames = buildStepTypes.Select((t) => t.Name).ToArray();
            foreach (var entry in buildSteps)
            {
                if (!buildStepTypeNames.Contains(entry.Key))
                {
                    buildSteps.Remove(entry.Key);
                    DestroyImmediate(entry.Value, true);
                }
            }
        }
    }
}
