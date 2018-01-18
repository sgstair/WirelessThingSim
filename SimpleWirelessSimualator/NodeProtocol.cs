using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// This protocol allows a relatively large mesh of devices to activate at the same time in response to an event on any node.
    /// </summary>
    [SimulatedNode]
    class NodeProtocol : SimulatedNode, ISimulatedDevice
    {
        const double PacketSpacing = 0.01;

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
            ProtocolPacket pkt = (ProtocolPacket)packet;
            if(WaitingForActivation)
            {
                // Do nothing- already aware of the impending activation
                // Future: Deal with the possibility of two nodes being activated independently
            }
            else
            {
                // Imperfect computation - try to improve.
                ActivateAt = CurrentTime + pkt.TimeToActivation;
                ActivatingId = pkt.SourceDeviceID;
                WaitingForActivation = true;
                double delay = ParentSimulation.NextRandom() * PacketSpacing;
                SetTimerCallback(delay, () => SendActivationPacket());
            }
        }

        /// <summary>
        /// The simulated device has N pushbuttons attached to it, this signals a change in one of them.
        /// </summary>
        public void InputEvent(int input, bool pressed)
        {
            if(pressed)
            {
                ActivateAt = CurrentTime + 1; // Activate in one second
                ActivatingId = MyID;
                WaitingForActivation = true;
                SendActivationPacket();
            }
        }

        void SendActivationPacket(double preDelay = 0)
        {
            ProtocolPacket pkt = new ProtocolPacket();
            pkt.SourceDeviceID = ActivatingId;
            pkt.TimeToActivation = ActivateAt - CurrentTime - preDelay;
            if (pkt.TimeToActivation > 0)
            {
                RadioTransmitPacket(pkt, preDelay);
            }

            // Decide whether to send more packets.
            double timetoEnd = ActivateAt - CurrentTime;
            if(timetoEnd < PacketSpacing)
            {
                SetTimerCallback(timetoEnd, () => Activate());
            }
            else
            {
                SetTimerCallback(PacketSpacing, () => SendActivationPacket());
            }
        }

        void Activate()
        {
            SetLedColor(Colors.Green);
            WaitingForActivation = false;
            SetTimerCallback(2, () => SetLedColor(Colors.Black));
        }

        bool WaitingForActivation = false;
        int ActivatingId = -1;
        double ActivateAt;

        struct ProtocolPacket
        {
            public double TimeToActivation;
            public int SourceDeviceID;
        }
    }
}
