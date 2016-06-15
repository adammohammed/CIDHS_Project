using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Diagnostics;

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
        int id = 0;
        private bool canvasSized = false;
        private Stopwatch s = new Stopwatch();
        private Config c;

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion Constructor


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.GetDefault();

            c = new Config();
            c.LRDist_float = Game.rightDistance;
            c.FDist_float = Game.forwardDistance;
            c.StartDist_float = Game.backwardDistance;
            c.Show();
            this.stepBtn.Click += StepBtn_Click;
            if(sensor != null)
            {
                sensor.Open();

                _reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
                _reader.MultiSourceFrameArrived += Reader_FrameArrived;
                cm = sensor.CoordinateMapper;
                if (!setup)
                {
                    this.CreateBones();
                }
            } 
        }

        private void StepBtn_Click(object sender, RoutedEventArgs e)
        {
            Game.setUserId(++id);
        }

        private void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        { 
            var refFrame = e.FrameReference.AcquireFrame();

            // update the video stream
            using(var frame = refFrame.ColorFrameReference.AcquireFrame())
            {
                if(frame != null)
                {
                    if (!setup)
                    {
                        this.canvas.Width = this.Width;
                        this.canvas.Height = this.Height;
                        setup = true; 
                    }
                    camera.Source = frame.ToBitmap();
                    if (!canvasSized)
                    {
                        this.canvas.Width = camera.Width;
                        this.canvas.Height = camera.Height;
                        canvasSized = true;
                    }
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
                                if(Game.gameState == Game.GameState.Begin)
                                {
                                    Game.backwardDistance = c.StartDist_float;
                                    Game.forwardDistance = c.FDist_float;
                                    Game.leftDistance = -1.0f * c.LRDist_float;
                                    Game.rightDistance = 1.0f * c.LRDist_float;
                                }
                                Game.RunGame(body);             // Game.cs Entry Point
                                
                            } // if body is tracked
                        } // if body null
                    } //foreach
                } // if frame 
            } // Using

            // Update the user text!
            this.instructionsTb.Text = Game.StatusText;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (sensor != null)
            {
                sensor.Close();
                sensor = null;
            }
            if(_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }
}
