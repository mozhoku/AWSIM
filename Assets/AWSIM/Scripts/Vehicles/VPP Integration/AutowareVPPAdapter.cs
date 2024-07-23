using System;
using UnityEngine;
using VehiclePhysics;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public class AutowareVPPAdapter : MonoBehaviour
    {
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

        private void Awake()
        {
            // Initialize the control mode to Autonomous
            ControlModeInput = VPPControlMode.Autonomous;
        }

        private void Start()
        {
            _vehicleController = GetComponent<VPVehicleController>();
            _rigidbody = GetComponent<Rigidbody>();
            _pedalMap = GetComponent<VehiclePedalMapLoader>();
        }

        private void Update()
        {
            SwitchControlMode();
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
                    HandleHazardLights();
                    HandleTurnSignal();
                    HandleAcceleration();
                    HandleGear();
                    HandleSteer();
                    break;
                case VPPControlMode.AutonomousSteerOnly:
                    HandleHazardLights();
                    HandleTurnSignal();
                    HandleGear();
                    HandleSteer();
                    break;
                case VPPControlMode.AutonomousVelocityOnly:
                    HandleHazardLights();
                    HandleTurnSignal();
                    HandleAcceleration();
                    HandleGear();
                    break;
                case VPPControlMode.Manual:
                    break;
                case VPPControlMode.Disengaged:
                    break;
                case VPPControlMode.NotReady:
                    break;
                default:
                    Debug.LogWarning("Control mode is not recognized.");
                    break;
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
            // Store current values
            _currentSpeed = _vehicleController.speed;
            if (_isJerkDefinedInput)
            {
                float currentAcceleration = _vehicleController.localAcceleration.magnitude;
                _currentJerk = (currentAcceleration - _previousAcceleration) / Time.fixedDeltaTime;
                _previousAcceleration = currentAcceleration;
            }

            // TODO: for some reason "isAccelerationDefinedInput" is always false from the Autoware side, can't use it (mozzz)
            // set accel
            if (_accelerationInput > 0 || _velocityInput > 0)
            {
                var throttlePercent = _pedalMap.GetPedalPercent(_pedalMap.AccelMap, _pedalMap.AccelMapVertical,
                    _pedalMap.AccelMapHeaders, _accelerationInput, _currentSpeed);
                // Debug.Log("Throttle %: " + throttlePercent);
                throttlePercent = RemapValue(throttlePercent, 0f, 0.5f, 0, 5000);
                _vehicleController.data.bus[Channel.Input][InputData.Throttle] = (int)throttlePercent;
            }

            // set brake
            if (_accelerationInput < 0 || _velocityInput < 0)
            {
                var brakePercent = _pedalMap.GetPedalPercent(_pedalMap.BrakeMap, _pedalMap.BrakeMapVertical,
                    _pedalMap.BrakeMapHeaders, _accelerationInput, _currentSpeed);
                // Debug.Log("Brake %: " + brakePercent);
                brakePercent = RemapValue(brakePercent, -0f, 0.8f, 0, 8000);
                _vehicleController.data.bus[Channel.Input][InputData.Brake] = (int)brakePercent;
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

        // Method to switch control mode based on user input
        // TODO: fix clashing with turn indicators and VPP shift (mozzz)
        private void SwitchControlMode()
        {
            // if (Input.GetKey(KeyCode.LeftControl))
            // {
            //     if (Input.GetKeyDown(KeyCode.Alpha0))
            //         ControlModeInput = VPPControlMode.NoCommand;
            //     else if (Input.GetKeyDown(KeyCode.Alpha1))
            //         ControlModeInput = VPPControlMode.Autonomous;
            //     else if (Input.GetKeyDown(KeyCode.Alpha2))
            //         ControlModeInput = VPPControlMode.AutonomousSteerOnly;
            //     else if (Input.GetKeyDown(KeyCode.Alpha3))
            //         ControlModeInput = VPPControlMode.AutonomousVelocityOnly;
            //     else if (Input.GetKeyDown(KeyCode.Alpha4))
            //         ControlModeInput = VPPControlMode.Manual;
            //     else if (Input.GetKeyDown(KeyCode.Alpha5))
            //         ControlModeInput = VPPControlMode.Disengaged;
            //     else if (Input.GetKeyDown(KeyCode.Alpha6))
            //         ControlModeInput = VPPControlMode.NotReady;
            // }
        }

        private static int RemapValue(float value, float from1, float to1, int from2, int to2)
        {
            return (int)((value - from1) / (to1 - from1) * (to2 - from2) + from2);
        }
    }
}
