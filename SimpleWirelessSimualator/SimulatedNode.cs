using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// [SimulatedNode] is used to tag classes that are trying to simulate specific kinds of nodes
    /// Any classes tagged with [SimulatedNode] must inherit from SimulatedNode, and implement ISimulatedDevice
    /// </summary>
    public class SimulatedNodeAttribute : Attribute
    {

    }

    /// <summary>
    /// Base class for all kinds of simulated nodes.
    /// The only valid actions for simulated nodes to take are various kinds of computation, and calls to methods on this function.
    /// Simulated nodes must implement ISimulatedDevice
    /// </summary>
    class SimulatedNode
    {
        public WirelessNetworkSimulation ParentSimulation;
        public WirelessNetworkNode SourceNode;
        public double DeviceTimingSkew = 1; // Allow for variation in exact timings - Crystal used is ~50ppm, so not much of a variation.

        public SimulationEventQueue PastEvents = new SimulationEventQueue();
        public List<WirelessPacketTransmission> InFlightPackets = new List<WirelessPacketTransmission>(); // Packets that have been sent but not fully received yet.

        public Color LedColor;

        public Action TimerAction;
        public SimulationEvent TimerEvent;

        public int MyID;

        public Dictionary<int, bool> ButtonState = new Dictionary<int, bool>();

        public static Type[] FindSimulatedNodeTypes()
        {
            List<Type> returnData = new List<Type>();
            foreach(var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attribute = type.GetCustomAttribute<SimulatedNodeAttribute>();
                if(attribute != null)
                {
                    if(!type.GetInterfaces().Contains(typeof(ISimulatedDevice)))
                    {
                        System.Diagnostics.Debug.WriteLine($"Type {type.Name} excluded from simulated node types because it doesn't implement ISimulatedDevice");
                        continue;
                    }

                    returnData.Add(type);
                }
            }
            return returnData.ToArray();
        }

        // Provide API to control the radio and power states of the device

        /// <summary>
        /// Power-on time of this device in seconds
        /// </summary>
        public double CurrentTime { get { return ParentSimulation.CurrentTime; } }

        /// <summary>
        /// Set the radio into polling mode in a loop - Spending some time active listening for packets, and then some time powered down.
        /// </summary>
        public void RadioSetModePolling(double timeOn, double timeOff)
        {

        }

        /// <summary>
        /// Turn the radio on to receive constantly
        /// </summary>
        public void RadioSetModeReceive()
        {

        }

        /// <summary>
        /// Turn off the radio completely.
        /// </summary>
        public void RadioSetModeOff()
        {

        }

        /// <summary>
        /// Go into the radio's transmit mode temporarily (after receiving a packet if one is in progress), and transmit the specified packet.
        /// </summary>
        public void RadioTransmitPacket(object packet, double preDelay = 0)
        {
            ParentSimulation.NodeSendPacket(this, packet, preDelay);
        }

        /// <summary>
        /// Provide a callback after the specified amount of time (in seconds) has passed. Replaces any existing callback if called more than once.
        /// </summary>
        public void SetTimerCallback(double time, Action callback)
        {
            TimerAction = callback;
            ParentSimulation.NodeSetTimer(this, time, callback);
        }

        /// <summary>
        /// The node has a RGB led on it - use this to set the color
        /// </summary>
        public void SetLedColor(Color c)
        {
            ParentSimulation.NodeSetLed(this, c);
        }

        /// <summary>
        /// Get the current state of one of the N pushbuttons attached to this device.
        /// </summary>
        public bool GetInputValue(int input = 0)
        {
            bool value;
            if(ButtonState.TryGetValue(input, out value))
            {
                return value;
            }
            return false;
        }

        /// <summary>
        /// Clear any set timer callback
        /// </summary>
        public void CancelTimer()
        {
            SetTimerCallback(0, null);
        }
    }

    interface ISimulatedDevice
    {
        /// <summary>
        /// Called to start or reset a device
        /// </summary>
        void DeviceStart();

        /// <summary>
        /// Called when a packet is received by this device.
        /// </summary>
        void ReceivePacket(object packet);

        /// <summary>
        /// The simulated device has N pushbuttons attached to it, this signals a change in one of them.
        /// </summary>
        void InputEvent(int input, bool pressed);
    }


}
