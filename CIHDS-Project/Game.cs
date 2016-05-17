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


        public static string StatusText = "Raise both your hands above your shoulders to play!";
        public enum GameState { Begin, Calibrating, FwdWalk, BwdWalk, SaveData, Finish};
        public static GameState gameState = GameState.Begin;

        private static int count = 0;
        //Start Position for fwd walking stage
        //Ending Position for backward walking stage
        private static float lowerPositionThreshold = 2.3f;
        private static float upperPositionThreshold = 2.5f;


        // Finish position for walking forward
        // Start Position for walking backward
        private static float lowerWalkingThreshold = 0.9f;
        private static float upperWalkingThreshold = 1.3f;

        public static void RunGame(this Body b)
        {
            var joints = b.Joints;
            
            CameraSpacePoint shoulder = joints[JointType.ShoulderLeft].Position;

            if (shoulder.Z < 0)
            {
                shoulder.Z = 0.1f;
            }

            switch (gameState)
            {
                case GameState.Begin:
                    StatusText = "Raise both your hands above your shoulders to play!";
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
                    StatusText = "Move into position!";

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
                        StatusText = "Stop Right There!";
                        count++;
                        if (count > 50)
                        {
                            count = 0;
                            gameState = GameState.FwdWalk;
                        }
                    }
                    break;
                case GameState.FwdWalk:
                    StatusText = "Move forward slowly";
                    
                    if(shoulder.Z < upperWalkingThreshold && shoulder.Z > lowerWalkingThreshold)
                    {
                        StatusText = "Stop!";
                        if(shoulder.Z < upperPositionThreshold)
                        count++;
                        if (count > 5)
                        {
                            count = 0;
                            gameState = GameState.BwdWalk;
                        }
                    }
                    break;
                case GameState.BwdWalk:
                    StatusText = "Walk back to the start";
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
                        StatusText = "Stop Right There!";
                        count++;
                        if (count > 30)
                        {
                            count = 0;
                            gameState = GameState.SaveData;
                        }
                    }

                    break;
                case GameState.SaveData:
                    StatusText = "Exercises Finished!"; 
                    //Save Dialog 
                    break;
                case GameState.Finish:
                    StatusText = "Thank You for Playing!";
                    break;
            }
        }
    }
}
