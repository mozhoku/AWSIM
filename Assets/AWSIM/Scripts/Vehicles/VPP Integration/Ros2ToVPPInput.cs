using ROS2;
using UnityEngine;
using UnityEngine.Serialization;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    [RequireComponent(typeof(AutowareVPPAdapter))]
    public class Ros2ToVPPInput : MonoBehaviour
    {
        // topics
        [SerializeField] private string turnIndicatorsCommandTopic = "/control/command/turn_indicators_cmd";
        [SerializeField] private string hazardLightsCommandTopic = "/control/command/hazard_lights_cmd";

        [FormerlySerializedAs("ackermannControlCommandTopic")] [SerializeField]
        private string controlCommandTopic = "/control/command/control_cmd";

        [SerializeField] private string gearCommandTopic = "/control/command/gear_cmd";
        [SerializeField] private string vehicleEmergencyStampedTopic = "/control/command/emergency_cmd";
        [SerializeField] private string positionTopic = "/initialpose";

        [SerializeField] private QoSSettings qosSettings = new QoSSettings();
        [SerializeField] private AutowareVPPAdapter adapter;
        [SerializeField] private QoSSettings positionQosInput;

        // subscribers.
        private ISubscription<autoware_vehicle_msgs.msg.TurnIndicatorsCommand> _turnIndicatorsCommandSubscriber;
        private ISubscription<autoware_vehicle_msgs.msg.HazardLightsCommand> _hazardLightsCommandSubscriber;
        private ISubscription<autoware_control_msgs.msg.Control> _controlCommandSubscriber;
        private ISubscription<autoware_vehicle_msgs.msg.GearCommand> _gearCommandSubscriber;
        private ISubscription<tier4_vehicle_msgs.msg.VehicleEmergencyStamped> _vehicleEmergencyStampedSubscriber;
        private ISubscription<geometry_msgs.msg.PoseWithCovarianceStamped> _positionSubscriber;

        VPPTurnSignal turnIndicatorsSignal = VPPTurnSignal.NONE;
        VPPTurnSignal hazardLightsSignal = VPPTurnSignal.NONE;
        VPPTurnSignal input = VPPTurnSignal.NONE;

        private bool _isEmergency;

        private void Reset()
        {
            if (adapter == null)
                adapter = GetComponent<AutowareVPPAdapter>();

            // initialize default QoS params.
            qosSettings.ReliabilityPolicy = ReliabilityPolicy.QOS_POLICY_RELIABILITY_RELIABLE;
            qosSettings.DurabilityPolicy = DurabilityPolicy.QOS_POLICY_DURABILITY_TRANSIENT_LOCAL;
            qosSettings.HistoryPolicy = HistoryPolicy.QOS_POLICY_HISTORY_KEEP_LAST;
            qosSettings.Depth = 1;
        }

        /// <summary>
        /// Processes the TurnSignal to be applied to the vehicle from the latest turnIndicatorsSignal and hazardLightsSignal values.
        /// Priority : HAZARD > LEFT/RIGHT > NONE
        /// </summary>
        private void UpdateVehicleTurnSignal()
        {
            // HAZARD > LEFT, RIGHT > NONE
            if (hazardLightsSignal == VPPTurnSignal.HAZARD)
                input = hazardLightsSignal;
            else if (turnIndicatorsSignal is VPPTurnSignal.LEFT or VPPTurnSignal.RIGHT)
                input = turnIndicatorsSignal;
            else
                input = VPPTurnSignal.NONE;

            // input
            if (adapter.SignalInput != input)
                adapter.SignalInput = input;
        }

        private void Start()
        {
            var qos = qosSettings.GetQoSProfile();
            var positionQoS = positionQosInput.GetQoSProfile();

            _turnIndicatorsCommandSubscriber =
                SimulatorROS2Node.CreateSubscription<autoware_vehicle_msgs.msg.TurnIndicatorsCommand>(
                    turnIndicatorsCommandTopic, msg =>
                    {
                        turnIndicatorsSignal = Ros2ToVPPUtilities.Ros2ToVPPTurnSignal(msg);
                        UpdateVehicleTurnSignal();
                    }, qos);

            _hazardLightsCommandSubscriber =
                SimulatorROS2Node.CreateSubscription<autoware_vehicle_msgs.msg.HazardLightsCommand>(
                    hazardLightsCommandTopic, msg =>
                    {
                        hazardLightsSignal = Ros2ToVPPUtilities.Ros2ToVPPHazard(msg);
                        UpdateVehicleTurnSignal();
                    }, qos);

            _controlCommandSubscriber = SimulatorROS2Node.CreateSubscription<autoware_control_msgs.msg.Control>(
                controlCommandTopic, msg =>
                {
                    // highest priority is EMERGENCY.
                    // If Emergency is true, ControlCommand is not used for vehicle acceleration input.
                    if (_isEmergency) return;

                    // longitudinal
                    adapter.VelocityInput = msg.Longitudinal.Velocity;
                    adapter.IsAccelerationDefinedInput = msg.Longitudinal.Is_defined_acceleration;
                    adapter.AccelerationInput = msg.Longitudinal.Acceleration;
                    adapter.IsJerkDefinedInput = msg.Longitudinal.Is_defined_jerk;
                    adapter.JerkInput = msg.Longitudinal.Jerk;

                    // lateral
                    adapter.SteerAngleInput = -(float)msg.Lateral.Steering_tire_angle * Mathf.Rad2Deg;
                    adapter.IsDefinedSteeringTireRotationRateInput = msg.Lateral.Is_defined_steering_tire_rotation_rate;
                    adapter.SteeringTireRotationRateInput = msg.Lateral.Steering_tire_rotation_rate;
                }, qos);

            _gearCommandSubscriber = SimulatorROS2Node.CreateSubscription<autoware_vehicle_msgs.msg.GearCommand>(
                gearCommandTopic,
                msg => { adapter.AutomaticShiftInput = Ros2ToVPPUtilities.Ros2ToVPPShift(msg); }, qos);

            _vehicleEmergencyStampedSubscriber =
                SimulatorROS2Node.CreateSubscription<tier4_vehicle_msgs.msg.VehicleEmergencyStamped>(
                    vehicleEmergencyStampedTopic, msg =>
                    {
                        // highest priority is EMERGENCY.
                        // If emergency is true, emergencyDeceleration is applied to the vehicle's deceleration.
                        _isEmergency = msg.Emergency;
                        // if (_isEmergency)
                        // adapter.AccelerationInput = emergencyDeceleration;
                    });
            _positionSubscriber = SimulatorROS2Node.CreateSubscription<geometry_msgs.msg.PoseWithCovarianceStamped>(
                positionTopic, msg =>
                {
                    var positionVector = new Vector3((float)msg.Pose.Pose.Position.X,
                        (float)msg.Pose.Pose.Position.Y,
                        (float)msg.Pose.Pose.Position.Z);

                    var rotationVector = new Quaternion((float)msg.Pose.Pose.Orientation.X,
                        (float)msg.Pose.Pose.Orientation.Y,
                        (float)msg.Pose.Pose.Orientation.Z,
                        (float)msg.Pose.Pose.Orientation.W);

                    adapter.PositionInput =
                        ROS2Utility.RosToUnityPosition(positionVector - Environment.Instance.MgrsOffsetPosition);
                    adapter.RotationInput = ROS2Utility.RosToUnityRotation(rotationVector);
                    adapter.WillUpdatePositionInput = true;
                }, positionQoS);
        }

        private void OnDestroy()
        {
            SimulatorROS2Node.RemoveSubscription<autoware_vehicle_msgs.msg.TurnIndicatorsCommand>(
                _turnIndicatorsCommandSubscriber);
            SimulatorROS2Node.RemoveSubscription<autoware_vehicle_msgs.msg.HazardLightsCommand>(
                _hazardLightsCommandSubscriber);
            SimulatorROS2Node.RemoveSubscription<autoware_control_msgs.msg.Control>(_controlCommandSubscriber);
            SimulatorROS2Node.RemoveSubscription<autoware_vehicle_msgs.msg.GearCommand>(_gearCommandSubscriber);
            SimulatorROS2Node.RemoveSubscription<tier4_vehicle_msgs.msg.VehicleEmergencyStamped>(
                _vehicleEmergencyStampedSubscriber);
            SimulatorROS2Node.RemoveSubscription<geometry_msgs.msg.PoseWithCovarianceStamped>(_positionSubscriber);
        }
    }
}
