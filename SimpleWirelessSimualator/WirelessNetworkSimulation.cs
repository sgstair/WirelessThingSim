using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWirelessSimualator
{
    class WirelessNetworkSimulation
    {
        public WirelessNetwork Network;

        public List<WirelessSimulationNode> SimulationNodes = new List<WirelessSimulationNode>();


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
            }

        }


        /// <summary>
        /// Start a simulation (turn on the nodes at random times before this point)
        /// </summary>
        /// <param name="preSimulationTime">How long before the simulation starts might the devices be turned on</param>
        public void StartSimulation(double preSimulationTime = 120)
        {

        }

        public void SimulateTime(double time)
        {

        }



    }
    class WirelessSimulationNode
    {
        public WirelessNetworkNode NetworkNode;
        public SimulatedNode Node;
    }
}
