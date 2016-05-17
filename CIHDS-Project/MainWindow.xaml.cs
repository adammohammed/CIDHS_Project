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
        CoordinateMapper cm;
        bool setup = false;
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
                cm = sensor.CoordinateMapper;
                this.CreateBones();
            }


        }

        private void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        { 
            var refFrame = e.FrameReference.AcquireFrame();

            // update the video stream
            using(var frame = refFrame.ColorFrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    if (setup)
                    {
                        this.canvas.Width = frame.FrameDescription.Width;
                        this.canvas.Height = frame.FrameDescription.Height;
                        setup = true; 
                    }
                    camera.Source = frame.ToBitmap();
                }
            }

            // update the body info
            using(var frame = refFrame.BodyFrameReference.AcquireFrame())
            {
                Pen p = new Pen(new SolidColorBrush(Colors.Blue), 2);
                if(frame != null)
                {
                    canvas.Children.Clear();

                    bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(bodies);

                    foreach (var body in bodies)
                    {

                        if(body != null)
                        {
                            if(body.IsTracked)
                            {
                                canvas.DrawSkeleton(body.Joints, p, this.cm);
                                body.RunGame();
                            } // if body is tracked
                        } // if body null
                    } //foreach
                } // if frame 
            } // Using

            // Update the user text!
            this.stepBtn.Content = Game.StatusText;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }
    }
}
