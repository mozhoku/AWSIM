using System;
using UnityEngine;
using VehiclePhysics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public enum VPPTurnSignal
    {
        NONE,
        LEFT,
        RIGHT,
        HAZARD
    }

    public class AutowareVPPAdapter : MonoBehaviour
    {
        [Header("Physics Settings (experimental)")] [SerializeField]
        float sleepVelocityThreshold;

        [SerializeField] float sleepTimeThreshold;

        // Inputs

        // Initial Position Inputs from Rviz
        [NonSerialized] public Vector3 PositionInput;
        [NonSerialized] public Quaternion RotationInput;
        [NonSerialized] public bool WillUpdatePositionInput;

        //Gear commands from Autoware
        //gear
        [NonSerialized] public int AutomaticShiftInput;

        // Control commands from Autoware
        //longitudinal
        [NonSerialized] public float VelocityInput;
        [NonSerialized] public bool IsAccelerationDefinedInput;
        [NonSerialized] public float AccelerationInput;
        [NonSerialized] public bool IsJerkDefinedInput;
        [NonSerialized] public float JerkInput;

        //lateral
        [NonSerialized] public float SteerAngleInput;
        [NonSerialized] public bool IsDefinedSteeringTireRotationRateInput;
        [NonSerialized] public float SteeringTireRotationRateInput;

        // Signal commands from Autoware
        [NonSerialized] public VPPTurnSignal SignalInput;

        // Outputs from Unity/VPP
        // private int _controlModeReport;
        [NonSerialized] public int VPGearReport;
        [NonSerialized] public Vector3 VPVelocityReport;
        [NonSerialized] public Vector3 VPAngularVelocityReport;
        [NonSerialized] public int VPSteeringReport;
        [NonSerialized] public int VPTurnIndicatorsReport;
        [NonSerialized] public int VPHazardLightsReport;


        private VPVehicleController _vehicleController;
        private Rigidbody _rigidbody;

        // private VPVisualEffects _visualEffects;
        private VPVehicleToolkit _toolkit;

        private void Start()
        {
            _vehicleController = GetComponent<VPVehicleController>();
            _rigidbody = GetComponent<Rigidbody>();

            // Set vehicle to parking gear
            // _vehicleController.data.Set(Channel.Settings, InputData.GearShift, 1);
            // _visualEffects = GetComponent<VPVisualEffects>();
        }

        //// VPP INPUT DATA
        // public struct InputData
        // {
        //     public const int Steer = 0;
        //     public const int Throttle = 1;
        //     public const int Brake = 2;
        //     public const int Handbrake = 3;
        //     public const int Clutch = 4;
        //     public const int ManualGear = 5;
        //     public const int AutomaticGear = 6;
        //     public const int GearShift = 7;
        //     public const int Retarder = 8;
        //     public const int Key = 9;
        //     public const int Max = 10;
        // }

        private void FixedUpdate()
        {
            // Update the ego position depending on RViz Input.
            if (WillUpdatePositionInput)
            {
                UpdateEgoPosition();
                WillUpdatePositionInput = false;
            }

            Debug.Log("AutomaticShiftInput: " + AutomaticShiftInput);
            // Update vehicle with ros2 input
            _vehicleController.data.bus[Channel.Input][InputData.AutomaticGear] = AutomaticShiftInput;

            Debug.Log("AccelerationInput: " + AccelerationInput);

            // temporary input handling for acceleration

            if (AccelerationInput > 0 && _vehicleController.data.bus[Channel.Vehicle][VehicleData.Speed] > 0)
            {
                _vehicleController.data.bus[Channel.Input][InputData.Throttle] += 2500; // + throttle
                _vehicleController.data.bus[Channel.Input][InputData.Brake] = 0; // 0 brake
            }
            else if (AccelerationInput > 0 && _vehicleController.data.bus[Channel.Vehicle][VehicleData.Speed] < 0)
                _vehicleController.data.bus[Channel.Input][InputData.Brake] += 1000; // + brake
            else if (AccelerationInput < 0 && _vehicleController.data.bus[Channel.Vehicle][VehicleData.Speed] > 0)
                _vehicleController.data.bus[Channel.Input][InputData.Brake] += 1000; // + brake
            else if (AccelerationInput < 0 && _vehicleController.data.bus[Channel.Vehicle][VehicleData.Speed] < 0)
            {
                _vehicleController.data.bus[Channel.Input][InputData.Throttle] += 2500; // + throttle
                _vehicleController.data.bus[Channel.Input][InputData.Brake] = 0; // 0 brake
            }
            else if (AccelerationInput == 0)
                _vehicleController.data.bus[Channel.Input][InputData.Brake] += 2000; // + brake

            Debug.Log("SteerAngleInput: " + SteerAngleInput);
            // temporary input handling for steering
            switch (SteerAngleInput)
            {
                case > 1:
                    _vehicleController.data.bus[Channel.Input][InputData.Steer] += 1000;
                    break;
                case < -1:
                    _vehicleController.data.bus[Channel.Input][InputData.Steer] -= 1000;
                    break;
                default:
                    _vehicleController.data.bus[Channel.Input][InputData.Steer] = 0;
                    break;
            }

            // _vehicleController.data.bus[Channel.Input][InputData.] = (int)SignalInput;


            // Update values sent to the ros2 publisher
            VPGearReport = _vehicleController.data.bus[Channel.Vehicle][VehicleData.GearboxMode];
            Debug.Log("VPGearReport: " + VPGearReport);

            // taken from rigidbody
            VPVelocityReport = _rigidbody.velocity;
            VPAngularVelocityReport = _rigidbody.angularVelocity;
            Debug.Log("VPVelocityReport: " + VPVelocityReport);
            Debug.Log("VPAngularVelocityReport: " + VPAngularVelocityReport);

            // return wheel angle for now
            VPSteeringReport = (int)_vehicleController.wheelState[0].steerAngle;
            Debug.Log("VPSteeringReport: " + VPSteeringReport);
        }

        private void UpdateEgoPosition()
        {
            // Method to update the position based on PositionInput
            Vector3 rayOrigin = new Vector3(PositionInput.x, 1000.0f, PositionInput.z);
            Vector3 rayDirection = Vector3.down;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Mathf.Infinity))
            {
                PositionInput = new Vector3(PositionInput.x, hit.point.y + 1.33f, PositionInput.z);
                transform.SetPositionAndRotation(PositionInput, RotationInput);
            }
            else
            {
                Debug.LogWarning(
                    "No mesh or collider detected on target location. Please ensure that the target location is on a mesh or collider.");
            }
        }
    }
}
