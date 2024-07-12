namespace AWSIM.Scripts.Vehicles.VPP_Integration
{
    public static class Ros2ToVPPUtilities
    {
        public static int Ros2ToVPPShift(autoware_vehicle_msgs.msg.GearCommand gearCommand)
        {
            return gearCommand.Command switch
            {
                autoware_vehicle_msgs.msg.GearReport.NONE or autoware_vehicle_msgs.msg.GearReport.PARK => 1,
                autoware_vehicle_msgs.msg.GearReport.REVERSE => 2,
                autoware_vehicle_msgs.msg.GearReport.NEUTRAL => 3,
                autoware_vehicle_msgs.msg.GearReport.DRIVE or autoware_vehicle_msgs.msg.GearReport.LOW => 8,
                _ => 1
            };
        }

        public static byte VPPToRos2Shift(int shift)
        {
            return shift switch
            {
                1 => autoware_vehicle_msgs.msg.GearReport.PARK,
                2 => autoware_vehicle_msgs.msg.GearReport.REVERSE,
                3 => autoware_vehicle_msgs.msg.GearReport.NEUTRAL,
                4 or 5 or 6 or 7 or 8 => autoware_vehicle_msgs.msg.GearReport.DRIVE,
                _ => autoware_vehicle_msgs.msg.GearReport.PARK
            };
        }

        public static VPPTurnSignal Ros2ToVPPTurnSignal(
            autoware_vehicle_msgs.msg.TurnIndicatorsCommand turnIndicatorsCommand)
        {
            return turnIndicatorsCommand.Command switch
            {
                autoware_vehicle_msgs.msg.TurnIndicatorsCommand.DISABLE => VPPTurnSignal.NONE,
                autoware_vehicle_msgs.msg.TurnIndicatorsCommand.ENABLE_LEFT => VPPTurnSignal.LEFT,
                autoware_vehicle_msgs.msg.TurnIndicatorsCommand.ENABLE_RIGHT => VPPTurnSignal.RIGHT,
                _ => VPPTurnSignal.NONE
            };
        }

        public static byte VPPToRos2TurnSignal(VPPTurnSignal turnSignal)
        {
            return turnSignal switch
            {
                VPPTurnSignal.NONE => autoware_vehicle_msgs.msg.TurnIndicatorsReport.DISABLE,
                VPPTurnSignal.LEFT => autoware_vehicle_msgs.msg.TurnIndicatorsReport.ENABLE_LEFT,
                VPPTurnSignal.RIGHT => autoware_vehicle_msgs.msg.TurnIndicatorsReport.ENABLE_RIGHT,
                _ => autoware_vehicle_msgs.msg.TurnIndicatorsReport.DISABLE
            };
        }

        public static VPPTurnSignal Ros2ToVPPHazard(
            autoware_vehicle_msgs.msg.HazardLightsCommand hazardLightsCommand)
        {
            return hazardLightsCommand.Command switch
            {
                autoware_vehicle_msgs.msg.HazardLightsCommand.ENABLE => VPPTurnSignal.HAZARD,
                autoware_vehicle_msgs.msg.HazardLightsCommand.DISABLE => VPPTurnSignal.NONE,
                _ => VPPTurnSignal.NONE
            };
        }

        public static byte VPPToRos2Hazard(VPPTurnSignal turnSignal)
        {
            return turnSignal switch
            {
                VPPTurnSignal.HAZARD => autoware_vehicle_msgs.msg.HazardLightsReport.ENABLE,
                VPPTurnSignal.NONE => autoware_vehicle_msgs.msg.HazardLightsReport.DISABLE,
                _ => autoware_vehicle_msgs.msg.HazardLightsReport.DISABLE
            };
        }
    }
}
