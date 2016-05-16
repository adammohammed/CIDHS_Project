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
using Microsoft.Kinect;
using System.ComponentModel;

namespace CIHDS_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        MultiSourceFrameReader _reader;
        IList<Body> bodies;

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion Constructor


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.GetDefault();

            if(sensor != null)
            {
                sensor.Open();

                _reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
                _reader.MultiSourceFrameArrived += Reader_FrameArrived;
            }


        }

        private void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var refFrame = e.FrameReference.AcquireFrame();

            using(var frame = refFrame.ColorFrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    camera.Source = frame.ToBitmap();
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }
    }
}
