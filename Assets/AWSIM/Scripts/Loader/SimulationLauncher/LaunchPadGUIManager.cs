using System;
using SFB;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static AWSIM.Scripts.Loader.SimulationLauncher.BundleLoader;
using Image = UnityEngine.UI.Image;

namespace AWSIM.Scripts.Loader.SimulationLauncher
{
    /// <summary>
    /// Coordinate systems for handling the spawn point input:
    /// Unity = 0 | LatLong = 1 | Mgrs = 2
    /// </summary>
    internal enum CoordSyss
    {
        Unity = 0,
        LatLong = 1,
        Mgrs = 2
    }

    public class LaunchPadGUIManager : MonoBehaviour
    {
        // load initial gui
        // update gui when an assetbundle is selected
        // remember old loaded prefabs

        // Vehicle Block
        [FormerlySerializedAs("_vehiclePathButton")] [SerializeField] private Button _loadVehicleBundleButton;
        [SerializeField] private Dropdown _vehiclesDropdown;
        [SerializeField] private Image _vehicleVisualArea;

        // Sensor Block
        [SerializeField] private InputField _sensorPathField;
        [SerializeField] private Dropdown _sensorsDropdown;

        // Environment Block
        [FormerlySerializedAs("_environmentPathButton")] [SerializeField] private Button _loadEnvironmentBundleButton;
        [SerializeField] private Dropdown _environmentsDropdown;
        [SerializeField] private Toggle _useCoordsToggle;
        [SerializeField] private Dropdown _spawnPointDropdown;
        [SerializeField] private Dropdown _CoordSysDropdown;
        [SerializeField] private InputField _positionFieldX;
        [SerializeField] private InputField _positionFieldY;
        [SerializeField] private InputField _positionFieldZ;
        [SerializeField] private InputField _rotationFieldX;
        [SerializeField] private InputField _rotationFieldY;
        [SerializeField] private InputField _rotationFieldZ;
        [SerializeField] private InputField _rotationFieldW;

        // Sim Params Block

        // Graphics Block

        // Config Block
        [SerializeField] private Button _loadSimConfButton;
        [SerializeField] private Button _saveSimConfButton;
        [SerializeField] private Button _startSimButton;

        private SimulationActions _simulationActions;
        private string _vehicleBundlePath;
        private string _environmentBundlePath;
        private GameObject _vehiclePrefab;
        private GameObject _environmentPrefab;

        private void Start()
        {
            // get launch script
            _simulationActions = GetComponent<SimulationActions>();

            // load initial gui
            // update gui when an assetbundle is selected
            // remember old loaded prefabs

            // Add listeners to buttons
            _loadVehicleBundleButton.onClick.AddListener(LoadVehicleBundleButtonOnClick);
            _loadEnvironmentBundleButton.onClick.AddListener(LoadEnvironmentBundleButtonOnClick);

            // Add listener to start button
            _startSimButton.onClick.AddListener(StartSim);
        }

        private void StartSim()
        {
            // load prefabs for simulation
            _vehiclePrefab = LoadPrefab(_vehicleBundlePath);
            _environmentPrefab = LoadPrefab(_environmentBundlePath);

            // unload bundles
            // AssetBundle.UnloadAllAssetBundles(true);

            // save GUI
            UpdateGUIFields();
            StoreGUIFields();

            // set spawn point
            // var spawnPoint = VehicleSpawnPoint(
            //     float.Parse(_positionFieldX.text),
            //     float.Parse(_positionFieldY.text),
            //     float.Parse(_positionFieldZ.text),
            //     float.Parse(_rotationFieldX.text),
            //     float.Parse(_rotationFieldY.text),
            //     float.Parse(_rotationFieldZ.text),
            //     float.Parse(_rotationFieldW.text),
            //     CoordSyss.Unity);
            var spawnPoint = VehicleSpawnPoint(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                CoordSyss.Unity);


            // launch simulation
            _simulationActions.Launch(_vehiclePrefab, _environmentPrefab, spawnPoint, 0, 0);
        }

        #region config

        public void LoadSimConf()
        {
            // load simulation configuration
            // pop up file selection dialog for selecting the config file
            
            var paths = StandaloneFileBrowser.OpenFilePanel("Open Simulation Configuration", "", "json", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                // Load the simulation configuration from the selected file
            }
            else
            {
                Debug.Log("File selection was canceled or no file selected.");
            }
        }

        public void SaveSimConf()
        {
            // save simulation configuration
            // pop up file selection dialog for saving the config file
            
            var paths = StandaloneFileBrowser.SaveFilePanel("Save Simulation Configuration", "", "config", "json");
            if (!string.IsNullOrEmpty(paths))
            {
                // Save the simulation configuration to the selected file
            }
            else
            {
                Debug.Log("File saving was canceled.");
            }
        }

        private void UpdateGUIFields()
        {
            // Update dropdowns or any other GUI elements based on loaded prefabs
            // Update visual area or other GUI elements related to the vehicle

            if (_vehiclePrefab != null)
            {
                _vehiclesDropdown.options.Add(new Dropdown.OptionData(_vehiclePrefab.name));
            }

            if (_environmentPrefab != null)
            {
                _environmentsDropdown.options.Add(new Dropdown.OptionData(_environmentPrefab.name));
            }
        }

        private void StoreGUIFields()
        {
            // save simulation configuration to file
        }

        #endregion

        #region vehicle

        private void VehicleRender()
        {
            // render vehicle for gui
        }

        #endregion

        #region environment

        private void SetSpawnPointFromDropdown()
        {
            // set spawn point from dropdown
        }

        // Handle unity coords for now
        private static Tuple<Vector3, Quaternion> VehicleSpawnPoint(float posX, float posY, float posZ, float rotX,
            float rotY, float rotZ, float rotW, CoordSyss coordSys)
        {
            Vector3 posVector = new();
            Quaternion rotQuaternion = new();

            switch (coordSys)
            {
                case CoordSyss.Unity:
                    posVector.x = posX;
                    posVector.y = posY;
                    posVector.z = posZ;
                    rotQuaternion.x = rotX;
                    rotQuaternion.y = rotY;
                    rotQuaternion.z = rotZ;
                    rotQuaternion.w = rotW;
                    break;
                case CoordSyss.LatLong:
                    // convert latlong to unity
                    break;
                case CoordSyss.Mgrs:
                    // convert mgrs to unity
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(coordSys), coordSys, null);
            }

            return new Tuple<Vector3, Quaternion>(posVector, rotQuaternion);
        }

        private void EnvironmentRender()
        {
            // render environment for gui
        }

        #endregion

        #region button functions

        private void LoadVehicleBundleButtonOnClick()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Open Vehicle Asset Bundle", "", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                _vehicleBundlePath = paths[0];
                Debug.Log("vehicle bundle path:" + _vehicleBundlePath);
            }
            else
            {
                Debug.Log("File selection was canceled or no file selected.");
            }
        }
        
        private void LoadEnvironmentBundleButtonOnClick()
        {
            var paths = StandaloneFileBrowser.OpenFilePanel("Open Vehicle Asset Bundle", "", "", false);
            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                _environmentBundlePath = paths[0];
            }
            else
            {
                Debug.Log("File selection was canceled or no file selected.");
            }
        }
        

        #endregion

        #region helpers

        private static string GetBundlePathFromField(InputField inputField)
        {
            try
            {
                var bundlePath = inputField.text;
                return bundlePath;
            }
            catch (Exception e)
            {
                e = new Exception($"Failed to get bundle path from input field: {inputField.text}", e);
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion
    }
}
