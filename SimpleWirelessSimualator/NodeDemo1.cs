using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// This demo device is a very simple program that turns its LED red briefly if it receives a message from another radio.
    /// It leaves the radio on all the time which is not very power efficient.
    /// </summary>
    [SimulatedNode]
    class NodeDemo1 : SimulatedNode, ISimulatedDevice
    {
        /// <summary>
        /// Called to start or reset a device
        /// </summary>
        public void DeviceStart()
        {
            RadioSetModeReceive();
        }

        /// <summary>
        /// Called when a packet is received by this device.
        /// </summary>
        public void ReceivePacket(object packet)
        {
            // Turn LED red on packet
            SetLedColor(Colors.Red);
            // After a second, turn it off again.
            SetTimerCallback(1, () => SetLedColor(Colors.Black));
        }

        /// <summary>
        /// The simulated device has N pushbuttons attached to it, this signals a change in one of them.
        /// </summary>
        public void InputEvent(int input, bool pressed)
        {
            if(pressed)
            {
                // On any button being pressed, send a packet to make other radios turn their LED on.
                RadioTransmitPacket(new object()); // Packet contents are not important in this example.
                // Turn this node's LED green
                SetLedColor(Colors.Green);
                // After a second, turn it off again.
                SetTimerCallback(1, () => SetLedColor(Colors.Black));
            }
        }

    }
}
