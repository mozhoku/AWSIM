using System;
using System.Collections.Generic;
using AWSIM.Loader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWSIM.Scripts.Loader.SimulationLauncher
{
    public class
    public class GraphicSettings
    {
        public bool useShadows;
        public bool usePostProcessing;
        public bool useAntiAliasing;
        public bool useVSync;
        public int targetFrameRate;
    }
    public class SimulationActions : MonoBehaviour
    {
        [SerializeField] private Canvas _launchpadCanvas;
        [SerializeField] private Canvas _transitionCanvas;

        [SerializeField] private string LaunchpadSceneName = "Launchpad";
        [SerializeField] private string SimulationCoreSceneName = "AutowareSimulation";
        [SerializeField] private string VehicleSceneName = "VehicleScene";
        [SerializeField] private string EnvironmentSceneName = "EnvironmentScene";

        // Optionally, a dictionary for storing spawn points
        private Dictionary<string, Transform> _spawnPoints;

        // Have scenes have their own spawnPoints defined for vehicles: preferably as dictionaries for transform and name for the GUI
        // Give option to use the default spawn points
        // If user wants to give his own point:
        // Get user input from GUI (latlon, mgrs, Unity xyz)
        // ATM WE DON'T HAVE SPAWN POINTS DEFINED IN THE SCENES

        //add sensor config in the future
        public void Launch(GameObject vehiclePrefab, GameObject environmentPrefab,
            Tuple<Vector3, Quaternion> spawnPoint, float simParams, float graphicSettings)
        {
            // turn off loader gui and transition into a waiting gui
            _launchpadCanvas = GetComponent<Canvas>();
            _launchpadCanvas.enabled = false;
            _transitionCanvas.enabled = true;

            // Save loaded bundles for other sessions as options for the dropdowns.

            // Load the core simulation scene
            SceneManager.LoadScene(SimulationCoreSceneName, LoadSceneMode.Additive);

            // Load the vehicle scene
            SceneManager.LoadScene(VehicleSceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(VehicleSceneName));
            var vehicle = Instantiate(vehiclePrefab);

            // Load the environment scene
            SceneManager.LoadScene(EnvironmentSceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(EnvironmentSceneName));
            var environment = Instantiate(environmentPrefab);

            // set prefabs to their positions in the simulation scene
            vehicle.transform.SetPositionAndRotation(spawnPoint.Item1, spawnPoint.Item2);
            environment.transform.position = new Vector3(0, 0, 0);
            // environment will be 0 usually, might change for multi scene confs

            var awsimConfiguration = new AWSIMConfiguration
            {
                mapConfiguration =
                {
                    mapName = "default",
                    useShadows = true // this shouldn't be a param in mapconf. Graphics?
                },
                simulationConfiguration =
                {
                    useTraffic = true,
                    timeScale = 1.0f
                },
                egoConfiguration =
                {
                    egoVehicleName = vehiclePrefab.name,
                    egoPosition = spawnPoint.Item1,
                    egoEulerAngles = spawnPoint.Item2.eulerAngles
                }
            };
        }

        public void ReturnToLaunchpad()
        {
            SceneManager.LoadScene(LaunchpadSceneName, LoadSceneMode.Single);
        }
    }
}
