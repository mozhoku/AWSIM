using System;
using UnityEngine;
using UnityEngine.UI;

namespace AWSIM.Scripts.Loader.SimulationLauncher
{
    // It initializes the prefab but gui doesn't have references
    // Should follow the following steps:
    // 1. preLoadBundles
    // 2. load main scene (GUI,clock etc.)
    // 3. initiate prefabs
    // 3.1. release bundles from memory
    // 4. update GUI & prefs
    // 5. when returning to launchpad, release unused assets from memory

    // change the script name
    // Integrate with the existing loader
    internal class BundleLoader : MonoBehaviour
    {
        // input fields
        [SerializeField] private InputField _userVehiclePathField;
        [SerializeField] private InputField _userEnvironmentPathField;

        // dropdowns
        [SerializeField] private Dropdown _vehiclesDropdown;
        [SerializeField] private Dropdown _environmentsDropdown;

        // buttons
        [SerializeField] private Button _startButton;

        // paths
        private string _vehicleBundlePath;
        private string _environmentBundlePath;

        // asset prefabs
        [NonSerialized] public GameObject VehiclePrefab;
        [NonSerialized] public GameObject EnvironmentPrefab;

        private void Start()
        {
            // Add listeners to input fields to update paths
            _userVehiclePathField.onValueChanged.AddListener(delegate { GetBundlePaths(); });
            _userEnvironmentPathField.onValueChanged.AddListener(delegate { GetBundlePaths(); });

            // Add listener to start button
            _startButton.onClick.AddListener(LaunchScenes);
        }

        public void LaunchScenes()
        {
            VehiclePrefab = LoadPrefab(_vehicleBundlePath);
            EnvironmentPrefab = LoadPrefab(_environmentBundlePath);
            AssetBundle.UnloadAllAssetBundles(true);
            UpdateGUI();
        }

        private void GetBundlePaths()
        {
            _vehicleBundlePath = _userVehiclePathField.text;
            _environmentBundlePath = _userEnvironmentPathField.text;
        }

        private static GameObject LoadPrefab(string path)
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

        // Step 4: Update the GUI (example method)
        private void UpdateGUI()
        {
            // Update dropdowns or any other GUI elements based on loaded prefabs
            if (VehiclePrefab != null)
            {
                _vehiclesDropdown.options.Add(new Dropdown.OptionData(VehiclePrefab.name));
            }

            if (EnvironmentPrefab != null)
            {
                _environmentsDropdown.options.Add(new Dropdown.OptionData(EnvironmentPrefab.name));
            }
        }
    }
}
