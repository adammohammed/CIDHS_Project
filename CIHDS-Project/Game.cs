using Microsoft.Kinect;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace CIHDS_Project
{
    public static class Game
    {


        public static string StatusText = "Raise your hands above your shoulders to play!";
        public enum GameState : int { Begin, Calibrating, FwdWalk, BwdWalk, LeftWalk, RightWalk, SaveData, Finish};
        public static GameState gameState = GameState.Begin;
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
        public static int user_id = 0;

        private static int count = 0;

        // Checkpoint threshold distance (in meters)
        private static float thresholdValue = 0.20f;

        //Start Position for fwd walking stage
        //Ending Position for backward walking stage
        private static float backwardDistance = 2.4f;
        private static float lowerPositionThreshold = backwardDistance - thresholdValue;
        private static float upperPositionThreshold = backwardDistance + thresholdValue;


        // Finish position for walking forward
        // Start Position for walking backward
        private static float forwardDistance = 1.0f;
        private static float lowerWalkingThreshold = forwardDistance - thresholdValue;
        private static float upperWalkingThreshold = forwardDistance + thresholdValue;

        // Left walking distance
        private static float leftDistance = -0.55f;

        // Right walking distance
        private static float rightDistance = 0.55f;


        private static CSVLogger c = new CSVLogger();
        private static IReadOnlyDictionary<JointType, Joint> joints;
        private static CameraSpacePoint shoulder;
        private static CameraSpacePoint head;
        private static float positionX;
        private static bool data_processed = false;
        
        public static void RunGame(Body b)
        {

            joints = b.Joints;

            shoulder = joints[JointType.ShoulderLeft].Position;
            head = joints[JointType.Head].Position;
            positionX = head.X;

            // Clamp down if the Z axis numbers are not real
            if (shoulder.Z < 0)
            {
                shoulder.Z = 0.1f;
            }
            // Start recording if in any of these states
            if (gameState == GameState.Calibrating ||
                gameState == GameState.FwdWalk ||
                gameState == GameState.BwdWalk ||
                gameState == GameState.LeftWalk ||
                gameState == GameState.RightWalk)
            {
                string s = stateNames[(int)gameState];
                if (!c.IsRecording)
                {
                    c.Start(user_id);
                }
                else
                {
                    c.Update(b);
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
                    data_processed = false;
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
                        if (count > 50)
                        {
                            count = 0;
                            c.Stop("Calibration");
                            gameState = GameState.FwdWalk;
                        }
                    }
                    break;
                    
                // Forward Walk - Asks Player to move forward
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
                            c.Stop("ForwardWalking");
                            gameState = GameState.BwdWalk;
                        }
                    }
                    break;
                    
                // Backward Walk - Asks Player to return to starting location
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
                            c.Stop("BackwardWalking");
                            gameState = GameState.LeftWalk;
                        }
                    }

                    break;

                case GameState.LeftWalk:
                    StatusText = "Turn left and walk around 5 ft";
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
                        if(count > 50)
                        {
                            count = 0;
                            c.Stop("LeftWalking");
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
                    StatusText = "Turn right and walk around 5 ft";
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
                        if(count > 50)
                        {
                            count = 0;
                            c.Stop("RightWalking");
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
                        c.CalculateRatios("KinectData/user_" + user_id.ToString(), "Calibration_vel_acc.csv", "Calibration_vel_acc_and_ratios.csv");

                        StatusText = "Processing Forward Walking Data";
                        c.CalculateRatios("KinectData/user_" + user_id.ToString(), "ForwardWalking_vel_acc.csv", "ForwardWalking_vel_acc_and_ratios.csv");

                        StatusText = "Processing Backward Walking Data";
                        c.CalculateRatios("KinectData/user_" + user_id.ToString(), "BackwardWalking_vel_acc.csv", "BackwardWalking_vel_acc_and_ratios.csv");
                        
                        StatusText = "Data Processed - Please wait for reset";
                    }
                    
                    count++;
                    if (count % 30 == 0)
                    {
                        if (count == 150)
                        {
                            user_id++;
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

        }

        public static void setUserId(int id)
        {
            user_id = id;
            gameState = GameState.Begin;
            StatusText = "Put your arms in the air to play!";
        }
    }
}
