using UnityEditor;

namespace AWSIM.Scripts.AssetBundleBuilder.Editor
{
    public static class AWSIMBuildTargets
    {
        /// <summary>
        /// Build targets used for building the bundles for AWSIM Labs.
        /// </summary>
        public enum SupportedBuildTargets
        {
            StandaloneLinux64 = 19,
            StandaloneWindows64 = 24
        }

        public static readonly BuildTarget[] TargetValues =
        {
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneWindows64
        };
    }
}
