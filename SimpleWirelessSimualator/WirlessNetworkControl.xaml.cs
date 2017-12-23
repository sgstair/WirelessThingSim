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

        public Point ScreenToLocal(Point screenPoint)
        {
            return screenPoint;
        }

        public Point LocalToScreen(Point localPoint)
        {
            return localPoint;
        }
        public double LocalToScreen(double localSize)
        {
            return localSize;
        }


        public void SetUserCursor(Point? p)
        {
            UserCursor = p;
            InvalidateVisual();
        }
        public void Redraw()
        {
            InvalidateVisual();
        }

        Point? UserCursor = null;

        protected override void OnRender(DrawingContext dc)
        {
            Rect windowSize = new Rect(0, 0, ActualWidth, ActualHeight);
            dc.PushClip(new RectangleGeometry(windowSize));
            dc.DrawRectangle(Brushes.White, null, windowSize);

            if (Network != null)
            {
                double range = LocalToScreen(Network.BaseTransmitRange);
                foreach (var node in Network.Nodes)
                {
                    Point pt = new Point(node.X, node.Y);
                    pt = LocalToScreen(pt);
                    dc.DrawEllipse(null, new Pen(Brushes.LightBlue, 1), pt, range, range);
                }
                foreach (var node in Network.Nodes)
                {
                    Point pt = new Point(node.X, node.Y);
                    pt = LocalToScreen(pt);
                    dc.DrawEllipse(Brushes.Black, null, pt, 10, 10);
                }


            }
            if(UserCursor != null)
            {
                dc.DrawEllipse(null, new Pen(Brushes.Green, 3), LocalToScreen(UserCursor.Value), 10, 10);
            }


            dc.Pop();
            base.OnRender(dc);
        }

    }
}
