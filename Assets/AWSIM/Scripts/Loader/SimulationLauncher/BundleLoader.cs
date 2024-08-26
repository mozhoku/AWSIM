using UnityEngine;

namespace AWSIM.Scripts.Loader.SimulationLauncher
{
    public static class BundleLoader
    {
        /// <summary>
        /// Loads the prefabs from the given asset bundle path.
        /// </summary>
        public static GameObject LoadPrefab(string path)
        {
            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogWarning($"Failed to load AssetBundle from path: {path}");
                return null;
            }

            // Load the PrefabInfo first to get the prefab name
            var prefabInfo = bundle.LoadAsset<PrefabInfo>("PrefabInfo");
            if (prefabInfo == null)
            {
                Debug.LogWarning("PrefabInfo not found in AssetBundle!");
                bundle.Unload(true);
                return null;
            }

            // Now load the prefab using the name from PrefabInfo
            var prefab = bundle.LoadAsset<GameObject>(prefabInfo.prefabName);
            bundle.Unload(true);

            return prefab;
        }
    }
}
