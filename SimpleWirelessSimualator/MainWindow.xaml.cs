using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int TickRateMs = 50;
        Timer SimulationTimer; 

        public MainWindow()
        {
            InitializeComponent();
            InitComboBox();
            SetupWirelessNetwork(new WirelessNetwork());

            NetworkControl.MouseMove += NetworkControl_MouseMove;
            NetworkControl.MouseLeftButtonDown += NetworkControl_MouseLeftButtonDown;
            NetworkControl.MouseLeftButtonUp += NetworkControl_MouseLeftButtonUp;

            SimulationTimer = new Timer(SimulationTick);

        }

        DateTime LastTick = DateTime.Now;
        void SimulationTick(object context)
        {
            Dispatcher.Invoke(() =>
            {
                if(Simulation != null)
                {
                    DateTime now = DateTime.Now;

                    double simTime = now.Subtract(LastTick).TotalSeconds;
                    LastTick = now;

                    Simulation.SimulateTime(simTime);

                }
            });
        }

        private void NetworkControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(NetworkControl);
            p = NetworkControl.ScreenToLocal(p);

            ActionContext c = GetActionContext();
            c?.MouseUp?.Invoke(p);
        }

        private void NetworkControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(NetworkControl);
            p = NetworkControl.ScreenToLocal(p);

            ActionContext c = GetActionContext();
            c?.MouseDown?.Invoke(p);
        }

        private void NetworkControl_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(NetworkControl);
            p = NetworkControl.ScreenToLocal(p);

            ActionContext c = GetActionContext();
            c?.MouseMove?.Invoke(p);
        }

        ActionContext GetActionContext()
        {
            ComboBoxItem item = comboBox.SelectedItem as ComboBoxItem;
            ActionContext c = item?.DataContext as ActionContext;
            return c;
        }

        WirelessNetwork Network;
        WirelessNetworkSimulation Simulation;

        void SetupWirelessNetwork(WirelessNetwork wn)
        {
            Network = wn;
            boxRange.Text = Network.BaseTransmitRange.ToString();
            NetworkControl.SetNetwork(wn);
        }


        void InitComboBox()
        {
            comboBox.Items.Clear();

            comboBox.Items.Add(new ComboBoxItem()
            {
                Content = "Move",
                DataContext = new ActionContext() { MouseMove = MoveHighlightNode, MouseDown = MoveMouseDown, MouseUp = MoveMouseUp }
            });

            comboBox.Items.Add(new ComboBoxItem()
            {
                Content = "Delete Node",
                DataContext = new ActionContext() { MouseMove = MoveHighlightNode, MouseDown = DeleteMouseDown, MouseUp = DeleteMouseUp }
            });


            comboBox.Items.Add(new ComboBoxItem()
            {
                Content = "Interact with Node",
                DataContext = new ActionContext() { MouseMove = MoveHighlightNode, MouseDown = InteractMouseDown, MouseUp = InteractMouseUp }
            });

            comboBox.Items.Add(new Separator());

            // Add nodes
            ComboBoxItem firstItem = null;
            foreach(var type in SimulatedNode.FindSimulatedNodeTypes())
            {
                ComboBoxItem item = new ComboBoxItem()
                {
                    Content = $"Add Node: {type.Name}",
                    DataContext = new ActionContext() { MouseMove = MoveHighlightNode, MouseDown = AddMouseDown, MouseUp = AddMouseUp, AddNodeType = type }
                };
                if (firstItem == null) firstItem = item;
                comboBox.Items.Add(item);
            }

            // If there is a node, select the first node.
            if(firstItem != null)
            {
                comboBox.SelectedItem = firstItem;
            }
        }

        WirelessNetworkNode SelectedNode = null;

        void MoveHighlightNode(Point p)
        {
            if (Network != null)
            {
                WirelessNetworkNode selNode = null;
                Point selPoint = new Point();
                double minDistance = double.PositiveInfinity;
                foreach(var node in Network.Nodes)
                {
                    Point np = new Point(node.X, node.Y);
                    double dsquared = (p - np).LengthSquared;
                    if(dsquared < minDistance)
                    {
                        selNode = node;
                        selPoint = np;
                        minDistance = dsquared;
                    }
                }

                if(minDistance < (20*20))
                {
                    SelectedNode = selNode;
                    p = selPoint;
                }
                else
                {
                    SelectedNode = null;
                }
            }
            NetworkControl.SetUserCursor(p);
        }

        void MoveMouseDown(Point p)
        {

        }
        void MoveMouseUp(Point p)
        {

        }

        void DeleteMouseDown(Point p)
        {
            if(SelectedNode != null)
            {
                Network.Nodes.Remove(SelectedNode);
                NetworkControl.Redraw();
            }
        }
        void DeleteMouseUp(Point p)
        {

        }

        void InteractMouseDown(Point p)
        {

        }
        void InteractMouseUp(Point p)
        {

        }

        void AddMouseDown(Point p)
        {
            p = NetworkControl.ScreenToLocal(p);
            ActionContext c = GetActionContext();
            if(c?.AddNodeType != null)
            {
                // Verify that we are not adding too close to another node.
                if (SelectedNode != null) return;

                Network.Nodes.Add(new WirelessNetworkNode()
                {
                    NodeType = c.AddNodeType.FullName,
                    X = p.X,
                    Y = p.Y

                });

                NetworkControl.Redraw();
            }

        }

        void AddMouseUp(Point p)
        {

        }

        private void boxRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newRange;
            if(Network != null && double.TryParse(boxRange.Text, out newRange))
            {
                Network.BaseTransmitRange = newRange;
                NetworkControl.Redraw();
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 1)
            {
                ComboBoxItem newSelection = e.AddedItems[0] as ComboBoxItem;
                if(newSelection != null)
                {
                    // May not need to care if selection changes - Just get action from current selection
                }
            }
        }

        bool SimulationRunning = false;
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if(SimulationRunning)
            {
                btnStartStop.Content = "Start Simulation";
                SimulationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                Simulation = null;

            }
            else
            {
                btnStartStop.Content = "Stop Simulation";
                Simulation = new WirelessNetworkSimulation(Network);
                Simulation.StartSimulation();
                LastTick = DateTime.Now;
                SimulationTimer.Change(TickRateMs, TickRateMs);
            }
            SimulationRunning = !SimulationRunning;
        }
    }
    class ActionContext
    {
        public delegate void MouseAction(Point location);
        public MouseAction MouseMove;
        public MouseAction MouseDown;
        public MouseAction MouseUp;
        public Type AddNodeType;
    }
    

}
