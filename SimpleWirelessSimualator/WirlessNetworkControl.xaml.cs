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
    /// Interaction logic for WirlessNetworkControl.xaml
    /// </summary>
    public partial class WirlessNetworkControl : UserControl
    {
        public WirlessNetworkControl()
        {
            InitializeComponent();
            
        }

        WirelessNetwork Network;
        public void SetNetwork(WirelessNetwork wn)
        {
            Network = wn;
            InvalidateVisual();
        }


        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.Black, null, new Rect(100, 100, 300, 300));

            base.OnRender(dc);
        }

    }
}
