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
    /// Interaction logic for DebugDevice.xaml
    /// </summary>
    public partial class DebugDevice : UserControl
    {
        public DebugDevice()
        {
            InitializeComponent();
        }

        internal void BindNode(DebugTimeWindow timeline, SimulatedNode node)
        {
            Timeline = timeline;
            Node = node;
            InvalidateVisual();
        }

        public double DividerX = 100;

        DebugTimeWindow Timeline;
        SimulatedNode Node;

        Typeface Face = new Typeface("Calibri");

        protected override void OnRender(DrawingContext dc)
        {
            if (Node != null)
            {

                FormattedText ft = new FormattedText(Node.MyID.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, 20, Brushes.Black);
                dc.DrawText(ft, new Point(5, 5));

                dc.DrawLine(new Pen(Brushes.Black, 2), new Point(DividerX, 0), new Point(DividerX, 40));

                // Draw the events that occur in the timeline in this time window.

                DrawTimeline(5, 30, dc,
                    Node.PastEvents.Events.Where(e => e.Type == EventType.PacketComplete && ((WirelessPacketTransmission)e.EventContext).ReceiveSuccess == true).Select(e => TranslatePacketComplete(e)), 
                    Brushes.LimeGreen);

                DrawTimeline(5, 30, dc,
                    Node.PastEvents.Events.Where(e => e.Type == EventType.PacketComplete && ((WirelessPacketTransmission)e.EventContext).Collision == true).Select(e => TranslatePacketComplete(e)),
                    Brushes.Red);

                DrawTimeline(5, 30, dc,
                    Node.PastEvents.Events.Where(e => e.Type == EventType.Packet).Select(e => TranslateEvent(e)),
                    Brushes.Blue);

            }
            //base.OnRender(dc);
        }

        TimeWindow TranslatePacketComplete(SimulationEvent e)
        {
            var wpt = (WirelessPacketTransmission)e.EventContext;
            double delay = wpt.WirelessDelay;
            return new TimeWindow() { Start = wpt.Packet.StartTime + delay, End = wpt.Packet.EndTime + delay };
        }

        TimeWindow TranslateEvent(SimulationEvent e)
        {
            return new TimeWindow() { Start = e.StartTime, End = e.EndTime };
        }

        void DrawTimeline(double y, double height, DrawingContext dc, IEnumerable<TimeWindow> events, Brush color)
        {
            double startTime = Timeline.StartTime;
            double endTime = Timeline.EndTime;
            double timeWidth = endTime - startTime;
            if (timeWidth <= 0) timeWidth = 0;
            double screenWidth = ActualWidth-DividerX;
            foreach (var e in events)
            {
                double x1 = (e.Start - startTime) * screenWidth / timeWidth;
                double x2 = (e.End - startTime) * screenWidth / timeWidth;

                if (x2 < 0 || x1 > screenWidth) continue;

                dc.DrawRectangle(color, null, new Rect(x1 + DividerX, y, x2 - x1, height));
            }
        }

        struct TimeWindow
        {
            public double Start, End;
        }
    }
}
