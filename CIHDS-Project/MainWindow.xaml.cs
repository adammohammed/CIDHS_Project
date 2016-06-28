using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Kinect;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
        bool VidEnabled = true;
        private bool canvasSized = false;
        private Stopwatch s = new Stopwatch();
        private Config c;
        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        #region Constructor
        public MainWindow()
        {
            List<Assembly> allAssemblies = new List<Assembly>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
                allAssemblies.Add(Assembly.LoadFile(dll));
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

            this.stepBtn.Click += StepBtn_Click;
            this.KeyDown += MainWindow_KeyDown;
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

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.C)
            {
                if(c != null){
                    c.Show();
                }else{
                    c = new Config();
                }

            }
        }

        private void StepBtn_Click(object sender, RoutedEventArgs e)
        {
            Game.gameState = Game.GameState.Begin;
        }

        private void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var refFrame = e.FrameReference.AcquireFrame();

            // update the video stream

            VidEnabled = this.c.VideoEnabled;


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
                    if (VidEnabled)
                    {
                        camera.Source = frame.ToBitmap();
                    }
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
                                    Game.Z_LRDistance = (Game.backwardDistance + Game.forwardDistance) / 2.0f;
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
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
            c.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }
    }
}
