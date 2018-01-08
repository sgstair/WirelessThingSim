using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimpleWirelessSimualator
{
    class WirelessNetworkSimulation
    {
        Random r = new Random();
        public WirelessNetwork Network;

        public List<WirelessSimulationNode> SimulationNodes = new List<WirelessSimulationNode>();


        public event Action LedStateChanged;

        public WirelessNetworkSimulation(WirelessNetwork baseNetwork)
        {
            Network = baseNetwork;

            foreach(var n in Network.Nodes)
            {
                Type t = Assembly.GetExecutingAssembly().GetType(n.NodeType);

                SimulatedNode simNode = (SimulatedNode)t.GetConstructor(new Type[] { }).Invoke(new object[] { });
                simNode.ParentSimulation = this;

                WirelessSimulationNode sn = new WirelessSimulationNode()
                {
                    NetworkNode = n,
                    Node = simNode
                };

                SimulationNodes.Add(sn);
            }

        }


        public double NextRandom()
        {
            return r.NextDouble();
        }

        /// <summary>
        /// Start a simulation (turn on the nodes at random times before this point)
        /// </summary>
        /// <param name="preSimulationTime">How long before the simulation starts might the devices be turned on</param>
        public void StartSimulation(double preSimulationTime = 120)
        {
            PendingEvents = new SimulationEventQueue();

            // Insert node poweron events
            foreach(var n in SimulationNodes)
            {
                double onTime = NextRandom() * preSimulationTime;
                PendingEvents.Insert(new SimulationEvent(onTime, n.Node, EventType.NodePowerOn));
            }
            CurrentTime = 0;

            // Simulate presimulation time
            SimulateTime(preSimulationTime);
        }

        public void SimulateTime(double time)
        {
            double endTime = CurrentTime + time;

            while(PendingEvents.HasEvent && PendingEvents.FirstEventTime < endTime)
            {
                CurrentTime = PendingEvents.FirstEventTime;
                var e = PendingEvents.NextEvent();

                ISimulatedDevice device = e.Origin as ISimulatedDevice;

                e.Origin.PastEvents.Append(e);

                switch(e.Type)
                {
                    case EventType.NodePowerOn:
                        device.DeviceStart();
                        break;
                    case EventType.NodePowerOff:
                        // Future feature - nodes can be powered off and leave the network.
                        throw new NotImplementedException();
                        break;
                    case EventType.PacketComplete:
                        var pkt = e.EventContext as WirelessPacketTransmission;
                        if(pkt.ReceiveSuccess)
                        {
                            device.ReceivePacket(pkt.Packet.PacketContents);
                        }
                        break;
                    case EventType.TimerComplete:
                        e.Origin.TimerAction?.Invoke();
                        break;
                    default:
                        throw new NotSupportedException("Unsupported event type in queue");
                }


            }
            CurrentTime = endTime;
        }

        public double CurrentTime;
        SimulationEventQueue PendingEvents = new SimulationEventQueue();


        public void SetButtonState(WirelessSimulationNode n, bool state, int index = 0)
        {
            n.Node.PastEvents.Append(new SimulationEvent(CurrentTime, n.Node, EventType.ButtonChange, new ButtonEventContext() { Index = index, Pressed = state }));
            n.Node.ButtonState[index] = state;
            ((ISimulatedDevice)n.Node).InputEvent(index, state);
        }



        internal void NodeSetLed(SimulatedNode n, Color c)
        {
            n.LedColor = c;
            n.PastEvents.Append(new SimulationEvent(CurrentTime, n, EventType.LedChange, c));
            LedStateChanged?.Invoke();
        }
        
        internal void NodeSetTimer(SimulatedNode n, double time, Action callback)
        {
            if(n.TimerEvent != null)
            {
                PendingEvents.Remove(n.TimerEvent);
                n.TimerEvent = null;
            }
            var context = new TimerEventContext() { Callback = callback, Time = time };
            n.PastEvents.Append(new SimulationEvent(CurrentTime, n, EventType.TimerSet, context));
            if(callback != null && time >= 0)
            {
                n.TimerEvent = new SimulationEvent(CurrentTime + time, n, EventType.TimerComplete, context);
                PendingEvents.Insert(n.TimerEvent);
            }
        }

        internal void NodeSendPacket(SimulatedNode n, object packet, double preDelay)
        {

        }
    }
    class WirelessSimulationNode
    {
        public WirelessNetworkNode NetworkNode;
        public SimulatedNode Node;
    }

    class SimulationEventQueue
    {
        public List<SimulationEvent> Events = new List<SimulationEvent>();

        public void Insert(SimulationEvent e)
        {
            int i;
            for(i=0;i<Events.Count;i++)
            {
                if (Events[i].StartTime > e.StartTime) break;
            }
            Events.Insert(i, e);
        }

        public void Remove(SimulationEvent e)
        {
            Events.Remove(e);
        }

        public void Append(SimulationEvent e)
        {
            Events.Add(e);
        }

        public bool HasEvent { get { return Events.Count > 0; } }

        public double FirstEventTime { get { return Events[0].StartTime; } }

        public SimulationEvent NextEvent()
        {
            if (Events.Count == 0) return null;
            SimulationEvent e = Events[0];
            Events.RemoveAt(0);
            return e;
        }
    }

    enum EventType
    {
        NodePowerOn,
        NodePowerOff, // Future: node offline / failure
        Packet,
        PacketComplete,
        PowerState,
        TimerSet,
        TimerComplete,
        LedChange,
        ButtonChange
    }

    class SimulationEvent
    {
        public SimulationEvent(double start, SimulatedNode node, EventType t, object context = null)
        {
            StartTime = start;
            Origin = node;
            Type = t;
            EventContext = context;
        }
        public double StartTime;
        public double EndTime = double.MaxValue;
        public SimulatedNode Origin;
        public EventType Type;
        public object EventContext;
    }

    public class TimerEventContext
    {
        public Action Callback;
        public double Time;
    }

    public class ButtonEventContext
    {
        public int Index;
        public bool Pressed;
    }

    class WirelessPacket
    {
        public SimulatedNode Origin;
        public object PacketContents;
        public double StartTime, EndTime;
    }

    class WirelessPacketTransmission
    {
        public WirelessPacket Packet;
        public SimulatedNode Receiver;
        public double WirelessDelay; // Time it takes packet to move through the air
        public bool ReceiveSuccess = true; // Set to false whenever packet fails for some reason.
    }

}
