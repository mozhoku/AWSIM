using UnityEngine;

namespace AWSIM.Scripts.Loader.RuntimeLoader
{
    /// <summary>
    /// Used for tracking prefab name when building assetBundles.
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabInfo", menuName = "AWSIM/Prefab Info", order = 1)]
    public class PrefabInfo : ScriptableObject
    {
        public string prefabName;
    }
}
