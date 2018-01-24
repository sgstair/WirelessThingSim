using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimpleWirelessSimualator
{
    class WirelessUnitTestAttribute : Attribute
    {

    }

    class WirelessUnitTesting
    {
        public static WirelessUnitTests[] FindUnitTests()
        {
            List<WirelessUnitTests> outTests = new List<WirelessUnitTests>();
            Type[] nodeTypes = SimulatedNode.FindSimulatedNodeTypes();
            foreach (var node in nodeTypes)
            {
                List<WirelessUnitTest> tests = new List<WirelessUnitTest>();
                foreach (var method in node.GetMethods())
                {
                    WirelessUnitTestAttribute attribute = method.GetCustomAttribute<WirelessUnitTestAttribute>();
                    if (attribute != null)
                    {
                        // Todo: Verify that test is static, and accepts a single parameter taking WirelessUnitTestInstance.

                        tests.Add(new WirelessUnitTest() { UnitTestAttribute = attribute, UnitTestMethod = method, NodeType = node });
                    }
                }
                if (tests.Count > 0)
                {
                    outTests.Add(new WirelessUnitTests() { NodeType = node, UnitTestMethods = tests.ToArray() });
                }
            }
            return outTests.ToArray();
        }
    }

    class WirelessUnitTestInstance
    {
        public static WirelessUnitTestInstance RunUnitTest(WirelessNetwork net, WirelessUnitTest test)
        {
            WirelessUnitTestInstance instance = new WirelessUnitTestInstance(net);

            instance.TestPassed = false;
            try
            {
                test.UnitTestMethod.Invoke(null, new object[] { instance });
                instance.TestPassed = true;
            }
            catch(Exception ex)
            {
                instance.TestException = ex;
            }

            return instance;
        }

        private WirelessUnitTestInstance(WirelessNetwork net, int? seed = null)
        {
            if(seed == null)
            {
                RandomSeed = seedingRandom.Next();
            }
            else
            {
                RandomSeed = seed.Value;
            }
            r = new Random(RandomSeed);
            Network = net;
            Simulation = new WirelessNetworkSimulation(Network, r.Next());
            Simulation.StartSimulation();
        }
        static Random seedingRandom = new Random();
        Random r;
        public WirelessNetwork Network;
        public WirelessNetworkSimulation Simulation;

        public bool TestPassed = false;
        public Exception TestException = null;
        public int RandomSeed;

        public SimulatedNode GetRandomNode()
        {
            return Simulation.SimulationNodes[r.Next(Simulation.SimulationNodes.Count)].Node;
        }

        public void VerifyAllLedsChange(double afterTime, Color expectedColor, double timeTolerance = 0.005)
        {
            Simulation.SimulateTime(afterTime + timeTolerance);
            foreach(var node in Simulation.SimulationNodes)
            {
                bool foundLedChange = false;
                double earliestAllowedTime = node.Node.CurrentTime - timeTolerance * 2;
                foreach (var e in node.Node.PastEvents.Events.Reverse<SimulationEvent>())
                {
                    if (e.StartTime < earliestAllowedTime) break;
                    if(e.Type == EventType.LedChange)
                    {
                        if (expectedColor == (Color)e.EventContext)
                        {
                            foundLedChange = true;
                            break;
                        }
                    }
                }
                if(!foundLedChange)
                {
                    throw new Exception("Node did not change LED color as expected");
                }
            }
        }


    }

    class WirelessUnitTests
    {
        public Type NodeType;
        public WirelessUnitTest[] UnitTestMethods;
    }
    class WirelessUnitTest
    {
        public Type NodeType;
        public MethodInfo UnitTestMethod;
        public WirelessUnitTestAttribute UnitTestAttribute;
    }


}
