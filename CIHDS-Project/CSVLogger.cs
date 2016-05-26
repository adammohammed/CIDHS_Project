using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Kinect;
using System.IO;

namespace CIHDS_Project
{
    public class CSVLogger
    {
        // Saves Kinects Data to CSV to Desktop 
        int _current = 0;

        bool _hasEnumeratedJoints = false;

        public bool IsRecording { get; protected set; }

        public string Folder { get; protected set; }

        public string Result { get; protected set; }
        public Stopwatch stopwatch;
        public long timestamp = 0;
        public Body bd;
        public int nodes = 0;
        public string DesktopFolder, CSVWriteDirectory;

        public void Start(int id )
        {
            nodes = 0;
            IsRecording = true;
            stopwatch = new Stopwatch();
            stopwatch.Start();
            Folder = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            Directory.CreateDirectory(Folder);
            DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            CSVWriteDirectory = Path.Combine(DesktopFolder, "KinectData/user_" + id.ToString());
            Directory.CreateDirectory(CSVWriteDirectory);
        }

        public void Update(Body body)
        {
            if (!IsRecording) return;
            if (body == null || !body.IsTracked) return;

            string path = Path.Combine(Folder, _current.ToString() + ".line");

            using (StreamWriter writer = new StreamWriter(path))
            {
                StringBuilder line = new StringBuilder();

                if (!_hasEnumeratedJoints)
                {
                    bd = body;
                    line.Append("TimeStamp,");
                    foreach (var joint in body.Joints.Values)
                    {
                        line.Append(string.Format("{0}_X,{0}_Y,{0}_Z", joint.JointType.ToString()));
                        if (joint.JointType.ToString() != JointType.ThumbRight.ToString())
                        {
                            line.Append(',');
                        }
                        nodes++;
                    }
                    line.AppendLine();

                    _hasEnumeratedJoints = true;
                }

                foreach (var joint in body.Joints.Values)
                {
                    line.Append(string.Format("{0},{1},{2},{3}", stopwatch.ElapsedMilliseconds, joint.Position.X, joint.Position.Y, joint.Position.Z));
                }

                writer.Write(line);

                _current++;
            }
        }

        public void Stop(string s = null)
        {
            IsRecording = false;
            _hasEnumeratedJoints = false;

            stopwatch.Stop();

            if (s == null)
            {
                Result = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss") + ".csv";
            }
            else
            {
                Result = s + ".csv";
            }

            Result = Path.Combine(CSVWriteDirectory, Result);

            using (StreamWriter writer = new StreamWriter(Result))
            {
                for (int index = 0; index < _current; index++)
                {
                    string path = Path.Combine(Folder, index.ToString() + ".line");

                    if (File.Exists(path))
                    {
                        string line = string.Empty;

                        using (StreamReader reader = new StreamReader(path))
                        {
                            line = reader.ReadToEnd();
                        }

                        writer.WriteLine(line);
                    }
                }
            }

            calculateVelocities(bd, Result, Path.Combine(CSVWriteDirectory, "VEL" + s + ".csv"), nodes);
            Directory.Delete(Folder, true);
        }

        private void calculateVelocities(Body b, string InFile, string OutFile, int nodes)
        {
            using (StreamReader sr = new StreamReader(InFile))
            {
                String HeaderLine = sr.ReadLine();
                StringBuilder newLines = new StringBuilder();
                using (StreamWriter s = new StreamWriter(OutFile))
                {
                    newLines.Append(",");
                    foreach (var joint in b.Joints.Values)
                    {
                        newLines.Append(string.Format("{0}_vel,{0}_vel,{0}_vel", joint.JointType));
                        if(joint.JointType != JointType.ThumbRight)
                        {
                            newLines.Append(',');
                        }
                    }
                    s.Write(HeaderLine);
                    s.WriteLine(newLines);

                    newLines.Clear();
                    bool firstLine = true;
                    string[] PreviousLine = { };
                    while (true)
                    {
                        var line = sr.ReadLine();
                        if (line == null) break;
                        var line_split = line.Split(',');

                        newLines.Append(',');
                        if(firstLine)
                        {
                            for(int i = 0; i < nodes*3; i++)
                            {
                                newLines.Append("0");
                                if(i != nodes * 3 - 1)
                                {
                                    newLines.Append(',');
                                }

                            }
                            firstLine = false;
                        }
                        else
                        {

                            for(int node = 0; node < nodes; node++)
                            {
                                var deltaTime = (float.Parse(line_split[0]) - float.Parse(PreviousLine[0])) / 1000.0f;
                                newLines.Append(string.Format("{0},{1},{2}",
                                    ((float.Parse(line_split[1 + node]) - (float.Parse(PreviousLine[1 + node]))) / deltaTime),
                                    ((float.Parse(line_split[2 + node]) - (float.Parse(PreviousLine[2 + node]))) / deltaTime),
                                    ((float.Parse(line_split[3 + node]) - (float.Parse(PreviousLine[3 + node]))) / deltaTime)
                                    ));
                                if(node != nodes - 1)
                                {
                                    newLines.Append(',');
                                }                                     
                            }

                        }


                        s.Write(line);
                        s.WriteLine(newLines);
                        newLines.Clear();

                        PreviousLine = line_split;
                    }

                }
            }
        }



    }
}
