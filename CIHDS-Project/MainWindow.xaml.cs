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
        // Sensor
        private KinectSensor sensor;

        // Frame Reader
        private BodyFrameReader bodyFrameReader;

        // Things to draw colors
        private List<Tuple<JointType,JointType>> bones;
        private List<Pen> bodyColors;

        // Visual representation of kinect data
        private DrawingGroup dg;
        private ImageSource imgSrc;

        // Body's in frame
        private Body[] bodies;

        // Mapping Data
        private CoordinateMapper cm;
        private double windowWidth;
        private double windowHeight;

        private List<Tuple<string,string>>steps;
        private int stepIndex;

        public MainWindow()
        {
            this.stepIndex = 0;
            this.steps = new List<Tuple<string, string>>();
            this.steps.Add(new Tuple<string,string>("Next Step:", "Please move 4 ft away from kinect"));
            this.steps.Add(new Tuple<string, string>("Next Step:", "Please put your arms in the air"));
            this.steps.Add(new Tuple<string, string>("Next Step:", "Please walk forward"));
            this.steps.Add(new Tuple<string, string>("Next Step:", "Please walk backward"));
            this.steps.Add(new Tuple<string, string>("Done:", "Successfully Completed Exercises!"));

            InitializeComponent();

            // Get the first kinect 
            this.sensor = KinectSensor.GetDefault();

            // Only read the skeletal source
            this.bodyFrameReader = this.sensor.BodyFrameSource.OpenReader();

            // Check to see if sensor is available or not
            this.sensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            // Viewport Setup
            this.dg = new DrawingGroup();
            this.imgSrc = new DrawingImage(this.dg);

            this.bodyColors = BodyBrush.createBrushes();
            this.bones = BodyBrush.createBones();


            this.sensor.Open();

            this.cm = this.sensor.CoordinateMapper;
            this.KinectDisplay.Visibility = Visibility.Visible;
            this.LoginDisplay.Visibility = Visibility.Visible;
            this.PatientDisplay.Visibility = Visibility.Hidden;

            this.nextBtn.Content = "Begin";
            this.loginBtn.Click += LoginBtn_Click;
            this.nextBtn.Click += NextBtn_Click;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (stepIndex < steps.Count)
            {
                this.nextBtn.Content = steps[stepIndex].Item1;
                this.stepDialog.Text = steps[stepIndex].Item2;
                stepIndex++;
            }
            else
            {
                this.nextBtn.Visibility = Visibility.Hidden;
                this.stepDialog.Visibility = Visibility.Hidden;
                MessageBox.Show("Save Dialog");
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if(usernameTb.Text.Equals("doctor") && passwordBox.Password.Equals(""))
            {
                this.LoginDisplay.Visibility = Visibility.Hidden;
                this.PatientDisplay.Visibility = Visibility.Visible;

            }
            else
            {
                MessageBox.Show("Incorrect Login!","Unauthorized");
            }
            usernameTb.Text = "";
            passwordBox.Password = "";
        }

        /// <summary>
        // Starts collecting Data once window is loaded
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            FrameDescription frameDesc = this.sensor.DepthFrameSource.FrameDescription;
            this.windowWidth = frameDesc.Width;
            this.windowHeight = frameDesc.Height;
            if(this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += Body_FrameArrived;
            }

            if (this.sensor == null)
            {
            }
        }


        // Gets the Image to Display to screen
        public ImageSource ImageSource
        {
            get
            {
                return this.imgSrc;
            }
        }

        /// <summary>
        /// Clean up the memory and close the bodyframe / sensor streams
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.sensor != null)
            {
                this.sensor.Close();
                this.sensor = null;
            }
        }

        /// <summary>
        /// Handles all incoming Body Frames
        /// </summary>
        private void Body_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool data_recvd = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if(bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    data_recvd = true;
                } 
            }

            if (data_recvd)
            {
                var target = this.bodies.FirstOrDefault();
                using(DrawingContext dc = this.dg.Open())
                {
                    Pen brush = this.bodyColors[3];

                    if (target.IsTracked)
                    {
                        //Draw Clipped Edges (body , dc)
                        IReadOnlyDictionary<JointType, Joint> joints = target.Joints;

                        Dictionary<JointType, Point> joint_pts = new Dictionary<JointType, Point>();

                        foreach(JointType jointType in joints.Keys)
                        {
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = 0.1f; 
                            }

                            DepthSpacePoint depthSpacePoint = this.cm.MapCameraPointToDepthSpace(position);
                            joint_pts[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }

                        DrawIt.DrawBody(joints, joint_pts, dc, brush, this.bones);
                        DrawIt.DrawHand(target.HandLeftState, joint_pts[JointType.HandLeft], dc);
                        DrawIt.DrawHand(target.HandRightState, joint_pts[JointType.HandRight], dc);

                    }
                }
            }


        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
           // change status text for sensor connected
        }
    }
}
