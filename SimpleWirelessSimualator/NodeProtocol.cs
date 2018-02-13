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
            //RadioSetModeReceive();
            RadioSetModePolling(0.05, 0.5);
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
                SetLedColor(Colors.Red);
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
            SetTimerCallback(0.1, FinishActivate);
        }
        void FinishActivate()
        {
            WaitingForActivation = false;
            SetTimerCallback(1.9, () => SetLedColor(Colors.Black));
        }

        bool WaitingForActivation = false;
        int ActivatingId = -1;
        double ActivateAt;

        struct ProtocolPacket
        {
            public double TimeToActivation;
            public int SourceDeviceID;
        }



        // Add unit tests to verify NodeProtocol
        [WirelessUnitTest]
        public static void VerifyBehavior(WirelessUnitTestInstance instance)
        {
            var node = instance.GetRandomNode();
            instance.Simulation.SetButtonState(node, true);
            instance.Simulation.SimulateTime(0.5);
            instance.Simulation.SetButtonState(node, false);
            instance.VerifyAllLedsChange(0.5, Colors.Green);
            instance.VerifyAllLedsChange(2 - 0.005, Colors.Black);
        }


        class NodeTime
        {
            public bool Success;
            public double Time;
            public SimulatedNode Node;
        }

        [WirelessReport]
        public static string NodeResponseReport(WirelessUnitTestInstance instance)
        {
            List<string> strings = new List<string>();
            double startTime = 0;
            while (true)
            {
                var evt = instance.FindNextButtonPress(startTime);
                if (evt == null) break;

                if (strings.Count != 0) strings.Add("");
                startTime = evt.StartTime;
                strings.Add($"Button press on Node {evt.Origin.MyID} at {startTime}:");

                List<NodeTime> LedChange = new List<NodeTime>();
                List<NodeTime> FirstPacket = new List<NodeTime>();

                foreach(var node in instance.Simulation.SimulationNodes)
                {
                    evt = instance.FindFirstLedColor(node.Node, startTime, startTime + 1.5, (c) => c == Colors.Green);
                    if (evt == null) LedChange.Add(new NodeTime() { Success = false, Node = node.Node });
                    else LedChange.Add(new NodeTime() { Success = true, Time = evt.StartTime - startTime, Node = node.Node });

                    evt = instance.FindFirstReceivedPacket(node.Node, startTime, startTime + 1.5);
                    if (evt == null) FirstPacket.Add(new NodeTime() { Success = false, Node = node.Node });
                    else FirstPacket.Add(new NodeTime() { Success = true, Time = evt.StartTime - startTime, Node = node.Node });
                }

                LedChange.Sort((n1, n2)=>n1.Time.CompareTo(n2.Time));
                FirstPacket.Sort((n1, n2) => n1.Time.CompareTo(n2.Time));

                foreach (var nt in FirstPacket) strings.Add($"  Node {nt.Node.MyID}: Received at {nt.Time} ({nt.Success})");
                foreach (var nt in LedChange) strings.Add($"  Node {nt.Node.MyID}: Led Change at {nt.Time} ({nt.Success})");

            }
            return string.Join("\n", strings);
        }
    }
}
