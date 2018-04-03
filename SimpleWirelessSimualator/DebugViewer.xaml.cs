using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// Interaction logic for DebugViewer.xaml
    /// </summary>
    public partial class DebugViewer : Window
    {
        public DebugViewer()
        {
            InitializeComponent();

            PreviewMouseWheel += DebugViewer_PreviewMouseWheel;
        }

        private void DebugViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double ZoomPower = 1.002;
            double zoom = Math.Pow(ZoomPower, e.Delta);

            if(RepresentativeDevice != null)
            {
                Point p = e.GetPosition(RepresentativeDevice);
                double x = p.X - RepresentativeDevice.DividerX;
                double w = RepresentativeDevice.ActualWidth - RepresentativeDevice.DividerX;

                double cursorTime = Timeline.StartTime + x * (Timeline.EndTime - Timeline.StartTime) / w;
                Timeline.StartTime = cursorTime + (Timeline.StartTime - cursorTime) * zoom;
                Timeline.EndTime = cursorTime + (Timeline.EndTime - cursorTime) * zoom;

                foreach (var item in deviceStack.Children)
                {
                    ((DebugDevice)item).InvalidateVisual();
                }

                e.Handled = true;
            }

        }

        internal void BindSimulation(WirelessNetworkSimulation sim)
        {
            Simulation = sim;
            GenerateSimulationUI();
        }

        WirelessNetworkSimulation Simulation;
        DebugTimeWindow Timeline;
        DebugDevice RepresentativeDevice;

        void GenerateSimulationUI()
        {
            deviceStack.Children.Clear();
            RepresentativeDevice = null;
            if(Simulation != null)
            {
                Timeline = new DebugTimeWindow() { StartTime = Simulation.SavedPreSimulationTime, EndTime = Simulation.CurrentTime };

                foreach (var device in Simulation.SimulationNodes)
                {
                    DebugDevice dev = new DebugDevice();
                    dev.BindNode(Timeline, device.Node);
                    RepresentativeDevice = dev;
                    deviceStack.Children.Add(dev);
                }
            }

        }



    }

    public class DebugTimeWindow
    {
        public double StartTime;
        public double EndTime;
    }
}
