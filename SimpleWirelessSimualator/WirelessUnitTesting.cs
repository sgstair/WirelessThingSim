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

    class WirelessReportAttribute : Attribute
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


    class WirelessReporting
    {
        public static WirelessReport[] FindReports()
        {
            List<WirelessReport> outReports = new List<WirelessReport>();
            Type[] nodeTypes = SimulatedNode.FindSimulatedNodeTypes();
            foreach (var node in nodeTypes)
            {
                foreach (var method in node.GetMethods())
                {
                    WirelessReportAttribute attribute = method.GetCustomAttribute<WirelessReportAttribute>();
                    if (attribute != null)
                    {
                        // Todo: Verify that test is static, and accepts a single parameter taking WirelessUnitTestInstance.

                        outReports.Add(new WirelessReport() { NodeType = node, ReportAttribute = attribute, ReportMethod = method });
                    }
                }
            }
            return outReports.ToArray();
        }
    }
    class WirelessUnitTestInstance
    {
        delegate void UnitTestDelegate(WirelessUnitTestInstance instance);
        delegate string ReportDelegate(WirelessUnitTestInstance instance);

        public string RunReport(WirelessReport report)
        {
            try
            {
                ReportDelegate d = (ReportDelegate)Delegate.CreateDelegate(typeof(ReportDelegate), report.ReportMethod);
                return d(this);
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public static WirelessUnitTestInstance RunUnitTest(WirelessNetwork net, WirelessUnitTest test)
        {
            WirelessUnitTestInstance instance = new WirelessUnitTestInstance(net);

            instance.TestPassed = false;
            try
            {
                UnitTestDelegate d = (UnitTestDelegate)Delegate.CreateDelegate(typeof(UnitTestDelegate), test.UnitTestMethod);
                d(instance);
                instance.TestPassed = true;
            }
            catch(Exception ex)
            {
                instance.TestException = ex;
            }

            return instance;
        }

        public static WirelessUnitTestInstance InstanceFromSimulation(WirelessNetworkSimulation simulation)
        {
            return new WirelessUnitTestInstance(simulation);
        }

        private WirelessUnitTestInstance(WirelessNetworkSimulation sim)
        {
            r = new Random();
            Simulation = sim;
            Network = sim.Network;
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

        public int NextRandom(int max)
        {
            return r.Next(max);
        }

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

        SimulationEvent FindNextEventWithCriteria(double exclusiveStart, Func<SimulationEvent, bool> filter)
        {
            SimulationEvent foundEvent = null;
            foreach(var node in Simulation.SimulationNodes)
            {
                foreach(var e in node.Node.PastEvents.Events)
                {
                    if (e.StartTime <= exclusiveStart) continue;
                    if (!filter(e)) continue;

                    // Found an event that matches the criteria
                    if(foundEvent == null || foundEvent.StartTime > e.StartTime)
                    {
                        foundEvent = e;
                    }
                    break;
                }
            }
            return foundEvent;
        }

        public SimulationEvent FindNextButtonPress(double exclusiveStartTime)
        {
            return FindNextEventWithCriteria(exclusiveStartTime, (ev) => ev.Type == EventType.ButtonChange);
        }

        public SimulationEvent FindFirstNodeEvent(SimulatedNode node, double exclusiveStartTime, double inclusiveEndTime, Func<SimulationEvent, bool> filter = null)
        {
            foreach (var e in node.PastEvents.Events)
            {
                if (e.StartTime <= exclusiveStartTime) continue;
                if (e.StartTime > inclusiveEndTime) break;

                if (!filter(e)) continue;

                // Found an event that matches the criteria
                return e;
            }
            return null;
        }

        public SimulationEvent FindFirstReceivedPacket(SimulatedNode node, double exclusiveStartTime, double inclusiveEndTime, Func<SimulationEvent, bool> filter = null)
        {
            // Note StartTime for PacketComplete event is the time the packet was received.
            return FindFirstNodeEvent(node, exclusiveStartTime, inclusiveEndTime, 
                (e) => e.Type == EventType.PacketComplete && ((WirelessPacketTransmission)e.EventContext).ReceiveSuccess && (filter == null || filter(e)));
        }
        public SimulationEvent FindFirstLedColor(SimulatedNode node, double exclusiveStartTime, double inclusiveEndTime, Func<Color, bool> filter = null)
        {
            return FindFirstNodeEvent(node, exclusiveStartTime, inclusiveEndTime, (e) => e.Type == EventType.LedChange && (filter == null || filter((Color)e.EventContext)));
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
    class WirelessReport
    {
        public Type NodeType;
        public MethodInfo ReportMethod;
        public WirelessReportAttribute ReportAttribute;
    }

}
