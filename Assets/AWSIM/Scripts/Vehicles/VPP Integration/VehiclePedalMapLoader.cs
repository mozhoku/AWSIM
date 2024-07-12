using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public class VehiclePedalMapLoader : MonoBehaviour
    {
        public TextAsset accelMapCsv;
        public TextAsset brakeMapCsv;
        private Dictionary<float, List<float>> _accelMap;
        private Dictionary<float, List<float>> _brakeMap;

        void Start()
        {
            _accelMap = LoadMap(accelMapCsv);
            _brakeMap = LoadMap(brakeMapCsv);
        }

        private static Dictionary<float, List<float>> LoadMap(TextAsset csv)
        {
            Dictionary<float, List<float>> map = new Dictionary<float, List<float>>();
            StringReader reader = new StringReader(csv.text);

            // Read the header line
            string line = reader.ReadLine();
            string[] headers = line.Split(',');

            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');
                float key = float.Parse(values[0], CultureInfo.InvariantCulture);
                List<float> data = new List<float>();

                for (int i = 1; i < values.Length; i++)
                {
                    data.Add(float.Parse(values[i], CultureInfo.InvariantCulture));
                }

                map[key] = data;
            }

            return map;
        }

        public float GetAccelValue(float input)
        {
            // Implement interpolation or lookup logic based on accelMap
            return InterpolateMap(_accelMap, input);
        }

        public float GetBrakeValue(float input)
        {
            // Implement interpolation or lookup logic based on brakeMap
            return InterpolateMap(_brakeMap, input);
        }

        private static float InterpolateMap(Dictionary<float, List<float>> map, float input)
        {
            // Implement your interpolation logic here
            // For simplicity, let's just return the closest value for now
            float closestKey = float.MaxValue;
            foreach (var key in map.Keys)
            {
                if (Mathf.Abs(key - input) < Mathf.Abs(closestKey - input))
                {
                    closestKey = key;
                }
            }

            // Assuming we use the first column's values for this example
            return map[closestKey][0];
        }
    }
}
