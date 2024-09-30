using System;
using System.Collections;
using System.Collections.Generic;
using AWSIM.Loader.SimulationCore;
using AWSIM.Scripts.UI;
using AWSIM.Scripts.UI.Toggle;
using AWSIM.TrafficSimulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWSIM.Scripts.Loader.SimulationCore
{
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
        // add sensor config in the future

        public void Launch(GameObject vehiclePrefab, GameObject environmentPrefab,
            Tuple<Vector3, Quaternion> spawnPoint, float simParams, GraphicSettings graphicSettings)
        {
            // Application.targetFrameRate = graphicSettings.FrameRateLimit;

            StartCoroutine(LoadSimulation(vehiclePrefab, environmentPrefab, spawnPoint));
        }

        public void ReturnToLaunchpad()
        {
            SceneManager.LoadScene(LaunchpadSceneName, LoadSceneMode.Single);
        }

        private IEnumerator LoadSimulation(GameObject vehiclePrefab, GameObject environmentPrefab,
            Tuple<Vector3, Quaternion> spawnPoint)
        {
            // turn off loader gui and transition into loading gui
            _launchpadCanvas.enabled = false;
            _transitionCanvas.enabled = true;
            yield return new WaitForEndOfFrame();

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
            ReInitializeScripts();

            // Turn off transition canvas
            _transitionCanvas.enabled = false;
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

        // very ugly, need to fix this (mozzz)
        private static void ReInitializeScripts()
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