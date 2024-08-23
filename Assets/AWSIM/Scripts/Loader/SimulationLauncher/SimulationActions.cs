using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWSIM.Scripts.Loader.SimulationLauncher
{
    public class SimulationActions : MonoBehaviour
    {
        [SerializeField] private Canvas _launchpadCanvas;
        [SerializeField] private Canvas _transitionCanvas;

        private BundleLoader _bundleLoader;
        private GameObject _vehiclePrefab;
        private GameObject _environmentPrefab;

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

        public void Launch()
        {
            // turn off loader gui and transition into a waiting gui
            _launchpadCanvas = GetComponent<Canvas>();
            _launchpadCanvas.enabled = false;
            _transitionCanvas.enabled = true;

            // Save loaded bundles for other sessions as options for the dropdowns.

            // Collect references for the simulation scene
            _bundleLoader = GetComponent<BundleLoader>();
            _vehiclePrefab = _bundleLoader.VehiclePrefab;
            _environmentPrefab = _bundleLoader.EnvironmentPrefab;

            // Load the core simulation scene
            SceneManager.LoadScene(SimulationCoreSceneName, LoadSceneMode.Additive);

            // Load the vehicle scene
            SceneManager.LoadScene(VehicleSceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(VehicleSceneName));
            var vehicle = Instantiate(_vehiclePrefab);

            // Load the environment scene
            SceneManager.LoadScene(EnvironmentSceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(EnvironmentSceneName));
            var environment = Instantiate(_environmentPrefab);

            // Unload all asset bundles (since we have the prefabs now)
            AssetBundle.UnloadAllAssetBundles(true);

            // set prefabs to their positions in the simulation scene
            SetPrefabPosition(vehicle, EnvironmentSceneName); // give the vehicle a spawn point
            environment.transform.position =
                new Vector3(0, 0, 0); // will be 0 usually, might change for multi scene confs

            // Update the core scene GUI with the references so toggles can have their references
        }

        public void ReturnToLaunchpad()
        {
            SceneManager.LoadScene(LaunchpadSceneName, LoadSceneMode.Single);
        }

        private void SetPrefabPosition(GameObject prefab, string sceneName)
        {
            // Check if the scene has defined spawn points
            if (_spawnPoints.TryGetValue(sceneName, out var spawnPoint))
            {
                prefab.transform.position = spawnPoint.position;
            }
            else
            {
                prefab.transform.position = Vector3.zero; // Default spawn position
            }
        }
    }
}
