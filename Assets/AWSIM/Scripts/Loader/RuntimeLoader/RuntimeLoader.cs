using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AWSIM.Scripts.Loader.RuntimeLoader
{
    // it initializes the prefab but gui doesnt have references
    // should follow the following steps:
    // 1. preLoadBundles
    // 2. load main scene (GUI,clock etc.)
    // 3. initiate prefabs
    // 3.1. release bundles from memory
    // 4. update GUI
    // 5. when returning to launchpad, release unused assetrs from memory
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

        // prefabs
        private GameObject _egoPrefab;
        private GameObject _environmentPrefab;

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

        private void LoadBundle(string path)
        {
            _egoBundle = AssetBundle.LoadFromFile(path);
            if (_egoBundle is not null)
            {
                // Load the prefab from the AssetBundle
                _egoPrefab = _egoBundle.LoadAsset<GameObject>("Lexus RX450h 2015 Sample Sensor");
                _egoVehicle = Instantiate(_egoPrefab);
                // _egoBundle.Unload(false);
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
