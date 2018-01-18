using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWirelessSimualator
{
    public class WirelessNetwork
    {
        public List<WirelessNetworkNode> Nodes = new List<WirelessNetworkNode>();
        public List<WirelessNetworkImage> Images = new List<WirelessNetworkImage>();
        public double BaseTransmitRange = 30;
    }

    public class WirelessNetworkNode
    {
        public double X, Y, Z;
        public string NodeType;
    }

    public class WirelessNetworkImage
    {
        public string Filename;
        public double X, Y;
        public double Scale;
    }


}
