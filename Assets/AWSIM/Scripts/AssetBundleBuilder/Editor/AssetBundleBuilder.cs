using System.IO;
using AWSIM.Scripts.Loader.SimulationCore;
using UnityEditor;
using UnityEngine;
using static AWSIM.Scripts.AssetBundleBuilder.HashGenerator;

//TODO: Add a button for opening bundle directory (mozzz)
namespace AWSIM.Scripts.AssetBundleBuilder.Editor
{
    /// <summary>
    /// Custom editor window for building AssetBundles in Unity.
    /// </summary>
    public class AssetBundleBuilder : EditorWindow
    {
        private BundleBuildInfo _vehicleBundleBuildInfo;
        private BundleBuildInfo _environmentBundleBuildInfo;

        private bool _doBuildVehicle;
        private bool _doBuildEnvironment;

        private const BuildAssetBundleOptions BuildOption =
            BuildAssetBundleOptions.None | BuildAssetBundleOptions.ForceRebuildAssetBundle;

        private readonly BuildTarget[] _buildTargetValues = AWSIMBuildTargets.TargetValues;

        private string[] _buildTargetNames;

        private int _selectedBuildTargetIndex;

        // Default locations to build the bundles
        // private static string BundleOutputPath => GetPlatformSpecificPath("AssetBundles");
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
            _buildTargetNames = System.Enum.GetNames(typeof(AWSIMBuildTargets.SupportedBuildTargets));

            // Initialize bundles
            _vehicleBundleBuildInfo = InitializeBundleInfo(VehicleOutputPath);
            _environmentBundleBuildInfo = InitializeBundleInfo(EnvironmentOutputPath);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Vehicle field
            _doBuildVehicle = EditorGUILayout.Toggle("Build Vehicle Bundle", _doBuildVehicle);
            if (_doBuildVehicle)
            {
                DrawBundleSettings(_vehicleBundleBuildInfo, "Vehicle");
            }

            GUILayout.Space(5);

            // Environment field
            _doBuildEnvironment = EditorGUILayout.Toggle("Build Environment Bundle", _doBuildEnvironment);
            if (_doBuildEnvironment)
            {
                DrawBundleSettings(_environmentBundleBuildInfo, "Environment");
            }

            GUILayout.Space(5);

            // Build Target
            _selectedBuildTargetIndex =
                EditorGUILayout.Popup("Build Target", _selectedBuildTargetIndex, _buildTargetNames);

            GUILayout.Space(10);

            if (GUILayout.Button("Build")) Build();
        }

        private static void DrawBundleSettings(BundleBuildInfo bundleBuildInfo, string label)
        {
            GUILayout.Space(5);
            GUILayout.Label($"{label} Bundle Settings", EditorStyles.boldLabel);

            bundleBuildInfo.Prefab =
                (GameObject)EditorGUILayout.ObjectField($"{label} Prefab", bundleBuildInfo.Prefab, typeof(GameObject),
                    false);
            bundleBuildInfo.BundleHashSeed = EditorGUILayout.TextField("Bundle Hash Seed", bundleBuildInfo.BundleHashSeed);
            bundleBuildInfo.BundleCreator = EditorGUILayout.TextField("Bundle Creator", bundleBuildInfo.BundleCreator);
            bundleBuildInfo.BundleVersion = EditorGUILayout.TextField("Bundle Version", bundleBuildInfo.BundleVersion);
            // TODO: Make description field TextArea (mozzz)
            bundleBuildInfo.BundleDescription =
                EditorGUILayout.TextField("Bundle Description", bundleBuildInfo.BundleDescription, GUILayout.Height(50));

            if (bundleBuildInfo.Prefab)
            {
                bundleBuildInfo.AssetPath = AssetDatabase.GetAssetPath(bundleBuildInfo.Prefab);
                bundleBuildInfo.AssetBundleName = bundleBuildInfo.Prefab.name;
            }

            bundleBuildInfo.OutputPath = EditorGUILayout.TextField("Output Path", bundleBuildInfo.OutputPath);
            GUILayout.Space(10);
        }

        private void Build()
        {
            var selectedBuildTarget = _buildTargetValues[_selectedBuildTargetIndex];
            var isBuildVehicleBundleSuccessful = false;
            var isBuildEnvironmentBundleSuccessful = false;

            if (_doBuildVehicle && _vehicleBundleBuildInfo.Prefab != null)
            {
                BuildAssetBundle(_vehicleBundleBuildInfo, BuildOption, selectedBuildTarget);
                isBuildVehicleBundleSuccessful = true;
            }

            if (_doBuildEnvironment && _environmentBundleBuildInfo.Prefab != null)
            {
                BuildAssetBundle(_environmentBundleBuildInfo, BuildOption, selectedBuildTarget);
                isBuildEnvironmentBundleSuccessful = true;
            }

            if (isBuildVehicleBundleSuccessful)
            {
                OpenOutputDirectory(VehicleOutputPath);
            }

            if (isBuildEnvironmentBundleSuccessful)
            {
                OpenOutputDirectory(EnvironmentOutputPath);
            }
        }

        private static void BuildAssetBundle(BundleBuildInfo bundleBuildInfo, BuildAssetBundleOptions options, BuildTarget target)
        {
            if (string.IsNullOrEmpty(bundleBuildInfo.OutputPath))
            {
                Debug.LogError("Output path is not set!");
                return;
            }

            if (bundleBuildInfo.Prefab == null)
            {
                Debug.LogError("Target prefab is not set!");
                return;
            }

            EnsureDirectoryExists(bundleBuildInfo.OutputPath);

            // Create a PrefabInfo asset with the prefab name
            // var combinedPrefabName = bundleInfo.Prefab.name.Replace(" ", "_");
            var bundleInfo = CreateInstance<BundleInfo>();

            // Debug.Log("bundleBuildInfo prefab name: " + bundleBuildInfo.Prefab.name);
            // Debug.Log("combined prefab name: " + bundleBuildInfo.Prefab.name);

            bundleInfo.Hash = GenerateMD5Hash(bundleBuildInfo.BundleHashSeed);
            bundleInfo.Name = bundleBuildInfo.Prefab.name;
            bundleInfo.Creator = bundleBuildInfo.BundleCreator;
            bundleInfo.CreationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            bundleInfo.BundleVersion = bundleBuildInfo.BundleVersion;
            bundleInfo.Description = bundleBuildInfo.BundleDescription;

            // bundleInfo.AssetBundleName = combinedPrefabName;

            // Save the PrefabInfo asset temporarily in the 'Assets' folder
            var bundleInfoPath = "Assets/BundleInfo.asset";
            AssetDatabase.CreateAsset(bundleInfo, bundleInfoPath);

            // Set the asset bundle name for the PrefabInfo
            var prefabImporter = AssetImporter.GetAtPath(bundleInfoPath);
            prefabImporter.assetBundleName = bundleBuildInfo.AssetBundleName;

            // Set the prefab itself to the asset bundle
            var importer = AssetImporter.GetAtPath(bundleBuildInfo.AssetPath);
            if (importer == null)
            {
                Debug.LogError($"Asset importer could not be found for {bundleBuildInfo.AssetPath}!");
                return;
            }

            importer.assetBundleName = bundleBuildInfo.AssetBundleName;

            // Build the asset bundle
            var manifest = BuildPipeline.BuildAssetBundles(bundleBuildInfo.OutputPath, options, target);
            if (manifest != null)
            {
                Debug.Log(
                    $"Asset Bundle '{bundleBuildInfo.AssetBundleName}' built successfully at '{bundleBuildInfo.OutputPath}'.");
            }
            else
            {
                Debug.LogError(
                    $"Failed to build Asset Bundle '{bundleBuildInfo.AssetBundleName}' at '{bundleBuildInfo.OutputPath}'.");
            }

            // Cleanup temporary assets and assignments
            AssetDatabase.DeleteAsset(bundleInfoPath);
            importer.assetBundleName = null;
        }

        private static BundleBuildInfo InitializeBundleInfo(string outputPath)
        {
            return new BundleBuildInfo
            {
                BundleHashSeed = null,
                OutputPath = outputPath,
                Prefab = null,
                AssetBundleName = null,
                AssetPath = null,
                BundleCreator = null,
                BundleVersion = null,
                BundleDescription = null
            };
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void OpenOutputDirectory(string path)
        {
            if (Directory.Exists(path))
            {
#if UNITY_EDITOR_WIN
                System.Diagnostics.Process.Start("explorer.exe", path);
#elif UNITY_EDITOR_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
#endif
            }
            else
            {
                Debug.LogWarning($"Directory does not exist: {path}");
            }
        }
    }
}
