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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitComboBox();
            SetupWirelessNetwork(new WirelessNetwork());
        }

        WirelessNetwork Network;

        void SetupWirelessNetwork(WirelessNetwork wn)
        {
            Network = wn;
            NetworkControl.SetNetwork(wn);
        }


        void InitComboBox()
        {
            comboBox.Items.Clear();

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

        void MoveHighlightNode(Point p)
        {

        }

        void DeleteMouseDown(Point p)
        {

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

        }

        void AddMouseUp(Point p)
        {

        }

        private void boxRange_TextChanged(object sender, TextChangedEventArgs e)
        {

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

        bool SimulationRunning;
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if(SimulationRunning)
            {
                btnStartStop.Content = "Start Simulation";

            }
            else
            {
                btnStartStop.Content = "Stop Simulation";
            }
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
