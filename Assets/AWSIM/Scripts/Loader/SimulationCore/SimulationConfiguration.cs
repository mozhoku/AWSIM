using UnityEngine;

namespace AWSIM.Scripts.Loader.SimulationCore
{
    public class SimulationConfiguration : MonoBehaviour
    {
        public string configName;
        public string description;
        public string creator;
        public string dateCreated;

        public string vehicleBundleName;
        public string vehicleBundleID;

        public string urdfFileName;
        public string urdfFileID;

        public string environmentBundleName;
        public string environmentBundleID;
    }
}
