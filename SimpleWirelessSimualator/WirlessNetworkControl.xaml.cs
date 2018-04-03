using System;
using System.Collections.Generic;
using System.Globalization;
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
    public partial class WirelessNetworkControl : UserControl
    {
        public WirelessNetworkControl()
        {
            InitializeComponent();
            
        }

        public List<RealizedNetworkImage> Images = new List<RealizedNetworkImage>();

        WirelessNetwork Network;
        public void SetNetwork(WirelessNetwork wn)
        {
            Network = wn;
            Images.Clear();
            foreach(var img in Network.Images)
            {
                RealizedNetworkImage ri = new RealizedNetworkImage();
                ri.SourceImage = img;
                ri.FullFilename = img.Filename;
                ri.Bitmap = new BitmapImage(new Uri(ri.FullFilename));
                Images.Add(ri);
            }
            InvalidateVisual();
        }

        WirelessNetworkSimulation Simulation;
        Dictionary<WirelessNetworkNode, WirelessSimulationNode> SimulationNodes;
        internal void SetSimulation(WirelessNetworkSimulation sim, Dictionary<WirelessNetworkNode, WirelessSimulationNode> simNodes)
        {
            Simulation = sim;
            SimulationNodes = simNodes;
            Redraw();
        }

        internal void StopSimulation()
        {
            Simulation = null;
            SimulationNodes = null;
            Redraw();
        }

        const double MinZoom = 0.1;
        const double MaxZoom = 1000;

        Point ScreenTopLeft;
        double Zoom = 80; // Number of pixels in screen space for one pixel in content space

        public Point ScreenToLocal(Point screenPoint)
        {
            return ScreenTopLeft + (screenPoint - new Point()) / Zoom;
        }

        public Point LocalToScreen(Point localPoint)
        {
            return new Point() + (localPoint - ScreenTopLeft) * Zoom;
        }
        public double ScreenToLocal(double screenSize)
        {
            return screenSize / Zoom;
        }
        public double LocalToScreen(double localSize)
        {
            return localSize * Zoom;
        }

        public void DoScroll(Vector screenMovement)
        {
            DoScrollLocal(screenMovement / Zoom);
        }
        public void DoScrollLocal(Vector localMovement)
        {
            ScreenTopLeft -= localMovement;
            Redraw();
        }

        public void DoZoom(Point screenPoint, double amount)
        {
            Point localPt = ScreenToLocal(screenPoint);
            Zoom = Math.Min(Math.Max(Zoom * amount, MinZoom), MaxZoom);
            Point newLocal = ScreenToLocal(screenPoint);
            ScreenTopLeft -= (newLocal - localPt);
            Redraw();
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
            Typeface face = new Typeface("Calibri");
            Rect windowSize = new Rect(0, 0, ActualWidth, ActualHeight);
            dc.PushClip(new RectangleGeometry(windowSize));
            dc.DrawRectangle(Brushes.White, null, windowSize);

            foreach(var img in Images)
            {
                Point imgLoc = LocalToScreen(new Point(img.SourceImage.X, img.SourceImage.Y));
                double scale = LocalToScreen(img.SourceImage.Scale);
                dc.DrawImage(img.Bitmap, new Rect(imgLoc, imgLoc + (new Vector(img.Bitmap.Width, img.Bitmap.Height) * scale)));
            }

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

                if(SimulationNodes != null)
                {
                    foreach(var sn in Simulation.SimulationNodes)
                    {
                        var node = sn.NetworkNode;
                        Point pt = new Point(node.X, node.Y);
                        pt = LocalToScreen(pt);
                        Brush b = new SolidColorBrush(sn.Node.LedColor);
                        dc.DrawEllipse(b, null, pt, 8, 8);

                    }
                }
                int i = 0;
                foreach (var node in Network.Nodes)
                {
                    Point pt = new Point(node.X, node.Y);
                    pt = LocalToScreen(pt);
                    FormattedText f = new FormattedText(i.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, 16, Brushes.Black);
                    i++;
                    pt.X -= f.Width / 2;
                    pt.Y += 12;
                    dc.DrawText(f, pt);
                }
            }
            if(UserCursor != null)
            {
                dc.DrawEllipse(null, new Pen(Brushes.Green, 3), LocalToScreen(UserCursor.Value), 10, 10);
            }


            // Add a scale
            Point scaleOrigin = windowSize.BottomLeft + new Vector(15, -10);
            Pen scalePen = new Pen(Brushes.Black, 1);
            double scaleLength = Zoom;
            string scaleName = "1m";
            if(scaleLength < 20)
            {
                scaleLength *= 10;
                scaleName = "10m";
            }
            if(scaleLength > 500)
            {
                scaleLength /= 10;
                scaleName = "0.1m";
            }

            dc.DrawLine(scalePen, scaleOrigin, scaleOrigin + new Vector(scaleLength, 0));
            dc.DrawLine(scalePen, scaleOrigin+ new Vector(0, 5), scaleOrigin + new Vector(0, -5));
            dc.DrawLine(scalePen, scaleOrigin+ new Vector(scaleLength, 5), scaleOrigin + new Vector(scaleLength, -5));

            FormattedText ft = new FormattedText(scaleName, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, 22, Brushes.Black);
            Point textPt = scaleOrigin + new Vector(scaleLength / 2 - ft.Width / 2, -5 - ft.Height);
            dc.DrawText(ft, textPt);

            dc.Pop();
            base.OnRender(dc);
        }

    }


    public class RealizedNetworkImage
    {
        public WirelessNetworkImage SourceImage;
        public string FullFilename;
        public ImageSource Bitmap;
    }
}
