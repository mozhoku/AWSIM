using UnityEditor;

namespace AWSIM.Scripts.Loader.RuntimeLoader
{
    public class SimpleBuilder
    {
        [MenuItem("AWSIMLabs/Build AssetBundles")]
        static void BuildAllAssetBundles()
        {
            BuildPipeline.BuildAssetBundles(
                "AssetBundles",
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneLinux64);
        }
    }
}
