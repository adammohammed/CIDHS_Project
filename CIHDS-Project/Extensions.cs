using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace CIHDS_Project
{
    public static class Extensions
    {
        public static List<Tuple<JointType, JointType>> bones;
        public static IDictionary<JointType,Point> jointPts;
        private static Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private static Pen drawingPen = new Pen(Brushes.Green, 3);


        public static ImageSource ToBitmap(this ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * (format.BitsPerPixel / 8); 
            
            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }


        public static void DrawSkeleton(this Canvas canvas, IReadOnlyDictionary<JointType,Joint> joints, Pen pen, CoordinateMapper cm)
        { 

            Dictionary<JointType, Point> bodyPts = new Dictionary<JointType, Point>();
            foreach(JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;
                TrackingState trackingState = joints[jointType].TrackingState;


                CameraSpacePoint position = joints[jointType].Position;
                if (position.Z < 0)
                {
                    position.Z = 0.1f;
                }

                DepthSpacePoint depthSpacePoint = cm.MapCameraPointToDepthSpace(position);
                bodyPts[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
        
                if(trackingState == TrackingState.Tracked)
                {
                    drawBrush = new SolidColorBrush(Color.FromArgb(128,0,0,255));
                }
                else
                {
                    drawBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                }

                if(drawBrush != null)
                {
                    canvas.DrawPoint(drawBrush, joints[jointType], cm.MapCameraPointToColorSpace(position));
                }

            }

             
            foreach(var bone in bones)
            {
                ColorSpacePoint b1 = cm.MapCameraPointToColorSpace(joints[bone.Item1].Position);
                ColorSpacePoint b2 = cm.MapCameraPointToColorSpace(joints[bone.Item2].Position);
                try {
                    canvas.DrawLine(b1, b2);
                }
                catch (Exception)
                {

                }

            } 
            
        }
        public static void CreateBones(this MainWindow m)
        {
            bones = new List<Tuple<JointType, JointType>>();
            bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.ThumbLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.ThumbRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));
        }
        
        public static void DrawBone(this Canvas c, IReadOnlyDictionary<JointType,Joint> joints, IDictionary<JointType,Point> jp, JointType j1, JointType j2, Pen p)
        {
            Joint joint0 = joints[j1];
            Joint joint1 = joints[j2];

            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            Pen drawPen = inferredBonePen;
            if((joint0.TrackingState == TrackingState.Tracked) &&
                (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            Line line = new Line {
                X1 = joint0.Position.X,
                Y1 = joint0.Position.Y,
                X2 = joint1.Position.X,
                Y2 = joint1.Position.Y,
                StrokeThickness = 8,
                Fill = Brushes.Aqua,
                Stroke = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0))
            };
            Console.WriteLine("Trying to draw line");
            c.Children.Add(line);
            Console.WriteLine("line Drawn");
        }

        public static void DrawPoint(this Canvas c, Brush b, Joint j, ColorSpacePoint point)
        { 
            if (j.TrackingState == TrackingState.NotTracked) return;

            Ellipse ellipse = new Ellipse
            {
                Width = 15,
                Height = 15,
                Fill = Brushes.White 

            };

            try
            {
                Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
                Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);

                c.Children.Add(ellipse); 
            } 
            catch(Exception)
            {
                return;
            }
            
        }

        public static void DrawLine(this Canvas canvas, ColorSpacePoint first, ColorSpacePoint second)
        { 
            Line line = new Line
            {
                X1 = (double) first.X,
                Y1 = (double) first.Y,
                X2 = (double) second.X,
                Y2 = (double) second.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.Green)

            };
            canvas.Children.Add(line);
        }

        public static void DrawMeALine(this Canvas can)
        {
            Line l = new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = 1000,
                Y2 = 1000,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.Green)
            };

            can.Children.Add(l);
        }

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

    } 
}
