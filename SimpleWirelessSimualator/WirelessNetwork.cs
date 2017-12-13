using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWirelessSimualator
{
    class WirelessNetwork
    {
        public List<WirelessNetworkNode> Nodes = new List<WirelessNetworkNode>();
        public double BaseTransmitRange = 30;
    }

    class WirelessNetworkNode
    {
        public double X, Y, Z;
        public string NodeType;
    }
}
