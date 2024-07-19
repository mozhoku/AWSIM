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
        // [Header("Physics Settings (experimental)")] [SerializeField]
        // float sleepVelocityThreshold;
        //
        // [SerializeField] float sleepTimeThreshold;

        [SerializeField] private VPWheelCollider _frontWheelCollider1;
        [SerializeField] private VPWheelCollider _frontWheelCollider2;

        // Inputs

        // Initial Position Inputs from Rviz
        [NonSerialized] public Vector3 PositionInput;
        [NonSerialized] public Quaternion RotationInput;
        [NonSerialized] public bool WillUpdatePositionInput;

        ////////////////////////////////////
        // Control commands from Autoware //
        ////////////////////////////////////
        //longitudinal
        [NonSerialized] public float VelocityInput;
        private float _velocityInput => VelocityInput;

        [NonSerialized] public bool IsAccelerationDefinedInput;
        private bool _isAccelerationDefinedInput => IsAccelerationDefinedInput;

        [NonSerialized] public float AccelerationInput;
        private float _accelerationInput => AccelerationInput;

        [NonSerialized] public bool IsJerkDefinedInput;
        private bool _isJerkDefinedInput => IsJerkDefinedInput;

        [NonSerialized] public float JerkInput;
        private float _jerkInput => JerkInput;

        //lateral
        [NonSerialized] public float SteerAngleInput;
        private float _steerAngleInput => SteerAngleInput;

        [NonSerialized] public bool IsDefinedSteeringTireRotationRateInput;
        private bool _isDefinedSteeringTireRotationRateInput => IsDefinedSteeringTireRotationRateInput;

        [NonSerialized] public float SteeringTireRotationRateInput;
        private float _steeringTireRotationRateInput => SteeringTireRotationRateInput;

        //gear
        [NonSerialized] public Gearbox.AutomaticGear AutomaticShiftInput;
        private Gearbox.AutomaticGear _automaticShiftInput => AutomaticShiftInput;

        // Signal commands from Autoware
        [NonSerialized] public VPPTurnSignal SignalInput;
        private VPPTurnSignal _signalInput => SignalInput;

        // Outputs from Unity/VPP
        // private int _controlModeReport;
        [NonSerialized] public int VPGearReport;
        [NonSerialized] public Vector3 VPVelocityReport;
        [NonSerialized] public Vector3 VPAngularVelocityReport;
        [NonSerialized] public float VPSteeringReport;
        [NonSerialized] public int VPTurnIndicatorsReport;
        [NonSerialized] public int VPHazardLightsReport;

        private VPVehicleController _vehicleController;

        // private VPTelemetry _telemetry;
        // private VPVisualEffects _visualEffects;
        // private VPVehicleToolkit _toolkit;
        private Rigidbody _rigidbody;
        private VehiclePedalMapLoader _pedalMap;

        private float currentSpeed;
        private float previousAcceleration;
        private float currentJerk;

        private void Start()
        {
            _vehicleController = GetComponent<VPVehicleController>();
            _rigidbody = GetComponent<Rigidbody>();
            _pedalMap = GetComponent<VehiclePedalMapLoader>();
        }


        private void FixedUpdate()
        {
            ////////////////////////////////
            // Debugs for Autoware Inputs //
            ////////////////////////////////
            // Debug.Log("AutomaticShiftInput: " + AutomaticShiftInput);
            // Debug.Log("Input accel: " + AccelerationInput);
            // Debug.Log("Input velo: " + VelocityInput);
            // Debug.Log("JerkInput: " + JerkInput);

            // Update the ego position depending on RViz Input.
            if (WillUpdatePositionInput)
            {
                UpdateEgoPosition();
                WillUpdatePositionInput = false;
            }

            // directly set wheel angles for now (will simulate steering wheel input later on)
            _vehicleController.wheelState[0].steerAngle = _steerAngleInput;
            _frontWheelCollider1.steerAngle = _steerAngleInput;
            _vehicleController.wheelState[1].steerAngle = _steerAngleInput;
            _frontWheelCollider2.steerAngle = _steerAngleInput;

            // set accel
            if (_accelerationInput > 0 || _velocityInput > 0)
            {
                var throttlePercent = _pedalMap.GetPedalPercent(_pedalMap.AccelMap, _pedalMap.AccelMapVertical,
                    _pedalMap.AccelMapHeaders, _accelerationInput, currentSpeed);
                // Debug.Log("Throttle %: " + throttlePercent);
                throttlePercent = RemapValue(throttlePercent, 0f, 0.5f, 0, 5000);
                _vehicleController.data.bus[Channel.Input][InputData.Throttle] = (int)throttlePercent;
            }

            // set brake
            if (_accelerationInput < 0 || _velocityInput < 0)
            {
                var brakePercent = _pedalMap.GetPedalPercent(_pedalMap.BrakeMap, _pedalMap.BrakeMapVertical,
                    _pedalMap.BrakeMapHeaders, _accelerationInput, currentSpeed);
                // Debug.Log("Brake %: " + brakePercent);
                brakePercent = RemapValue(brakePercent, -0f, 0.8f, 0, 8000);
                _vehicleController.data.bus[Channel.Input][InputData.Brake] = (int)brakePercent;
            }

            // Store current values
            // speed
            currentSpeed = _vehicleController.speed;
            // jerk
            // if (_isJerkDefinedInput)
            // {
            //     float currentAcceleration = _vehicleController.localAcceleration.magnitude;
            //     currentJerk = (currentAcceleration - previousAcceleration) / Time.fixedDeltaTime;
            //     previousAcceleration = currentAcceleration;
            // }

            // set gear
            _vehicleController.data.bus[Channel.Input][InputData.AutomaticGear] = (int)_automaticShiftInput;

            // Update values sent to the ros2 publisher
            VPGearReport = _vehicleController.data.bus[Channel.Vehicle][VehicleData.GearboxMode];
            // Debug.Log("gearCommand: " + _automaticShiftInput);
            // Debug.Log("VPGearReport: " + VPGearReport);
            VPVelocityReport = transform.InverseTransformDirection(_rigidbody.velocity.normalized * currentSpeed);
            VPAngularVelocityReport = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            VPSteeringReport = _frontWheelCollider1.steerAngle;
            // VPHazardLightsReport = (int)_signalInput;
            // VPTurnIndicatorsReport = (int)_signalInput;

            // Debug.Log("rigidbody veloc: " + _rigidbody.velocity.magnitude);
            // Debug.Log("vpp veloc: " + currentSpeed);
            // Debug.Log("VPVelocityReport: " + VPVelocityReport.magnitude);
        }

        private static float RemapValue(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        private static int RemapValue(float value, float from1, float to1, int from2, int to2)
        {
            return (int)((value - from1) / (to1 - from1) * (to2 - from2) + from2);
        }

        /// <summary>
        /// Convert vpp speed to km/h
        /// </summary>
        // private static float ConvertToKmh(float vppSpeed)
        // {
        //     return vppSpeed * 3.6f / 1000;
        // }
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