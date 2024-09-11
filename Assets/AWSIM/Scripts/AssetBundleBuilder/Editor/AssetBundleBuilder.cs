using System.IO;
using AWSIM.Scripts.Loader.SimulationLauncher;
using UnityEditor;
using UnityEngine;

namespace AWSIM.Scripts.AssetBundleBuilder.Editor
{
    /// <summary>
    /// Custom editor window for building AssetBundles in Unity.
    /// </summary>
    public class AssetBundleBuilder : EditorWindow
    {
        private BundleInfo _vehicleBundleInfo;
        private BundleInfo _environmentBundleInfo;

        private bool _doBuildVehicle;
        private bool _doBuildEnvironment;

        private readonly BuildAssetBundleOptions[] _buildOptionsValues =
            (BuildAssetBundleOptions[])System.Enum.GetValues(typeof(BuildAssetBundleOptions));

        private readonly BuildTarget[] _buildTargetsValues = (BuildTarget[])System.Enum.GetValues(typeof(BuildTarget));

        private string[] _buildOptionsNames;
        private string[] _buildTargetsNames;

        private int _selectedBuildOptionIndex;
        private int _selectedBuildTargetIndex;

        // Default locations to build the bundles
        private static string VehicleOutputPath => GetPlatformSpecificPath("AssetBundles/Vehicles");
        private static string EnvironmentOutputPath => GetPlatformSpecificPath("AssetBundles/Environments");

        private static string GetPlatformSpecificPath(string relativePath)
        {
            var projectFolderPath = Path.GetDirectoryName(Application.dataPath);
#if UNITY_EDITOR_WIN
            return Path.Combine(projectFolderPath, relativePath.Replace("/", "\\"));
#else
    return Path.Combine(projectFolderPath, relativePath);
#endif
        }

        [MenuItem("AWSIMLabs/Bundle Build Menu")]
        private static void ShowWindow()
        {
            GetWindow<AssetBundleBuilder>("Bundle Build Menu");
        }

        private void OnEnable()
        {
            _buildOptionsNames = System.Enum.GetNames(typeof(BuildAssetBundleOptions));
            _buildTargetsNames = System.Enum.GetNames(typeof(BuildTarget));

            // Initialize bundles
            _vehicleBundleInfo = InitializeBundleInfo(VehicleOutputPath);
            _environmentBundleInfo = InitializeBundleInfo(EnvironmentOutputPath);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Vehicle field
            _doBuildVehicle = EditorGUILayout.Toggle("Build Vehicle Bundle", _doBuildVehicle);
            if (_doBuildVehicle) DrawBundleSettings(_vehicleBundleInfo, "Vehicle");

            GUILayout.Space(5);

            // Environment field
            _doBuildEnvironment = EditorGUILayout.Toggle("Build Environment Bundle", _doBuildEnvironment);
            if (_doBuildEnvironment) DrawBundleSettings(_environmentBundleInfo, "Environment");

            GUILayout.Space(5);

            // Shared options for bundles
            _selectedBuildOptionIndex =
                EditorGUILayout.Popup("Build Options", _selectedBuildOptionIndex, _buildOptionsNames);
            _selectedBuildTargetIndex =
                EditorGUILayout.Popup("Build Target", _selectedBuildTargetIndex, _buildTargetsNames);

            GUILayout.Space(10);

            if (GUILayout.Button("Build")) Build();
        }

        private static void DrawBundleSettings(BundleInfo bundleInfo, string label)
        {
            GUILayout.Space(5);
            GUILayout.Label($"{label} Bundle Settings", EditorStyles.boldLabel);
            bundleInfo.Prefab =
                (GameObject)EditorGUILayout.ObjectField($"{label} Prefab", bundleInfo.Prefab, typeof(GameObject),
                    false);

            if (bundleInfo.Prefab)
            {
                bundleInfo.AssetPath = AssetDatabase.GetAssetPath(bundleInfo.Prefab);
                bundleInfo.AssetBundleName = bundleInfo.Prefab.name;
            }

            bundleInfo.OutputPath = EditorGUILayout.TextField("Output Path", bundleInfo.OutputPath);
            GUILayout.Space(10);
        }

        private void Build()
        {
            var selectedBuildOption = _buildOptionsValues[_selectedBuildOptionIndex];
            var selectedBuildTarget = _buildTargetsValues[_selectedBuildTargetIndex];

            if (_doBuildVehicle && _vehicleBundleInfo.Prefab != null)
                BuildAssetBundle(_vehicleBundleInfo, selectedBuildOption, selectedBuildTarget);
            if (_doBuildEnvironment && _environmentBundleInfo.Prefab != null)
                BuildAssetBundle(_environmentBundleInfo, selectedBuildOption, selectedBuildTarget);
        }

        private static void BuildAssetBundle(BundleInfo bundleInfo, BuildAssetBundleOptions options, BuildTarget target)
        {
            if (string.IsNullOrEmpty(bundleInfo.OutputPath))
            {
                Debug.LogError("Output path is not set!");
                return;
            }

            if (bundleInfo.Prefab == null)
            {
                Debug.LogError("Target prefab is not set!");
                return;
            }

            EnsureDirectoryExists(bundleInfo.OutputPath);
            
            // Get the lightmaps for the scene/prefab
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            if (lightmaps != null && lightmaps.Length > 0)
            {
                foreach (var lightmapData in lightmaps)
                {
                    if (lightmapData.lightmapColor != null)
                    {
                        var lightmapPath = AssetDatabase.GetAssetPath(lightmapData.lightmapColor);
                        var lightmapImporter = AssetImporter.GetAtPath(lightmapPath);
                        lightmapImporter.assetBundleName = bundleInfo.AssetBundleName;
                    }

                    if (lightmapData.lightmapDir!=null)
                    {
                        var lightmapPath = AssetDatabase.GetAssetPath(lightmapData.lightmapDir);
                        var lightmapImporter = AssetImporter.GetAtPath(lightmapPath);
                        lightmapImporter.assetBundleName = bundleInfo.AssetBundleName;
                    }
                }
            }

            // Create a PrefabInfo asset with the prefab name
            // var combinedPrefabName = bundleInfo.Prefab.name.Replace(" ", "_");
            var prefabInfo = CreateInstance<PrefabInfo>();
            
            Debug.Log("bundleinfo prefab name: "+ bundleInfo.Prefab.name);
            Debug.Log("combined prefab name: "+ bundleInfo.Prefab.name);
            
            prefabInfo.prefabName = bundleInfo.Prefab.name;
            // bundleInfo.AssetBundleName = combinedPrefabName;

            // Save the PrefabInfo asset temporarily in the 'Assets' folder
            var prefabInfoPath = "Assets/PrefabInfo.asset";
            AssetDatabase.CreateAsset(prefabInfo, prefabInfoPath);

            // Set the asset bundle name for the PrefabInfo
            var prefabImporter = AssetImporter.GetAtPath(prefabInfoPath);
            prefabImporter.assetBundleName = bundleInfo.AssetBundleName;

            // Set the prefab itself to the asset bundle
            var importer = AssetImporter.GetAtPath(bundleInfo.AssetPath);
            if (importer == null)
            {
                Debug.LogError($"Asset importer could not be found for {bundleInfo.AssetPath}!");
                return;
            }

            importer.assetBundleName = bundleInfo.AssetBundleName;

            // Build the asset bundle
            var manifest = BuildPipeline.BuildAssetBundles(bundleInfo.OutputPath, options, target);
            if (manifest != null)
            {
                Debug.Log(
                    $"Asset Bundle '{bundleInfo.AssetBundleName}' built successfully at '{bundleInfo.OutputPath}'.");
            }
            else
            {
                Debug.LogError(
                    $"Failed to build Asset Bundle '{bundleInfo.AssetBundleName}' at '{bundleInfo.OutputPath}'.");
            }

            // Cleanup temporary assets and assignments
            AssetDatabase.DeleteAsset(prefabInfoPath);
            importer.assetBundleName = null;
        }

        private static BundleInfo InitializeBundleInfo(string outputPath)
        {
            return new BundleInfo
            {
                OutputPath = outputPath,
                Prefab = null,
                AssetBundleName = null,
                AssetPath = null
            };
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
