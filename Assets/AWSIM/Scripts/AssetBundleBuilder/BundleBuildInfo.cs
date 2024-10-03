using UnityEngine;

namespace AWSIM.Scripts.AssetBundleBuilder
{
    /// <summary>
    /// The class containing bundle information for building asset bundles
    /// </summary>
    public class BundleBuildInfo
    {
        public string BundleHashSeed;
        public string OutputPath;
        public GameObject Prefab;
        public string AssetBundleName;
        public string AssetPath;
        public string BundleCreator;
        public string BundleVersion;
        public string BundleDescription;
    }
}
