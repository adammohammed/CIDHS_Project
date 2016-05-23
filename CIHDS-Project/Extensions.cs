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
        public static IDictionary<JointType, Point> jointPts;
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


        public static void DrawSkeleton(this Canvas canvas, IReadOnlyDictionary<JointType, Joint> joints, Pen pen, CoordinateMapper cm)
        {

            Dictionary<JointType, Point> bodyPts = new Dictionary<JointType, Point>();
            foreach (JointType jointType in joints.Keys)
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

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
                }
                else
                {
                    drawBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                }

                if (drawBrush != null)
                {
                    canvas.DrawPoint(drawBrush, joints[jointType], cm.MapCameraPointToColorSpace(position));
                }

            }


            foreach (var bone in bones)
            {

                //canvas.DrawBone(joints, bodyPts, bone.Item1, bone.Item2, new Pen()); 
                ColorSpacePoint b1 = cm.MapCameraPointToColorSpace(joints[bone.Item1].Position);
                ColorSpacePoint b2 = cm.MapCameraPointToColorSpace(joints[bone.Item2].Position);
                if (joints[bone.Item1].TrackingState != TrackingState.NotTracked ||
                    joints[bone.Item2].TrackingState != TrackingState.NotTracked)
                {
                    bool isInferred = (joints[bone.Item1].TrackingState == TrackingState.Inferred) || (joints[bone.Item2].TrackingState == TrackingState.Inferred);
                    canvas.DrawLine(b1, b2, isInferred);
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
        
        public static void DrawPoint(this Canvas c, Brush b, Joint j, ColorSpacePoint point)
        {
            if (j.TrackingState == TrackingState.NotTracked) return;
            Ellipse ellipse = new Ellipse
            {
                Width = 15,
                Height = 15,
                Fill = Brushes.White

            };

            if (float.IsInfinity(point.X) || float.IsInfinity(point.Y)) return;
            float pointX = float.IsInfinity(point.X) ? 0 : point.X;
            float pointY = float.IsInfinity(point.Y) ? 0 : point.Y;

            // Scaling
            pointX = pointX / 1920.0f * (float)c.Width;
            pointY = pointY / 1080.0f * (float)c.Height;
            Canvas.SetLeft(ellipse, pointX - ellipse.Width / 2);
            Canvas.SetTop(ellipse, pointY - ellipse.Height / 2);

            c.Children.Add(ellipse);

        }

        public static void DrawLine(this Canvas canvas, ColorSpacePoint first, ColorSpacePoint second, bool inferred)
        {
            if (float.IsInfinity(first.X) || float.IsInfinity(first.Y)) return;
            if (float.IsInfinity(second.X) || float.IsInfinity(second.Y)) return;

            Line line = new Line
            {
                X1 = float.IsInfinity(first.X) ? 0 : canvas.scaleToWidth(first.X),
                Y1 = float.IsInfinity(first.Y) ? 0 : canvas.scaleToHeight(first.Y),
                X2 = float.IsInfinity(second.X) ? 0 : canvas.scaleToWidth(second.X),
                Y2 = float.IsInfinity(second.Y) ? 0 : canvas.scaleToHeight(second.Y),
                StrokeThickness = inferred ? 3 : 8,
                Stroke = inferred ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.CadetBlue) 
            };
            canvas.Children.Add(line);
        }

        private static float scaleToHeight(this Canvas c, float p)
        {
            return (p / 1080.0f * (float)c.Height);
        }
        
        private static float scaleToWidth(this Canvas c, float p)
        {
            return (p / 1920.0f * (float)c.Width);
        }
    }
}
