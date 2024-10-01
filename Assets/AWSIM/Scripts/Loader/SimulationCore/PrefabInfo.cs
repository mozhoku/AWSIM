using UnityEngine;

namespace AWSIM.Scripts.Loader.SimulationCore
{
    /// <summary>
    /// Used for tracking metadata when building and loading assetBundles.
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabInfo", menuName = "AWSIM/Prefab Info", order = 1)]
    public class PrefabInfo : ScriptableObject
    {
        public string prefabName;
        public BundleType bundleType;
        public string version;
        public string creator;
        public string description;
    }
}
