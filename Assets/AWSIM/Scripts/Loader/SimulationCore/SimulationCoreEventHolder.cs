using UnityEngine;

namespace AWSIM.Scripts.Loader.SimulationCore
{
    /// <summary>
    /// Used to hold events used for Simulation Core Scene GUI updates etc.
    /// </summary>
    public class SimulationCoreEventHolder : MonoBehaviour
    {
        // event for vehicle bundle loaded
        // it will pass the BundleInfo object
        public delegate void VehicleBundleLoaded(BundleInfo bundleInfo);
        public static event VehicleBundleLoaded OnVehicleBundleLoaded;

        // event for vehicle selected from dropdown
        // it will pass the name and bundle path of the vehicle
        public delegate void VehicleSelected(string vehicleName, string bundlePath);
        public static event VehicleSelected OnVehicleSelected;

        // event for urdf loaded
        // it will pass the name of the urdf
        public delegate void URDFLoaded(string urdfName);
        public static event URDFLoaded OnURDFLoaded;

        // event for urdf selected from dropdown
        // it will pass the name and path of the urdf
        public delegate void URDFSelected(string urdfName, string urdfPath);
        public static event URDFSelected OnURDFSelected;

        // event for environment bundle loaded
        // it will pass the BundleInfo object
        public delegate void EnvironmentBundleLoaded(BundleInfo bundleInfo);
        public static event EnvironmentBundleLoaded OnEnvironmentBundleLoaded;

        // event for environment selected from dropdown
        // it will pass the name and bundle path of the environment
        public delegate void EnvironmentSelected(string environmentName, string bundlePath);
        public static event EnvironmentSelected OnEnvironmentSelected;

        // event for spawn point selected
        public delegate void SpawnPointSelected(string spawnPointName);
        public static event SpawnPointSelected OnSpawnPointSelected;

        // event for spawn point added
        public delegate void SpawnPointAdded(string spawnPointName);
        public static event SpawnPointAdded OnSpawnPointAdded;

        // event for Configuration file loaded
        public delegate void ConfigurationFileLoaded(string configurationFileName);
        public static event ConfigurationFileLoaded OnConfigurationFileLoaded;

        // event for Configuration file Saved
        // it will pass the name of the configuration file
        public delegate void ConfigurationFileSaved(string configurationFileName);
        public static event ConfigurationFileSaved OnConfigurationFileSaved;
    }
}
