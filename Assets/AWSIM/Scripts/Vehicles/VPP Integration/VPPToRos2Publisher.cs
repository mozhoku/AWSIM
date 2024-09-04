using System.Collections;
using ROS2;
using UnityEngine;

namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public class VPPToRos2Publisher : MonoBehaviour
    {
        [Header("AutowareVPPAdapter")]
        [SerializeField]
        private AutowareVPPAdapter _adapter;

        [Header("Ros2 fields")]
        [SerializeField]
        private string _controlModeReportTopic = "/vehicle/status/control_mode";

        [SerializeField] private string _gearReportTopic = "/vehicle/status/gear_status";
        [SerializeField] private string _steeringReportTopic = "/vehicle/status/steering_status";
        [SerializeField] private string _turnIndicatorsReportTopic = "/vehicle/status/turn_indicators_status";
        [SerializeField] private string _hazardLightsReportTopic = "/vehicle/status/hazard_lights_status";
        [SerializeField] private string _velocityReportTopic = "/vehicle/status/velocity_status";
        [SerializeField] private string _ActuationStatusTopic = "/vehicle/status/actuation_status";
        [SerializeField] private string _frameId = "base_link";

        [Range(1, 60)][SerializeField] private int _publishHz = 30;
        [SerializeField] private QoSSettings _qosSettings;

        // messages
        private autoware_vehicle_msgs.msg.ControlModeReport _controlModeReportMsg;
        private autoware_vehicle_msgs.msg.GearReport _gearReportMsg;
        private autoware_vehicle_msgs.msg.SteeringReport _steeringReportMsg;
        private autoware_vehicle_msgs.msg.TurnIndicatorsReport _turnIndicatorsReportMsg;
        private autoware_vehicle_msgs.msg.HazardLightsReport _hazardLightsReportMsg;
        private autoware_vehicle_msgs.msg.VelocityReport _velocityReportMsg;
        private tier4_vehicle_msgs.msg.ActuationStatusStamped _actuationStatusReport;

        // publisher
        private IPublisher<autoware_vehicle_msgs.msg.ControlModeReport> _controlModeReportPublisher;
        private IPublisher<autoware_vehicle_msgs.msg.GearReport> _gearReportPublisher;
        private IPublisher<autoware_vehicle_msgs.msg.SteeringReport> _steeringReportPublisher;
        private IPublisher<autoware_vehicle_msgs.msg.TurnIndicatorsReport> _turnIndicatorsReportPublisher;
        private IPublisher<autoware_vehicle_msgs.msg.HazardLightsReport> _hazardLightsReportPublisher;
        private IPublisher<autoware_vehicle_msgs.msg.VelocityReport> _velocityReportPublisher;
        private IPublisher<tier4_vehicle_msgs.msg.ActuationStatusStamped> _actuationStatusPublisher;

        private bool _isInitialized;

        private void Start()
        {
            InitializePublishers();
            InitializeMessages();
            _isInitialized = true;
            if (_isInitialized)
            {
                StartCoroutine(PublishRoutine());
            }
        }

        private IEnumerator PublishRoutine()
        {
            var interval = 1.0f / _publishHz;
            interval -= 0.00001f; // Allow for accuracy errors.
            while (true)
            {
                UpdateMessages();
                PublishMessages();
                yield return new WaitForSeconds(interval);
            }
        }

        private void InitializePublishers()
        {
            var qos = _qosSettings.GetQoSProfile();
            _controlModeReportPublisher =
                SimulatorROS2Node.CreatePublisher<autoware_vehicle_msgs.msg.ControlModeReport>(_controlModeReportTopic,
                    qos);
            _gearReportPublisher =
                SimulatorROS2Node.CreatePublisher<autoware_vehicle_msgs.msg.GearReport>(_gearReportTopic, qos);
            _steeringReportPublisher =
                SimulatorROS2Node.CreatePublisher<autoware_vehicle_msgs.msg.SteeringReport>(_steeringReportTopic, qos);
            _turnIndicatorsReportPublisher =
                SimulatorROS2Node.CreatePublisher<autoware_vehicle_msgs.msg.TurnIndicatorsReport>(
                    _turnIndicatorsReportTopic, qos);
            _hazardLightsReportPublisher =
                SimulatorROS2Node.CreatePublisher<autoware_vehicle_msgs.msg.HazardLightsReport>(
                    _hazardLightsReportTopic, qos);
            _velocityReportPublisher =
                SimulatorROS2Node.CreatePublisher<autoware_vehicle_msgs.msg.VelocityReport>(_velocityReportTopic, qos);
            _actuationStatusPublisher =
                SimulatorROS2Node.CreatePublisher<tier4_vehicle_msgs.msg.ActuationStatusStamped>(_ActuationStatusTopic,
                    qos);
        }

        private void InitializeMessages()
        {
            _controlModeReportMsg = new autoware_vehicle_msgs.msg.ControlModeReport();
            _gearReportMsg = new autoware_vehicle_msgs.msg.GearReport();
            _steeringReportMsg = new autoware_vehicle_msgs.msg.SteeringReport();
            _turnIndicatorsReportMsg = new autoware_vehicle_msgs.msg.TurnIndicatorsReport();
            _hazardLightsReportMsg = new autoware_vehicle_msgs.msg.HazardLightsReport();
            _velocityReportMsg = new autoware_vehicle_msgs.msg.VelocityReport
            {
                Header = new std_msgs.msg.Header
                {
                    Frame_id = _frameId,
                }
            };
            _actuationStatusReport = new tier4_vehicle_msgs.msg.ActuationStatusStamped
            {
                Header = new std_msgs.msg.Header()
                {
                    Frame_id = _frameId,
                }
            };
        }

        private void UpdateMessages()
        {
            // ControlModeReport
            _controlModeReportMsg.Mode = Ros2ToVPPUtilities.VPPToRos2ControlMode(_adapter.VpControlModeReport);

            // GearReport
            _gearReportMsg.Report = Ros2ToVPPUtilities.VPPToRos2Shift(_adapter.VpGearReport);

            // SteeringReport
            _steeringReportMsg.Steering_tire_angle = -1 * _adapter.VpSteeringReport * Mathf.Deg2Rad;

            // TurnIndicatorsReport
            _turnIndicatorsReportMsg.Report = Ros2ToVPPUtilities.VPPToRos2TurnSignal(_adapter.VehicleSignalInput);

            // HazardLightsReport
            _hazardLightsReportMsg.Report = Ros2ToVPPUtilities.VPPToRos2Hazard(_adapter.VehicleSignalInput);

            // VelocityReport
            var rosLinearVelocity = ROS2Utility.UnityToRosPosition(_adapter.VpVelocityReport);
            var rosAngularVelocity = ROS2Utility.UnityToRosPosition(_adapter.VpAngularVelocityReport);
            _velocityReportMsg.Longitudinal_velocity = rosLinearVelocity.x;
            _velocityReportMsg.Lateral_velocity = rosLinearVelocity.y;
            _velocityReportMsg.Heading_rate = rosAngularVelocity.z;

            // ActuationStatusReport
            _actuationStatusReport.Status.Accel_status = _adapter.VpThrottleStatusReport;
            _actuationStatusReport.Status.Brake_status = _adapter.VpBrakeStatusReport;
            _actuationStatusReport.Status.Steer_status = _adapter.VpSteerStatusReport;

            // Update timestamps
            var time = SimulatorROS2Node.GetCurrentRosTime();
            _controlModeReportMsg.Stamp = time;
            _gearReportMsg.Stamp = time;
            _steeringReportMsg.Stamp = time;
            _turnIndicatorsReportMsg.Stamp = time;
            _hazardLightsReportMsg.Stamp = time;

            MessageWithHeader velocityReportMsgHeader = _velocityReportMsg;
            SimulatorROS2Node.UpdateROSTimestamp(ref velocityReportMsgHeader);
            MessageWithHeader actuationStatusReportHeader = _actuationStatusReport;
            SimulatorROS2Node.UpdateROSTimestamp(ref actuationStatusReportHeader);
        }

        private void PublishMessages()
        {
            _controlModeReportPublisher.Publish(_controlModeReportMsg);
            _gearReportPublisher.Publish(_gearReportMsg);
            _steeringReportPublisher.Publish(_steeringReportMsg);
            _turnIndicatorsReportPublisher.Publish(_turnIndicatorsReportMsg);
            _hazardLightsReportPublisher.Publish(_hazardLightsReportMsg);
            _velocityReportPublisher.Publish(_velocityReportMsg);
            _actuationStatusPublisher.Publish(_actuationStatusReport);
        }

        private void OnDestroy()
        {
            StopCoroutine(PublishRoutine());

            SimulatorROS2Node.RemovePublisher<autoware_vehicle_msgs.msg.ControlModeReport>(_controlModeReportPublisher);
            SimulatorROS2Node.RemovePublisher<autoware_vehicle_msgs.msg.GearReport>(_gearReportPublisher);
            SimulatorROS2Node.RemovePublisher<autoware_vehicle_msgs.msg.SteeringReport>(_steeringReportPublisher);
            SimulatorROS2Node.RemovePublisher<autoware_vehicle_msgs.msg.TurnIndicatorsReport>(
                _turnIndicatorsReportPublisher);
            SimulatorROS2Node.RemovePublisher<autoware_vehicle_msgs.msg.HazardLightsReport>(
                _hazardLightsReportPublisher);
            SimulatorROS2Node.RemovePublisher<autoware_vehicle_msgs.msg.VelocityReport>(_velocityReportPublisher);
            SimulatorROS2Node.RemovePublisher<tier4_vehicle_msgs.msg.ActuationStatusStamped>(_actuationStatusPublisher);
        }
    }
}
