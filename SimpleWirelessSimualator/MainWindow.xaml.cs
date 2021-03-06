﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Xml.Serialization;

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
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            InitializeComponent();
            InitComboBox();
            InitUnitTestMenu();
            SetupWirelessNetwork(new WirelessNetwork());

            NetworkControl.MouseMove += NetworkControl_MouseMove;
            NetworkControl.MouseLeftButtonDown += NetworkControl_MouseLeftButtonDown;
            NetworkControl.MouseLeftButtonUp += NetworkControl_MouseLeftButtonUp;
            NetworkControl.MouseWheel += NetworkControl_MouseWheel;

            SimulationTimer = new Timer(SimulationTick);

            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;

            TryAutoload();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            
        }

        void TryAutoload()
        {
            // Try to load the autoload file. If it fails, just silently give up
            string autoload = Properties.Settings.Default.AutoloadFile;
            if(!string.IsNullOrEmpty(autoload))
            {
                try
                {
                    WirelessNetwork wn = LoadNetwork(autoload);

                    StopSimulation();
                    SetupWirelessNetwork(wn);
                }
                catch
                {

                }
            }
        }

        void SetAutoload(string filename)
        {
            Properties.Settings.Default.AutoloadFile = filename;
            Properties.Settings.Default.Save();
        }

        void InitUnitTestMenu()
        {
            mnuUnitTest.Items.Clear();
            MenuItem item;
            foreach (var unitTests in WirelessUnitTesting.FindUnitTests())
            {
                item = new MenuItem() { Header = $"For {unitTests.NodeType.Name}" };
                

                foreach(var test in unitTests.UnitTestMethods)
                {
                    MenuItem testItem = new MenuItem() { Header = test.UnitTestMethod.Name, DataContext = test };
                    testItem.Click += TestItem_Click;
                    item.Items.Add(testItem);
                }
                mnuUnitTest.Items.Add(item);
            }

            mnuUnitTest.Items.Add(new Separator());
            item = new MenuItem() { Header = "Run Unit Tests..." };
            item.Click += RunTests_Click;
            mnuUnitTest.Items.Add(item);

            mnuUnitTest.Items.Add(new Separator());

            foreach(var report in WirelessReporting.FindReports())
            {
                item = new MenuItem() { Header = $"{report.NodeType.Name}:{report.ReportMethod.Name}", DataContext = report };
                item.Click += ReportItem_Click;
                mnuUnitTest.Items.Add(item);
            }
        }

        private void ReportItem_Click(object sender, RoutedEventArgs e)
        {
            WirelessReport rpt = ((MenuItem)sender).DataContext as WirelessReport;
            if (rpt != null && Simulation != null)
            {
                WirelessUnitTestInstance instance = WirelessUnitTestInstance.InstanceFromSimulation(Simulation);
                string output = instance.RunReport(rpt);
                System.Diagnostics.Debug.WriteLine(output);
            }
        }

        private void RunTests_Click(object sender, RoutedEventArgs e)
        {
            RunUnitTests w = new RunUnitTests();
            w.LinkedWindow = this;
            w.Show();
        }

        private void TestItem_Click(object sender, RoutedEventArgs e)
        {
            WirelessUnitTest test = ((MenuItem)sender).DataContext as WirelessUnitTest;
            if (test != null)
            {
                WirelessUnitTestInstance instance = WirelessUnitTestInstance.RunUnitTest(Network, test);
                MessageBox.Show(instance.TestPassed ? "Passed!" : "Failed!");
            }
        }

        private void NetworkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double ZoomPower = 1.002;

            Point screen = e.GetPosition(NetworkControl);
            double zoom = Math.Pow(ZoomPower, e.Delta);

            NetworkControl.DoZoom(screen, zoom);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopSimulation();
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

        internal WirelessNetwork Network;
        WirelessNetworkSimulation Simulation;

        void SetupWirelessNetwork(WirelessNetwork wn)
        {
            Network = wn;
            boxRange.Text = Network.BaseTransmitRange.ToString();
            NetworkControl.SetNetwork(wn);
        }

        ComboBoxItem InteractItem;

        void InitComboBox()
        {
            comboBox.Items.Clear();

            comboBox.Items.Add(new ComboBoxItem()
            {
                Content = "Move",
                DataContext = new ActionContext() { MouseMove = MoveMouseMove, MouseDown = MoveMouseDown, MouseUp = MoveMouseUp }
            });

            comboBox.Items.Add(new ComboBoxItem()
            {
                Content = "Delete Node",
                DataContext = new ActionContext() { MouseMove = MoveHighlightNode, MouseDown = DeleteMouseDown, MouseUp = DeleteMouseUp }
            });


            InteractItem = (new ComboBoxItem()
            {
                Content = "Interact with Node",
                DataContext = new ActionContext() { MouseMove = MoveHighlightNode, MouseDown = InteractMouseDown, MouseUp = InteractMouseUp }
            });
            comboBox.Items.Add(InteractItem);

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
                double snapDistance = 20; // pixels
                snapDistance = Math.Pow(NetworkControl.ScreenToLocal(snapDistance), 2);

                if(minDistance < snapDistance)
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

        bool panning = false;
        Point lastMove;
        void MoveMouseDown(Point p)
        {
            if (SelectedNode == null)
            {
                panning = true;
                lastMove = p;
            }
        }
        void MoveMouseUp(Point p)
        {
            panning = false;
        }

        void MoveMouseMove(Point p)
        {
            MoveHighlightNode(p);

            if(panning)
            {
                NetworkControl.DoScrollLocal(p - lastMove);
                //lastMove = p;
            }
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
            if(SelectedNode != null)
            {
                Simulation.SetButtonState(SimulatorNodes[SelectedNode], true);
            }
        }
        void InteractMouseUp(Point p)
        {
            if (SelectedNode != null)
            {
                // Fodo: Currently button release only occurs if you keep the mouse over the node, fix this in the future.
                Simulation.SetButtonState(SimulatorNodes[SelectedNode], false);
            }
        }

        void AddMouseDown(Point p)
        {
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

        Dictionary<WirelessNetworkNode, WirelessSimulationNode> SimulatorNodes = new Dictionary<WirelessNetworkNode, WirelessSimulationNode>();
        bool SimulationRunning = false;
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if(SimulationRunning)
            {
                StopSimulation();
            }
            else
            {
                btnStartStop.Content = "Stop Simulation";

                comboBox.SelectedItem = InteractItem;
                comboBox.IsEnabled = false;

                Simulation = new WirelessNetworkSimulation(Network);
                Simulation.LedStateChanged += Simulation_LedStateChanged;
                SimulatorNodes.Clear();
                foreach(var n in Simulation.SimulationNodes)
                {
                    SimulatorNodes[n.NetworkNode] = n;
                }
                Simulation.StartSimulation();
                NetworkControl.SetSimulation(Simulation, SimulatorNodes);
                LastTick = DateTime.Now;
                SimulationTimer.Change(TickRateMs, TickRateMs);
            }
            SimulationRunning = !SimulationRunning;
        }

        private void Simulation_LedStateChanged()
        {
            // Need to update rendering for LEDs.
            NetworkControl.Redraw();
        }

        private void StopSimulation()
        {
            if (SimulationRunning)
            {
                btnStartStop.Content = "Start Simulation";
                SimulationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                Simulation = null;
                NetworkControl.StopSimulation();

                comboBox.IsEnabled = true;
            }
        }

        private void mnuLoad_Click(object sender, RoutedEventArgs e)
        {
            string filename = FileDialog.GetOpenFilename("Load Project...", "ws", "Wireless Simulator File");
            if(filename != null)
            {
                // Try to load it
                try
                {
                    WirelessNetwork wn = LoadNetwork(filename);

                    StopSimulation();
                    SetupWirelessNetwork(wn);
                    SetAutoload(filename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception while trying to Load file.\n" + ex.ToString());
                }
            }
        }

        private static WirelessNetwork LoadNetwork(string filename)
        {
            FileStream fs = File.OpenRead(filename);

            XmlSerializer xs = new XmlSerializer(typeof(WirelessNetwork));
            WirelessNetwork wn = (WirelessNetwork)xs.Deserialize(fs);

            fs.Close();
            return wn;
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            string filename = FileDialog.GetSaveFilename("Save Project...", "ws", "Wireless Simulator File");
            if (filename != null)
            {
                // Save current network to file.
                try
                {
                    FileStream fs = File.OpenWrite(filename);

                    XmlSerializer xs = new XmlSerializer(typeof(WirelessNetwork));
                    xs.Serialize(fs, Network);

                    fs.Close();
                    SetAutoload(filename);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Exception while trying to save file.\n" + ex.ToString());
                }

            }
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void mnuSetBackground_Click(object sender, RoutedEventArgs e)
        {
            string filename = FileDialog.GetOpenFilename("Open background image...", "png", "Image File");
            if(filename != null)
            {
                try
                {
                    BitmapImage img = new BitmapImage(new Uri(filename));

                    RealizedNetworkImage ri = new RealizedNetworkImage();
                    ri.Bitmap = img;
                    ri.FullFilename = filename;
                    ri.SourceImage = new WirelessNetworkImage();
                    ri.SourceImage.Scale = 1.0/39;
                    ri.SourceImage.Filename = filename;


                    NetworkControl.Images.Clear();
                    NetworkControl.Images.Add(ri);

                    Network.Images.Clear();
                    Network.Images.Add(ri.SourceImage);

                    NetworkControl.Redraw();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception while trying to load image.\n" + ex.ToString());
                }
            }
        }

        private void mnuRemoveBackground_Click(object sender, RoutedEventArgs e)
        {
            Network.Images.Clear();
            NetworkControl.Images.Clear();
            NetworkControl.Redraw();

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
