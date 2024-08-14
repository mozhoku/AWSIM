using System;
using System.Collections.Generic;
using AWSIM.Scripts.Vehicles.VPP_Integration.Enums;
using AWSIM.Scripts.Vehicles.VPP_Integration.IVehicleControlModes;
using UnityEngine;
using VehiclePhysics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public class AutowareVPPAdapter : MonoBehaviour
    {
        /// <summary>
        /// This script applies the VPP vehicle inputs.
        /// Vehicle inputs are updated from the `Ros2ToVPPInput.cs`.
        /// Results from this script are sent to the `VPPtoRos2Publisher.cs`.
        /// </summary>

        // Initial Position inputs from Rviz
        [NonSerialized] public bool WillUpdatePositionInput;

        [NonSerialized] public Vector3 PositionInput;
        [NonSerialized] public Quaternion RotationInput;

        /// Control inputs from Autoware

        //longitudinal
        [NonSerialized] public float VelocityInput;

        [NonSerialized] public bool IsAccelerationDefinedInput;
        [NonSerialized] public float AccelerationInput;
        [NonSerialized] public bool IsJerkDefinedInput;
        [NonSerialized] public float JerkInput;
        private float _velocityInput => VelocityInput;
        private bool _isAccelerationDefinedInput => IsAccelerationDefinedInput;
        private float _accelerationInput => AccelerationInput;
        private bool _isJerkDefinedInput => IsJerkDefinedInput;
        private float _jerkInput => JerkInput;

        //lateral
        [NonSerialized] public float SteerAngleInput;
        [NonSerialized] public bool IsDefinedSteeringTireRotationRateInput;
        [NonSerialized] public float SteeringTireRotationRateInput;
        private float _steerAngleInput => SteerAngleInput;
        private bool _isDefinedSteeringTireRotationRateInput => IsDefinedSteeringTireRotationRateInput;
        private float _steeringTireRotationRateInput => SteeringTireRotationRateInput;

        // Gear input from Autoware
        [NonSerialized] public Gearbox.AutomaticGear AutomaticShiftInput;
        private Gearbox.AutomaticGear _automaticShiftInput => AutomaticShiftInput;

        // Signal input from Autoware
        [NonSerialized] public VPPSignal VehicleSignalInput;
        private VPPSignal _vehicleSignalInput => VehicleSignalInput;

        // Emergency input from Autoware
        [NonSerialized] public bool IsEmergencyInput;
        private bool _isEmergencyInput => IsEmergencyInput;

        // Control mode input from Autoware
        [NonSerialized] public VPPControlMode ControlModeInput;
        private VPPControlMode _controlModeInput => ControlModeInput;

        // Actuation commands from Autoware
        public double SteerInput { get; set; }
        private double _steerInput => SteerInput;
        public double BrakeInput { get; set; }
        private double _brakeInput => BrakeInput;
        public double ThrottleInput { get; set; }
        private double _throttleInput => ThrottleInput;


        // Outputs from Unity/VPP
        [NonSerialized] public VPPControlMode VPControlModeReport;
        [NonSerialized] public int VPGearReport;
        [NonSerialized] public Vector3 VPVelocityReport;
        [NonSerialized] public Vector3 VPAngularVelocityReport;
        [NonSerialized] public float VPSteeringReport;
        [NonSerialized] public VPPSignal VPTurnIndicatorReport;
        [NonSerialized] public VPPSignal VPHazardLightsReport;
        [NonSerialized] public double VPThrottleStatusReport;
        [NonSerialized] public double VPBrakeStatusReport;
        [NonSerialized] public double VPSteerStatusReport;

        // VPP components
        private VPVehicleController _vehicleController;

        [Header("Lateral")][SerializeField] private VPWheelCollider[] _frontWheels;

        private Rigidbody _rigidbody;

        /// <summary>
        /// Whether set wheel angle directly from Autoware or simulate with additive steering wheel input
        /// </summary>
        [SerializeField] private bool _simulateSteering;

        /// <summary>
        /// Change applied to steering wheel per fixed update
        /// </summary>
        [Range(0f, 100f)][SerializeField] private float _steerWheelInput = 5f;

        private int _vppSteerFromLastFrame;

        /// <summary>
        /// Brake pedal percent on emergency brake
        /// This value is mapped to [0, 10000] to match VPP input
        /// </summary>
        [Range(0f, 100f)][SerializeField] private float _emergencyBrakePercent = 100f;

        private float _currentSpeed;
        private float _previousAcceleration;
        private float _currentJerk;

        // Control mode variables
        private Dictionary<VPPControlMode, IVehicleControlMode> _controlModes;
        private IVehicleControlMode _currentMode;

        // RViz2 Update position variables
        [Header("RViz2 Update Position")]
        [SerializeField]
        private float _updatePositionOffsetY = 1.33f;

        [SerializeField] private float _updatePositionRayOriginY = 1000f;

        // Pedal calibration stuff
        private int _brakeAmount;
        private int _throttleAmount;
        [SerializeField] private bool _doPedalCalibration;

        private void Awake()
        {   //TODO: Implement the control mode switch from simulator (mozzz)
            // Initialize the control mode as Autonomous
            ControlModeInput = VPPControlMode.Autonomous;
        }

        private void Start()
        {
            _vehicleController = GetComponent<VPVehicleController>();
            _rigidbody = GetComponent<Rigidbody>();

            // Initialize the control modes
            _controlModes = new Dictionary<VPPControlMode, IVehicleControlMode>
            {
                { VPPControlMode.NoCommand, new ControlMode.NoCommand() },
                { VPPControlMode.Autonomous, new ControlMode.Autonomous() },
                { VPPControlMode.AutonomousSteerOnly, new ControlMode.AutonomousSteerOnly() },
                { VPPControlMode.AutonomousVelocityOnly, new ControlMode.AutonomousVelocityOnly() },
                { VPPControlMode.Manual, new ControlMode.Manual() },
                { VPPControlMode.Disengaged, new ControlMode.Disengaged() },
                { VPPControlMode.NotReady, new ControlMode.NotReady() }
            };

            // Set initial vehicle gear to Park to prevent sliding
            _vehicleController.data.bus[Channel.Input][InputData.AutomaticGear] = (int)Gearbox.AutomaticGear.P;
        }

        private void Update()
        {
            // TODO: Implement the control mode switch from simulator (mozzz)
            UserSwitchControlMode();
        }

        private void FixedUpdate()
        {
            // Update the ego position depending on RViz Input.
            if (WillUpdatePositionInput)
            {
                UpdateEgoPosition();
                WillUpdatePositionInput = false;
            }

            if (_doPedalCalibration)
            {
                PedalCalibrationMode();
            }

            // Control the vehicle based on the control mode
            ControlVehicle(_controlModeInput);
            Debug.Log("Control Mode: " + _controlModeInput);

            // Update the publisher values for VPPToRos2Publisher.cs
            ReportVehicleState();
        }

        private void ControlVehicle(VPPControlMode controlMode)
        {
            if (_controlModes.TryGetValue(controlMode, out _currentMode))
            {
                _currentMode.ExecuteControlMode(this);
            }
            else
            {
                Debug.LogWarning("Control mode is not recognized.");
            }
        }

        public void HandleHazardLights()
        {
            // TODO: this is implemented in Ros2ToVPPInput.cs, move it here (mozzz)
        }

        public void HandleTurnSignal()
        {
            // TODO: this is implemented in Ros2ToVPPInput.cs, move it here (mozzz)
        }

        public void HandleSteer()
        {
            if (_simulateSteering)
            {
                SimulateSteeringWheelInput();
            }
            else
            {
                // set wheel angle directly
                _vehicleController.wheelState[0].steerAngle = _steerAngleInput;
                _vehicleController.wheelState[1].steerAngle = _steerAngleInput;
                foreach (var wheel in _frontWheels)
                {
                    wheel.steerAngle = _steerAngleInput;
                }
            }
        }

        private void SimulateSteeringWheelInput()
        {
            // simulate steering wheel input
            if (_steerAngleInput == 0)
                return;

            if (_steerAngleInput > _vehicleController.wheelState[0].steerAngle)
            {
                _vehicleController.data.bus[Channel.Input][InputData.Steer] =
                    _vppSteerFromLastFrame + (int)_steerWheelInput;
            }
            else if (_steerAngleInput < _vehicleController.wheelState[0].steerAngle)
            {
                _vehicleController.data.bus[Channel.Input][InputData.Steer] =
                    _vppSteerFromLastFrame - (int)_steerWheelInput;
            }

            _vppSteerFromLastFrame = _vehicleController.data.bus[Channel.Input][InputData.Steer];
        }

        public void HandleGear()
        {
            _vehicleController.data.bus[Channel.Input][InputData.AutomaticGear] = (int)_automaticShiftInput;
        }

        public void HandleAcceleration()
        {
            // Store current values
            _currentSpeed = _vehicleController.speed;
            if (_isJerkDefinedInput)
            {
                float currentAcceleration = _vehicleController.localAcceleration.magnitude;
                _currentJerk = (currentAcceleration - _previousAcceleration) / Time.fixedDeltaTime;
                _previousAcceleration = currentAcceleration;
            }

            SetThrottle((int)(_throttleInput * 10000));
            SetBrake((int)(_brakeInput * 10000));
        }

        /// <summary>
        /// Range:[0-10000]
        /// </summary>
        private void SetThrottle(int amount)
        {
            _vehicleController.data.bus[Channel.Input][InputData.Throttle] = amount;
        }

        /// <summary>
        /// Range:[0-10000]
        /// </summary>
        private void SetBrake(int amount)
        {
            _vehicleController.data.bus[Channel.Input][InputData.Brake] = amount;
        }

        // TODO: report jerk state (mozzz)
        private void ReportVehicleState()
        {
            VPControlModeReport = _controlModeInput;
            VPHazardLightsReport = _vehicleSignalInput;
            VPTurnIndicatorReport = _vehicleSignalInput;
            VPSteeringReport = _frontWheels[0].steerAngle;
            VPGearReport = _vehicleController.data.bus[Channel.Vehicle][VehicleData.GearboxMode];
            VPVelocityReport =
                transform.InverseTransformDirection(_rigidbody.velocity.normalized * _vehicleController.speed);
            VPAngularVelocityReport = transform.InverseTransformDirection(_rigidbody.angularVelocity);
            VPThrottleStatusReport = _vehicleController.data.bus[Channel.Input][InputData.Throttle] * 0.0001f;
            VPBrakeStatusReport = _vehicleController.data.bus[Channel.Input][InputData.Brake] * 0.0001f;
            VPSteerStatusReport = _vehicleController.data.bus[Channel.Input][InputData.Steer] * 0.0001f;
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

        // TODO: Method to switch control mode based on user input (mozzz)
        private void UserSwitchControlMode()
        {
        }

        // TODO: Add UI and documentation for this method (mozzz)
        /// <summary>
        /// Used to enable Numpad(+,-,0) keys to set constant throttle/brake values for ease of calibration
        /// </summary>
        private void PedalCalibrationMode()
        {
            _vehicleController.data.bus[Channel.Input][InputData.Throttle] = _throttleAmount;
            _vehicleController.data.bus[Channel.Input][InputData.Brake] = _brakeAmount;

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                _throttleAmount += 1000;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                _brakeAmount += 1000;
            }

            if (Input.GetKeyDown(KeyCode.Keypad0))
            {
                _throttleAmount = 0;
                _brakeAmount = 0;
            }
        }
    }
}
