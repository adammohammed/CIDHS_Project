using Microsoft.Kinect;

namespace CIHDS_Project
{
    public static class Game
    {


        public static string StatusText = "Raise your hands above your shoulders to play!";
        public enum GameState : int { Begin, Calibrating, FwdWalk, BwdWalk, SaveData, Finish};
        public static GameState gameState = GameState.Begin;
        public static string[] stateNames = {
            "Begin",
            "Calibrating",
            "ForwardWalking",
            "BackwardWalking",
            "SaveData",
            "Finished",
        };
        public static int user_id = 0;

        private static int count = 0;
        //Start Position for fwd walking stage
        //Ending Position for backward walking stage
        private static float lowerPositionThreshold = 2.3f;
        private static float upperPositionThreshold = 2.5f;


        // Finish position for walking forward
        // Start Position for walking backward
        private static float lowerWalkingThreshold = 0.9f;
        private static float upperWalkingThreshold = 1.3f;
        private static CSVLogger c = new CSVLogger();

        public static void RunGame(this Body b)
        {
            var joints = b.Joints;
            
            CameraSpacePoint shoulder = joints[JointType.ShoulderLeft].Position;

            // Clamp down if the Z axis numbers are not real
            if (shoulder.Z < 0)
            {
                shoulder.Z = 0.1f;
            }

            // Start recording if in any of these states
            if (gameState == GameState.Calibrating ||
                gameState == GameState.FwdWalk ||
                gameState == GameState.BwdWalk)
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
                            gameState = GameState.SaveData;
                        }
                    }

                    break;
                    
                // Puts the new files ina specific folder
                case GameState.SaveData:
                    StatusText = "Exercises Finished!";
                    count++;
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
