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

            MouseMove += DebugDevice_MouseMove;
            MouseLeave += DebugDevice_MouseLeave;
        }

        private void DebugDevice_MouseLeave(object sender, MouseEventArgs e)
        {
            if (FocusEvent.HasValue)
            {
                FocusEvent = null;
                FocusEventChange?.Invoke(this);
            }
        }

        private void DebugDevice_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(this);
            TimeWindow? w = FindTimelineEvent(p);
            if(w != FocusEvent)
            {
                FocusEvent = w;
                FocusEventChange?.Invoke(this);
            }
        }

        internal TimeWindow? FocusEvent;

        internal delegate void EventFocusChangeDelegate(DebugDevice dev);
        internal event EventFocusChangeDelegate FocusEventChange;


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

                dc.PushClip(new RectangleGeometry(new Rect(DividerX, 0, ActualWidth - DividerX, ActualHeight)));

                DrawTimeline(1, 38, dc, ComputeReceiveRegions(), Brushes.LightGray);

                DrawTimeline(5, 15, dc,
                    Node.PastEvents.Events.Where(e => e.Type == EventType.PacketComplete && ((WirelessPacketTransmission)e.EventContext).ReceiveSuccess == true).Select(e => TranslatePacketComplete(e)), 
                    Brushes.LimeGreen);

                DrawTimeline(20, 15, dc,
                    Node.PastEvents.Events.Where(e => e.Type == EventType.PacketComplete && ((WirelessPacketTransmission)e.EventContext).Collision == true).Select(e => TranslatePacketComplete(e)),
                    Brushes.Red);

                DrawTimeline(20, 15, dc,
                    Node.PastEvents.Events.Where(e => e.Type == EventType.Packet).Select(e => TranslateEvent(e)),
                    Brushes.Blue);

                dc.Pop();
            }
            //base.OnRender(dc);
        }

        IEnumerable<TimeWindow> ComputeReceiveRegions()
        {
            SimulationEvent[] terminatingEvent = new SimulationEvent[] { new SimulationEvent(Node.ParentSimulation.CurrentTime, Node, EventType.PowerState, false) };

            SimulationEvent previousEvent = null;
            object previousState = null;
            double prevTime = 0;
            foreach(var e in Node.PastEvents.Events.Where(e => e.Type == EventType.PowerState).Concat(terminatingEvent))
            {
                double curTime = e.StartTime;
                if(previousState is bool && (bool)previousState == true)
                {
                    // Radio was on for the current period
                    yield return new TimeWindow() { Event = previousEvent, Start = prevTime, End = curTime };
                }
                else if(previousState is ReceiverPollingContext)
                {
                    ReceiverPollingContext c = (ReceiverPollingContext)previousState;

                    double time = prevTime;
                    while (time < curTime)
                    {
                        double endTime = c.TimeOn + time;
                        if (endTime > curTime) endTime = curTime;
                        yield return new TimeWindow() { Event = previousEvent, Start = time, End = endTime };
                        time += c.TimeOn + c.TimeOff;
                    }
                }
                previousEvent = e;
                previousState = e.EventContext;
                prevTime = e.StartTime;
            }
        }

        TimeWindow TranslatePacketComplete(SimulationEvent e)
        {
            var wpt = (WirelessPacketTransmission)e.EventContext;
            double delay = wpt.WirelessDelay;
            return new TimeWindow() { Start = wpt.Packet.StartTime + delay, End = wpt.Packet.EndTime + delay, Event = e };
        }

        TimeWindow TranslateEvent(SimulationEvent e)
        {
            return new TimeWindow() { Start = e.StartTime, End = e.EndTime, Event = e };
        }

        List<TimeWindow> StoredTimelineEvents = new List<TimeWindow>();

        internal TimeWindow? FindTimelineEvent(Point p)
        {
            if (ActualWidth <= DividerX) return null;

            double pointTime = Timeline.StartTime + (p.X - DividerX) / (ActualWidth - DividerX) * (Timeline.EndTime - Timeline.StartTime);
            foreach (TimeWindow w in StoredTimelineEvents.Reverse<TimeWindow>())
            {
                if(w.Y <= p.Y && (w.Y+w.Height) >= p.Y)
                {
                    if(pointTime >= w.Start && pointTime <= w.End)
                    {
                        return w;
                    }
                }
            }
            return null;
        } 


        void ResetTimeline()
        {
            StoredTimelineEvents.Clear();
        }

        void DrawTimeline(double y, double height, DrawingContext dc, IEnumerable<TimeWindow> events, Brush color)
        {
            double startTime = Timeline.StartTime;
            double endTime = Timeline.EndTime;
            double timeWidth = endTime - startTime;
            if (timeWidth <= 0) timeWidth = 0;
            double screenWidth = ActualWidth-DividerX;

            TimeWindow[] useEvents = events.Select(e =>
            {
                e.Y = y; e.Height = height; return e;
            }).ToArray();

            StoredTimelineEvents.AddRange(useEvents);

            foreach (var e in useEvents)
            {
                double x1 = (e.Start - startTime) * screenWidth / timeWidth;
                double x2 = (e.End - startTime) * screenWidth / timeWidth;

                if (x2 < 0 || x1 > screenWidth) continue;

                dc.DrawRectangle(color, null, new Rect(x1 + DividerX, y, x2 - x1, height));
            }
        }

        internal Rect GetOutline(TimeWindow w)
        {
            double startTime = Timeline.StartTime;
            double endTime = Timeline.EndTime;
            double timeWidth = endTime - startTime;
            if (timeWidth <= 0) timeWidth = 0;
            double screenWidth = ActualWidth - DividerX;
            double x1 = (w.Start - startTime) * screenWidth / timeWidth;
            double x2 = (w.End - startTime) * screenWidth / timeWidth;
            Rect rc = new Rect(x1 + DividerX, w.Y, x2 - x1, w.Height);
            rc.Offset(VisualOffset);
            return rc;
        }

        internal struct TimeWindow : IEquatable<TimeWindow>
        {
            public double Start, End;
            public double Y, Height;
            public SimulationEvent Event;

            public bool Equals(TimeWindow other)
            {
                return Start == other.Start && End == other.End && Y == other.Y && Height == other.Height && Event == other.Event;
            }

            public static bool operator ==(TimeWindow a, TimeWindow b)
            {
                return a.Equals(b);
            }
            public static bool operator !=(TimeWindow a, TimeWindow b)
            {
                return !a.Equals(b);
            }
        }
    }
}
