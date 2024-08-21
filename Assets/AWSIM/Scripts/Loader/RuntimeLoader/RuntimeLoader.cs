using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AWSIM.Scripts.Loader.RuntimeLoader
{
    public class RuntimeLoader : MonoBehaviour
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

        // asset bundles
        private AssetBundle _egoBundle;
        private AssetBundle _environmentBundle;

        // game objects
        private GameObject _egoVehicle;
        private GameObject _environment;

        // canvas
        private Canvas _canvas;

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            _userVehiclePathField.onValueChanged.AddListener(delegate { GetBundlePaths(); });
            _userEnvironmentPathField.onValueChanged.AddListener(delegate { GetBundlePaths(); });
        }

        private void GetBundlePaths()
        {
            _vehicleBundlePath = _userVehiclePathField.text;
            _environmentBundlePath = _userEnvironmentPathField.text;
        }

        private void LoadBundles()
        {
            _egoBundle = AssetBundle.LoadFromFile(_vehicleBundlePath);
            _environmentBundle =
                AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, _environmentBundlePath));
        }

        private void GetAssetFromBundle()
        {
            _egoVehicle = _egoBundle.LoadAsset<GameObject>("nameofvehicle");
            _environment = _environmentBundle.LoadAsset<GameObject>("nameofenv");
        }

        private void LoadBundle(string path)
        {
            var bundle = AssetBundle.LoadFromFile(path);

            if (bundle != null)
            {
                // Load the prefab from the AssetBundle
                GameObject prefab = bundle.LoadAsset<GameObject>("Lexus RX450h 2015 Sample Sensor");
                Instantiate(prefab);
                // bundle.Unload(false);
            }
            else
            {
                Debug.LogWarning("Failed to load AssetBundle!");
            }
        }

        public void LaunchScenes()
        {
            SceneManager.LoadScene("AutowareSimulation", LoadSceneMode.Additive);
            LoadBundle(_vehicleBundlePath);
            _canvas.GetComponent<Canvas>().gameObject.SetActive(false);
        }
    }
}
