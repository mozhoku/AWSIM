using AWSIM.Scripts.Vehicles.VPP_Integration;
using AWSIM.Scripts.Vehicles.VPP_Integration.Enums;
using UnityEngine;

namespace AWSIM.Scripts.UI.Toggle
{
    public class UIKeyboardControlToggle : MonoBehaviour
    {
        private GameObject _egoVehicle;
        private AutowareVPPAdapter _adapter;

        private void Start()
        {
            _egoVehicle = GameObject.FindWithTag("Ego");
            _adapter = _egoVehicle.GetComponent<AutowareVPPAdapter>();

            // Set the toggle to the current state of the keyboard control mode
            GetComponent<UnityEngine.UI.Toggle>().isOn = _adapter.ControlModeInput != VPPControlMode.Autonomous;
        }

        public void Activate()
        {
            enabled = true;
            Start();
        }

        // Toggle the keyboard control
        public void OnClick(bool isOn)
        {
            _adapter.ControlModeInput = _adapter.ControlModeInput == VPPControlMode.Autonomous
                ? VPPControlMode.Manual
                : VPPControlMode.Autonomous;
        }
    }
}
