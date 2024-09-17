using System;
using System.Collections;
using System.Collections.Generic;
using AWSIM.Scripts.UI;
using AWSIM.Scripts.UI.Toggle;
using AWSIM.TrafficSimulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWSIM.Scripts.Loader.SimulationLauncher
{
    public class GraphicSettings
    {
        public bool UseShadows;
        public bool UsePostProcessing;
        public bool UseAntiAliasing;
        public bool UseVSync;

        public int FrameRateLimit;
        // TODO: Add more settings (mozzz)
    }

    public class SimulationActions : MonoBehaviour
    {
        [SerializeField] private Canvas _launchpadCanvas;
        [SerializeField] private Canvas _transitionCanvas;

        [SerializeField] private string LaunchpadSceneName = "Launchpad";
        [SerializeField] private string SimulationSceneName = "AWSIMSimulation";
        [SerializeField] private string VehicleSceneName = "VehicleScene";
        [SerializeField] private string EnvironmentSceneName = "EnvironmentScene";

        // Optionally, a dictionary for storing spawn points
        private Dictionary<string, Transform> _spawnPoints;

        // Have scenes have their own spawnPoints defined for vehicles: preferably as dictionaries for transform and name for the GUI
        // Give option to use the default spawn points
        // If user wants to give his own point:
        // Get user input from GUI (latlon, mgrs, Unity xyz)
        // ATM WE DON'T HAVE SPAWN POINTS DEFINED IN THE SCENES
        // generate screenshots for the UI

        //add sensor config in the future
        public void Launch(GameObject vehiclePrefab, GameObject environmentPrefab,
            Tuple<Vector3, Quaternion> spawnPoint, float simParams, float graphicSettings)
        {
            // turn off loader gui and transition into a waiting gui
            _launchpadCanvas = GetComponent<Canvas>();
            _launchpadCanvas.enabled = false;
            _transitionCanvas.enabled = true;

            // TODO: Save loaded bundles for other sessions as options for the dropdowns.

            StartCoroutine(LoadSimulation(vehiclePrefab, environmentPrefab, spawnPoint));

            // var awsimConfiguration = new AWSIMConfiguration
            // {
            //     mapConfiguration =
            //     {
            //         mapName = "default",
            //         useShadows = true // this shouldn't be a param in mapconf. Graphics?
            //     },
            //     simulationConfiguration =
            //     {
            //         useTraffic = true,
            //         timeScale = 1.0f
            //     },
            //     egoConfiguration =
            //     {
            //         egoVehicleName = vehiclePrefab.name,
            //         egoPosition = spawnPoint.Item1,
            //         egoEulerAngles = spawnPoint.Item2.eulerAngles
            //     }
            // };
        }

        public void ReturnToLaunchpad()
        {
            SceneManager.LoadScene(LaunchpadSceneName, LoadSceneMode.Single);
        }

        private IEnumerator LoadSimulation(GameObject vehiclePrefab, GameObject environmentPrefab,
            Tuple<Vector3, Quaternion> spawnPoint)
        {
            // Load the scenes
            yield return StartCoroutine(LoadScene(SimulationSceneName));
            yield return StartCoroutine(LoadScene(EnvironmentSceneName));
            yield return StartCoroutine(LoadScene(VehicleSceneName));

            // Setup EnvironmentScene
            var environmentScene = SceneManager.GetSceneByName(EnvironmentSceneName);
            if (environmentScene.isLoaded)
            {
                SceneManager.SetActiveScene(environmentScene);
                var environment = Instantiate(environmentPrefab);
                environment.transform.position = new Vector3(0, 0, 0); // Default position
            }

            // Setup CoreSimulationScene
            var vehicleScene = SceneManager.GetSceneByName(VehicleSceneName);
            if (vehicleScene.isLoaded)
            {
                SceneManager.SetActiveScene(vehicleScene);
                var vehicle = Instantiate(vehiclePrefab);
                vehicle.transform.SetPositionAndRotation(spawnPoint.Item1, spawnPoint.Item2);
            }

            // Fix UI references
            ReInitializeGUI();
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            var asyncLoadScene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!asyncLoadScene.isDone)
            {
                Debug.Log("Loading Scene: " + sceneName);
                yield return null;
            }
        }

        private static void ReInitializeGUI()
        {
            // Set camera GUI
            FollowCamera followCamera = FindObjectOfType<FollowCamera>();
            FindObjectOfType<MainCameraViewUI>().SetFollowCamera(followCamera);

            // // Set scene time scale
            // DemoUI demoUi = GameObject.FindObjectOfType<DemoUI>();
            // demoUi.SetTimeScale(simulationConfiguration.timeScale);
            // demoUi.TimeScaleSlider.value = simulationConfiguration.timeScale;

            TrafficControlManager trafficControlManager = FindObjectOfType<TrafficControlManager>();
            trafficControlManager.TrafficManager = FindObjectOfType<TrafficManager>();
            trafficControlManager.Activate();

            UIKeyboardControlToggle uiKeyboardControlToggle = FindObjectOfType<UIKeyboardControlToggle>();
            uiKeyboardControlToggle.Activate();

            UITrafficControlVisibilityToggle uiTrafficControlVisibilityToggle =
                FindObjectOfType<UITrafficControlVisibilityToggle>();
            uiTrafficControlVisibilityToggle.Activate();

            UITrafficControlPlayToggle uiTrafficControlPlayToggle = FindObjectOfType<UITrafficControlPlayToggle>();
            uiTrafficControlPlayToggle.Activate();

            UITrafficVehicleDensity uiTrafficVehicleDensity = FindObjectOfType<UITrafficVehicleDensity>();
            uiTrafficVehicleDensity.Activate();

            BirdEyeView birdEyeView = FindObjectOfType<BirdEyeView>();
            birdEyeView.Activate();

            GraphicsSettings graphicsSettings = FindObjectOfType<GraphicsSettings>();
            graphicsSettings.Activate();

            UISensorInteractionPanel uiSensorInteractionPanel = FindObjectOfType<UISensorInteractionPanel>();
            uiSensorInteractionPanel.Activate();

            UIMainCameraToggle uiMainCameraToggle = FindObjectOfType<UIMainCameraToggle>();
            uiMainCameraToggle.Activate();

            // Set traffic on/off
            var trafficSims = FindObjectsOfType<TrafficManager>();
            foreach (var trafficSim in trafficSims)
            {
                // trafficSim.gameObject.SetActive(simulationConfiguration.useTraffic);
            }

            // Turn shadows for directional light
            var lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.shadows = LightShadows.Soft;
                }
            }
        }
    }
}
