using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace CIHDS_Project
{
    public static class Game
    {


        public static string StatusText = "Raise both your hands above your shoulders!";
        public enum GameState { Begin, Calibrating, Stage2, Stage3, Stage4, Finish};
        public static GameState gameState = GameState.Begin;

        private static int count = 0;
        private static float upperPositionThreshold = 2.5f;
        private static float lowerPositionThreshold = 2.3f;

        public static void RunGame(this Body b)
        {
            var joints = b.Joints;
            switch (gameState)
            {
                case GameState.Begin:
                    // Check both hands above shoulders
                    float rhand = joints[JointType.HandRight].Position.Y;
                    float lhand = joints[JointType.HandLeft].Position.Y;
                    float topSpine = joints[JointType.SpineShoulder].Position.Y;
                    if(rhand > topSpine && lhand > topSpine)
                    {
                        count++;
                        if(count > 50)
                        {
                            gameState = GameState.Calibrating;
                            count = 0;
                        }
                    }
                    else if (count > 0)
                    {
                        count = 0;
                    }
                    
                    break;
                case GameState.Calibrating:
                    StatusText = "Move to 4 Feet away";
                    // Check the top of spine for distance from kinect
                    CameraSpacePoint shoulder = joints[JointType.ShoulderLeft].Position;
                    
                    // Clamping
                    if (shoulder.Z < 0)
                    {
                        shoulder.Z = 0.1f;
                    }

                    if(shoulder.Z > upperPositionThreshold)
                    {
                        StatusText = StatusText + " - move forward";
                        count = 0;
                    }
                    else if (shoulder.Z < lowerPositionThreshold)
                    {
                        StatusText = StatusText + " - move backward";
                        count = 0;
                    }
                    else
                    {
                        StatusText += " Right There!";
                        count++;
                        if (count > 50)
                        {
                            count = 0;
                            gameState = GameState.Stage2;
                        }
                    }
                    break;
                case GameState.Stage2:
                    StatusText = "Now on stage 2";
                    break;
                case GameState.Stage3:
                    break;
                case GameState.Stage4:
                    break;
            }
        }
    }
}
