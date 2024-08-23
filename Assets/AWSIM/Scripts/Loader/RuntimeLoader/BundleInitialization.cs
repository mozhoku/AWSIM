using UnityEngine;

namespace AWSIM.Scripts.Loader.RuntimeLoader
{
    public class BundleInitialization : MonoBehaviour
    {
        // private IEnumerator LoadBundle(string path)
        // {
        //     var bundle = AssetBundle.LoadFromFile(path);
        //
        //     if (bundle != null)
        //     {
        //         // Load the prefab from the AssetBundle
        //         GameObject prefab = bundle.LoadAsset<GameObject>("Pepega");
        //         Instantiate(prefab);
        //         bundle.Unload(false);
        //     }
        //     else
        //     {
        //         Debug.LogError("Failed to load AssetBundle!");
        //     }
        //
        //     yield return null;
        // }
    }
}
