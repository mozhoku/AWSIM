using UnityEngine;
using UnityEngine.Serialization;

namespace AWSIM.Scripts.Loader.SimulationCore
{
    /// <summary>
    /// Used for tracking metadata when building and loading assetBundles.
    /// </summary>
    [CreateAssetMenu(fileName = "BundleInfo", menuName = "AWSIM/Bundle Info", order = 1)]
    public class BundleInfo : ScriptableObject
    {
        public string Hash;
        public BundleType Type;
        public string Name;
        public string Creator;
        public string CreationDate;
        public string BundleVersion;
        public string Description;
    }
}
