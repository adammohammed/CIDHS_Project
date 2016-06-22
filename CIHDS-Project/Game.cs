using Microsoft.Kinect;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;

namespace CIHDS_Project
{
    static class Game
    {


        public static string StatusText = "Raise your hands above your shoulders to play!";
        public enum GameState : int { Begin, Calibrating, FwdWalk, BwdWalk, LeftWalk, RightWalk, SaveData, Finish};
        public static string[] stateNames = {
            "Begin",
            "Calibrating",
            "ForwardWalking",
            "BackwardWalking",
            "LeftWalking",
            "RightWalking",
            "SaveData",
            "Finished",
        };
        public static GameState gameState = GameState.Begin;

        // Start position / BwdWalk End Position
        public static float backwardDistance = 2.5f;

        // Forward Walking end position
        public static float forwardDistance = 1.0f;
        
        // Left walking end position
        public static float leftDistance = -0.80f;

        // Right walking end position
        public static float rightDistance = 0.80f;


        private static GameState[] recordables = { GameState.Calibrating, GameState.FwdWalk, GameState.BwdWalk, GameState.LeftWalk, GameState.RightWalk}; 
        
        // Checkpoint threshold distance (in meters)
        private static float thresholdValue = 0.20f;

        //Start Position for fwd walking stage with buffer
        //Ending Position for backward walking stage
        private static float lowerPositionThreshold = backwardDistance - thresholdValue; 
        private static float upperPositionThreshold = backwardDistance + thresholdValue;


        // Finish position for walking forward
        // Start Position for walking backward
        private static float lowerWalkingThreshold = forwardDistance - thresholdValue;
        private static float upperWalkingThreshold = forwardDistance + thresholdValue;
 
        private static int count = 0;

        private static CSVLogger c = new CSVLogger();
        private static bool data_processed = false;
        private static string outputName;
        private static string currentUser;
        
        public static void RunGame(Body b)
        {

            IReadOnlyDictionary<JointType, Joint> joints = b.Joints;

            CameraSpacePoint shoulder = joints[JointType.ShoulderLeft].Position;
            CameraSpacePoint head = joints[JointType.Head].Position;
            float positionX = head.X;

            bool recordFrame = recordables.Contains(gameState);

            // Clamp down if the Z axis numbers are not real
            if (shoulder.Z < 0)
            {
                shoulder.Z = 0.1f;
            }
            // Start recording if in any of these states

            if (recordFrame)
            {
                outputName = stateNames[(int)gameState];
                if (!c.IsRecording)
                {
                    c.Start(currentUser);
                }
            }

            // Check different values at different game times
            switch (gameState)
            {
                case GameState.Begin:
                    StatusText = "Put your arms in the air to play!";
                    // Check both hands above shoulders
                    float rhand = joints[JointType.HandRight].Position.Y;
                    float lhand = joints[JointType.HandLeft].Position.Y;
                    float topSpine = joints[JointType.SpineShoulder].Position.Y;
                    
                    upperPositionThreshold = backwardDistance + thresholdValue;
                    lowerPositionThreshold = backwardDistance - thresholdValue;

                    lowerWalkingThreshold = forwardDistance - thresholdValue;
                    upperWalkingThreshold = forwardDistance + thresholdValue;

                    data_processed = false;
                    if(rhand > topSpine && lhand > topSpine)
                    {
                        
                        count++;
                        if(count > 50)
                        {
                            currentUser = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                            gameState = GameState.Calibrating;
                            count = 0;
                        }
                    }
                    else if (count > 0)
                    {
                        count = 0;
                    }
                    break;

                // Calibration - Moving Player to accurate Start Position
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
                        if (count > 10)
                        {
                            count = 0;
                            c.Stop(outputName);
                            gameState = GameState.FwdWalk;
                        }
                    }
                    break;
                    
                // Forward Walk - Asks Player to move forward
                case GameState.FwdWalk:
                    StatusText = "Move forward slowly";
                    
                    // Check to see if between start and end position
                    if(shoulder.Z > backwardDistance || shoulder.Z < forwardDistance) recordFrame = false;

                    if(shoulder.Z < upperWalkingThreshold && shoulder.Z > lowerWalkingThreshold)
                    {
                        StatusText = "Stop Right There!";
                        if(shoulder.Z < upperPositionThreshold)
                        count++;
                        if (count > 10)
                        {
                            count = 0;
                            c.Stop(outputName);
                            gameState = GameState.BwdWalk;
                        }
                    }
                    break;
                    
                // Backward Walk - Asks Player to return to starting location
                case GameState.BwdWalk:
                    StatusText = "Walk back to the start";

                    // Check to see if between start/end position
                    if (shoulder.Z > backwardDistance || shoulder.Z < forwardDistance) recordFrame = false;
                    
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
                        if (count > 10)
                        {
                            count = 0;
                            c.Stop(outputName);
                            gameState = GameState.LeftWalk;
                        }
                    }

                    break;

                case GameState.LeftWalk:
                    StatusText = "Turn left and walk - ";
                    // Check to see if between start/end position
                    if (positionX > 0 || positionX < leftDistance) recordFrame = false;
                    
                    if(positionX > leftDistance+thresholdValue)
                    {
                        StatusText += positionX.ToString() ;
                        count = 0;
                    }
                    else if ((positionX < leftDistance + thresholdValue) &&
                        (positionX > leftDistance - thresholdValue))
                    {
                        StatusText = "Stop Right There";
                        count++;
                        if(count > 10)
                        {
                            count = 0;
                            c.Stop(outputName);
                            gameState = GameState.RightWalk;
                        } 
                    }
                    else
                    {
                        StatusText += " Too far!";
                        count = 0;
                    }
                    break;

                case GameState.RightWalk:
                    StatusText = "Turn right and walk - ";
                    
                    if (positionX < 0 || positionX > rightDistance) recordFrame = false;

                    if(positionX < rightDistance - thresholdValue)
                    {
                        StatusText += positionX.ToString() ;
                        count = 0;
                    }
                    else if ((positionX > rightDistance - thresholdValue) &&
                        (positionX < rightDistance + thresholdValue))
                    {
                        StatusText = "Stop Right There!";
                        count++;
                        if(count > 30)
                        {
                            count = 0;
                            c.Stop(outputName);
                            StatusText = "Saving/Processing Data...";
                            gameState = GameState.SaveData;
                        } 
                    }
                    else
                    {
                        StatusText += " Too far!";
                        count = 0;
                    }
                    break;
                    
                // Puts the new files ina specific folder
                case GameState.SaveData:
                    if (!data_processed)
                    {
                        data_processed = true;
                        StatusText = "Processing Calibration Data";
                        c.CalculateRatios("KinectData/user_" + currentUser, "Calibrating_vel_acc.csv", "Calibrating_vel_acc_and_ratios.csv");

                        StatusText = "Processing Forward Walking Data";
                        c.CalculateRatios("KinectData/user_" + currentUser, "ForwardWalking_vel_acc.csv", "ForwardWalking_vel_acc_and_ratios.csv");

                        StatusText = "Processing Backward Walking Data";
                        c.CalculateRatios("KinectData/user_" + currentUser, "BackwardWalking_vel_acc.csv", "BackwardWalking_vel_acc_and_ratios.csv");

                        StatusText = "Processing Left Walking Data";
                        c.CalculateRatios("KinectData/user_" + currentUser, "LeftWalking_vel_acc.csv", "LeftWalking_vel_acc_and_ratios.csv");

                        StatusText = "Processing Right Walking Data";
                        c.CalculateRatios("KinectData/user_" + currentUser, "RightWalking_vel_acc.csv", "RightWalking_vel_acc_and_ratios.csv");
                        
                        StatusText = "Data Processed - Please wait for reset";
                    }
                    
                    count++;
                    if (count % 30 == 0)
                    {
                        if (count == 150)
                        {
                            gameState = GameState.Begin;
                        }
                        else
                        {
                            StatusText = "Resetting in ";
                            StatusText += (5 - count / 30).ToString() + " seconds";
                        }
                    }
                    
                    //Save Dialog 
                    break;
                 
                // Game Reset
                case GameState.Finish:
                    StatusText = "Thank You for Playing!";
                    break;
            }

            if (recordFrame && c.IsRecording)
            {
                c.Update(b);
            }

        }

        public static void setUserId(int id)
        {
            gameState = GameState.Begin;
            StatusText = "Put your arms in the air to play!";
        }
    }
}
