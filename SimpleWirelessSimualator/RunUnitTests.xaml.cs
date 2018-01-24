using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace SimpleWirelessSimualator
{
    /// <summary>
    /// Interaction logic for RunUnitTests.xaml
    /// </summary>
    public partial class RunUnitTests : Window
    {
        public RunUnitTests()
        {
            InitializeComponent();
            textIterations.Text = "1000";
            InitComboBox();
            
        }

        public MainWindow LinkedWindow;

        int IterationCount = 0;

        void InitComboBox()
        {
            comboSelectTest.Items.Clear();
            foreach (var unitTests in WirelessUnitTesting.FindUnitTests())
            {
                foreach (var test in unitTests.UnitTestMethods)
                {
                    ComboBoxItem item = new ComboBoxItem() { Content = $"{test.NodeType.Name}: {test.UnitTestMethod.Name}", DataContext = test };
                    comboSelectTest.Items.Add(item);
                }
            }
            comboSelectTest.SelectedIndex = 0;
        }


        private void textIterations_TextChanged(object sender, TextChangedEventArgs e)
        {
            int iterations;
            if(int.TryParse(textIterations.Text, out iterations))
            {
                IterationCount = iterations;
                textIterations.Background = Brushes.White;
            }
            else
            {
                textIterations.Background = Brushes.LightPink;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int count = IterationCount;
            int pass = 0;
            int fail = 0;
            int completed = 0;

            // Determine which test to use
            WirelessUnitTest test = ((ComboBoxItem)comboSelectTest.SelectedItem)?.DataContext as WirelessUnitTest;
            if (test == null) return;

            listBox.Items.Clear();
            listBox.Items.Add(new ListBoxItem() { Content = $"Starting {count} iterations of {test.UnitTestMethod.Name}", Background=Brushes.LightBlue });


            btnStart.IsEnabled = false;
            ThreadPool.QueueUserWorkItem((context) =>
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();
                Parallel.For(0, count, (index) =>
                {
                    WirelessUnitTestInstance instance = WirelessUnitTestInstance.RunUnitTest(LinkedWindow.Network, test);

                    if (instance.TestPassed)
                    {
                        Interlocked.Increment(ref pass);
                    }
                    else
                    {
                        Interlocked.Increment(ref fail);

                        Dispatcher.Invoke(() =>
                        {
                            ListBoxItem lb = new ListBoxItem() { Content = $"({index}) Test Failed: {instance.TestException.ToString()}", DataContext = instance, Background = Brushes.LightPink };
                            lb.MouseDoubleClick += OpenTestFailureItem;
                            listBox.Items.Add(lb);
                        });
                    }
                    if (Interlocked.Increment(ref completed) % 50 == 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Title = $"Running tests... ({(pass + fail)}/{count})";
                        });
                    }
                });
                sw.Stop();

                double ms = Math.Floor(sw.Elapsed.TotalMilliseconds * 100) / 100;

                Dispatcher.Invoke(() => {
                    listBox.Items.Add(new ListBoxItem() { Content = $"Completed in {ms}ms. {pass} Passed, {fail} Failed.", Background = Brushes.LightBlue });

                    Title = "Run Unit Tests";
                    btnStart.IsEnabled = true;
                });
            });
        }

        private void OpenTestFailureItem(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
