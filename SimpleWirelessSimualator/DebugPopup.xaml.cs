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
    /// Interaction logic for DebugPopup.xaml
    /// </summary>
    public partial class DebugPopup : UserControl
    {
        public DebugPopup()
        {
            InitializeComponent();

        }

        internal void SetElement(DebugDevice dev, DebugDevice.TimeWindow? w)
        {
            RenderDevice = dev;
            RenderWindow = w;
            InvalidateVisual();
        }

        DebugDevice RenderDevice;
        DebugDevice.TimeWindow? RenderWindow;

        protected override void OnRender(DrawingContext dc)
        {
            if(RenderWindow != null)
            {
                Rect eventRect = RenderDevice.GetOutline(RenderWindow.Value);
                eventRect.Inflate(2, 2);

                dc.DrawRectangle(null, new Pen(Brushes.Black, 4), eventRect);

            }
        }
    }
}
