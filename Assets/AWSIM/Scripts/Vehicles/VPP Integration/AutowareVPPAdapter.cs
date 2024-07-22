using System;
using UnityEngine;
using VehiclePhysics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public class AutowareVPPAdapter : MonoBehaviour
    {
        public enum VPPSignal
        {
            None,
            Left,
            Right,
            Hazard
        }

        public enum VPPControlMode
        {
            NoCommand,
            Autonomous,
            AutonomousSteerOnly,
            AutonomousVelocityOnly,
            Manual,
            Disengaged,
            NotReady
        }

        // [Header("Physics Settings (experimental)")] [SerializeField]
        // float sleepVelocityThreshold;
        //
        // [SerializeField] float sleepTimeThreshold;

        [SerializeField] private VPWheelCollider _frontWheelCollider1;
        [SerializeField] private VPWheelCollider _frontWheelCollider2;

        /// <summary>
        /// Inputs
        /// </summary>

        // Initial Position Inputs from Rviz
        [NonSerialized] public Vector3 PositionInput;

        [NonSerialized] public Quaternion RotationInput;
        [NonSerialized] public bool WillUpdatePositionInput;

        // Control commands from Autoware

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
        [NonSerialized] public VPPSignal SignalInput;
        private VPPSignal _signalInput => SignalInput;

        // Control mode from Autoware
        [NonSerialized] public VPPControlMode ControlModeInput;
        private VPPControlMode _controlModeInput => ControlModeInput;

        // Outputs from Unity/VPP
        [NonSerialized] public VPPControlMode VPControlModeReport;
        [NonSerialized] public int VPGearReport;
        [NonSerialized] public Vector3 VPVelocityReport;
        [NonSerialized] public Vector3 VPAngularVelocityReport;
        [NonSerialized] public float VPSteeringReport;
        [NonSerialized] public int VPTurnIndicatorsReport;
        [NonSerialized] public int VPHazardLightsReport;

        // VPP components
        private VPVehicleController _vehicleController;
        // private VPTelemetry _telemetry;
        // private VPVisualEffects _visualEffects;
        // private VPVehicleToolkit _toolkit;

        private Rigidbody _rigidbody;
        private VehiclePedalMapLoader _pedalMap;

        private float _currentSpeed;
        private float _previousAcceleration;
        private float _currentJerk;

        [SerializeField] private float _updatePositionOffsetY = 1.33f;
        [SerializeField] private float _updatePositionRayOriginY = 1000.0f;

        private void Start()
        {
            _vehicleController = GetComponent<VPVehicleController>();
            _rigidbody = GetComponent<Rigidbody>();
            _pedalMap = GetComponent<VehiclePedalMapLoader>();
        }

        private void FixedUpdate()
        {
            // Update the ego position depending on RViz Input.
            if (WillUpdatePositionInput)
            {
                UpdateEgoPosition();
                WillUpdatePositionInput = false;
            }

            // Control the vehicle based on the control mode
            ControlVehicle(_controlModeInput);
        }

        private void ControlVehicle(VPPControlMode controlMode)
        {
            switch (controlMode)
            {
                case VPPControlMode.NoCommand:
                    break;

                case VPPControlMode.Autonomous:
                    HandleTurnSignal();
                    HandleHazardLights();
                    HandleSteer();
                    HandleGear();
                    HandleAcceleration();
                    break;

                case VPPControlMode.AutonomousSteerOnly:
                    HandleTurnSignal();
                    HandleHazardLights();
                    HandleSteer();
                    HandleGear();
                    break;

                case VPPControlMode.AutonomousVelocityOnly:
                    HandleTurnSignal();
                    HandleHazardLights();
                    HandleGear();
                    HandleAcceleration();
                    break;

                case VPPControlMode.Manual:
                    break;

                case VPPControlMode.Disengaged:
                    break;

                case VPPControlMode.NotReady:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(controlMode), controlMode, null);
            }

            ReportVehicleState();
        }

        private void HandleHazardLights()
        {
        }

        private void HandleTurnSignal()
        {
        }

        private void HandleSteer()
        {
            // set wheel angles for now (will simulate steering wheel input later on)
            _vehicleController.wheelState[0].steerAngle = _steerAngleInput;
            _frontWheelCollider1.steerAngle = _steerAngleInput;
            _vehicleController.wheelState[1].steerAngle = _steerAngleInput;
            _frontWheelCollider2.steerAngle = _steerAngleInput;
        }

        private void HandleGear()
        {
            _vehicleController.data.bus[Channel.Input][InputData.AutomaticGear] = (int)_automaticShiftInput;
        }

        private void HandleAcceleration()
        {
            if (_isAccelerationDefinedInput)
            {
                // set accel
                if (_accelerationInput > 0 || _velocityInput > 0)
                {
                    var throttlePercent = VehiclePedalMapLoader.GetPedalPercent(_pedalMap.AccelMap,
                        _pedalMap.AccelMapVertical,
                        _pedalMap.AccelMapHeaders, _accelerationInput, _currentSpeed);
                    // Debug.Log("Throttle %: " + throttlePercent);
                    throttlePercent = RemapValue(throttlePercent, 0f, 0.5f, 0, 5000);
                    _vehicleController.data.bus[Channel.Input][InputData.Throttle] = (int)throttlePercent;
                }

                // set brake
                if (_accelerationInput < 0 || _velocityInput < 0)
                {
                    var brakePercent = VehiclePedalMapLoader.GetPedalPercent(_pedalMap.BrakeMap,
                        _pedalMap.BrakeMapVertical,
                        _pedalMap.BrakeMapHeaders, _accelerationInput, _currentSpeed);
                    // Debug.Log("Brake %: " + brakePercent);
                    brakePercent = RemapValue(brakePercent, -0f, 0.8f, 0, 8000);
                    _vehicleController.data.bus[Channel.Input][InputData.Brake] = (int)brakePercent;
                }
            }

            // Store current values
            _currentSpeed = _vehicleController.speed;
            if (_isJerkDefinedInput)
            {
                float currentAcceleration = _vehicleController.localAcceleration.magnitude;
                _currentJerk = (currentAcceleration - _previousAcceleration) / Time.fixedDeltaTime;
                _previousAcceleration = currentAcceleration;
            }
        }

        // TODO: report jerk state (mozzz)
        private void ReportVehicleState()
        {
            VPControlModeReport = _controlModeInput;
            VPHazardLightsReport = (int)_signalInput;
            VPTurnIndicatorsReport = (int)_signalInput;
            VPSteeringReport = _frontWheelCollider1.steerAngle;
            VPGearReport = _vehicleController.data.bus[Channel.Vehicle][VehicleData.GearboxMode];
            VPVelocityReport = transform.InverseTransformDirection(_rigidbody.velocity.normalized * _currentSpeed);
            VPAngularVelocityReport = transform.InverseTransformDirection(_rigidbody.angularVelocity);
        }

        private void UpdateEgoPosition()
        {
            // Method to update the position based on PositionInput
            Vector3 rayOrigin = new Vector3(PositionInput.x, _updatePositionRayOriginY, PositionInput.z);
            Vector3 rayDirection = Vector3.down;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Mathf.Infinity))
            {
                PositionInput = new Vector3(PositionInput.x, hit.point.y + _updatePositionOffsetY, PositionInput.z);
                transform.SetPositionAndRotation(PositionInput, RotationInput);
            }
            else
            {
                Debug.LogWarning(
                    "No mesh or collider detected on target location. Please ensure that the target location is on a mesh or collider.");
            }
        }

        private static int RemapValue(float value, float from1, float to1, int from2, int to2)
        {
            return (int)((value - from1) / (to1 - from1) * (to2 - from2) + from2);
        }
    }
}
