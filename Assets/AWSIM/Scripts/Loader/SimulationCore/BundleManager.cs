using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace AWSIM.Scripts.Loader.SimulationCore
{
    public static class BundleManager
    {
        // Dictionary to store bundle name and its internal path
        private static Dictionary<string, string> bundlePathDictionary = new();

        // Define locations for different bundle types
        private static readonly string VehicleFolder = Path.Combine(Application.persistentDataPath, "Bundles/Vehicles");
        private static readonly string EnvironmentFolder = Path.Combine(Application.persistentDataPath, "Bundles/Environments");
        private static readonly string UnknownFolder = Path.Combine(Application.persistentDataPath, "Bundles/Sensors");

        /// <summary>
        /// Imports the bundle to the target directory based on the type specified in PrefabInfo.
        /// </summary>
        public static bool ImportBundle(string bundlePath)
        {
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogWarning($"Failed to load AssetBundle from path: {bundlePath}");
                return false;
            }

            // Load the PrefabInfo to determine bundle type
            var prefabInfo = bundle.LoadAsset<BundleInfo>("PrefabInfo");
            if (prefabInfo == null)
            {
                Debug.LogWarning("PrefabInfo not found in AssetBundle!");
                bundle.Unload(true);
                return false;
            }

            // Determine the target directory based on the bundle type
            string targetDirectory = GetTargetDirectory(prefabInfo.Type);

            if (string.IsNullOrEmpty(targetDirectory))
            {
                Debug.LogWarning("No valid target directory found for this bundle type!");
                bundle.Unload(true);
                return false;
            }

            // Move the bundle to the correct directory
            string destinationPath = CopyBundleToInternalFolder(bundlePath, targetDirectory);

            if (!string.IsNullOrEmpty(destinationPath))
            {
                // Add the bundle name and internal path to the dictionary
                bundlePathDictionary[prefabInfo.Name] = destinationPath;
            }

            bundle.Unload(true);
            // Caching.Clear();
            return true;
        }

        /// <summary>
        /// Loads a prefab from the previously imported bundle.
        /// </summary>
        public static GameObject LoadPrefab(string prefabName)
        {
            if (!bundlePathDictionary.TryGetValue(prefabName, out string bundlePath))
            {
                Debug.LogWarning($"Bundle for prefab '{prefabName}' not found in the dictionary.");
                return null;
            }

            // Load the bundle and retrieve the prefab
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogWarning($"Failed to load AssetBundle from path: {bundlePath}");
                return null;
            }

            // Load the prefab
            var prefab = bundle.LoadAsset<GameObject>(prefabName);
            bundle.Unload(false);
            return prefab;
        }

        /// <summary>
        /// Returns the target directory based on the bundle type.
        /// </summary>
        private static string GetTargetDirectory(BundleType bundleType)
        {
            return bundleType switch
            {
                BundleType.Vehicle => VehicleFolder,
                BundleType.Environment => EnvironmentFolder,
                BundleType.Unknown => UnknownFolder,
                _ => null
            };
        }

        /// <summary>
        /// Moves the bundle file to the target directory and returns the destination path.
        /// </summary>
        private static string CopyBundleToInternalFolder(string bundlePath, string targetDirectory)
        {
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            string bundleFileName = Path.GetFileName(bundlePath);
            string destinationPath = Path.Combine(targetDirectory, bundleFileName);

            if (File.Exists(destinationPath))
            {
                Debug.LogWarning($"Bundle already exists at {destinationPath}, it will be overwritten.");
            }

            File.Copy(bundlePath, destinationPath, true); // Copy and overwrite if necessary
            Debug.Log($"Bundle successfully moved to {destinationPath}");

            return destinationPath;
        }

        /// <summary>
        /// Gets the path of the imported bundle by its name from the dictionary.
        /// </summary>
        public static string GetBundlePath(string prefabName)
        {
            return bundlePathDictionary.GetValueOrDefault(prefabName);
        }
    }
}
