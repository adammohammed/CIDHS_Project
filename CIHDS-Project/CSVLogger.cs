using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Kinect;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Combinatorics.Collections;

namespace CIHDS_Project
{
    class CSVLogger
    {
        // Saves Kinects Data to CSV to Desktop 
        int _current = 0;

        bool _hasEnumeratedJoints = false;

        public bool IsRecording { get; protected set; }

        public string Folder { get; protected set; }

        public string Result { get; protected set; }
        private Stopwatch stopwatch;
        private Body bd;
        private int nodes = 0;
        private string CSVWriteDirectory;
        private string DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        private IList<int> comboIndex = Enumerable.Range(0, 25).Select(x => x * 3).ToList();
        private Combinations<int> x_ratios;
        private bool combosCalculated = false;

        public void Start(string date)
        {
            // This starts recording
            nodes = 0;
            IsRecording = true;
            stopwatch = new Stopwatch();
            stopwatch.Start();
            Folder = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            
            Directory.CreateDirectory(Folder);

            CSVWriteDirectory = Path.Combine(DesktopFolder, "KinectData/user_" + date);
            Directory.CreateDirectory(CSVWriteDirectory);
        }

        public void Update(Body body)
        {
            // Call this each time you receive a new body frame in order to update CSV
            if (!IsRecording) return;
            if (body == null || !body.IsTracked) return;

            string path = Path.Combine(Folder, _current.ToString() + ".line");
            using (StreamWriter writer = new StreamWriter(path))
            {
                StringBuilder line = new StringBuilder();
                StringBuilder notTracked = new StringBuilder();
                StringBuilder labeller = new StringBuilder();


                if (!_hasEnumeratedJoints)
                {
                    bd = body;
                    line.Append("TimeStamp,");
                    foreach (var joint in body.Joints.Values)
                    {
                        line.Append(string.Format("{0}_X,{0}_Y,{0}_Z", joint.JointType.ToString()));
                        labeller.Append(string.Format("{0}", joint.JointType.ToString()));
                        if (joint.JointType.ToString() != JointType.ThumbRight.ToString())
                        {
                            line.Append(',');
                            labeller.Append(",");
                        }
                        nodes++;
                    }
                    line.Append("," + labeller);
                    line.AppendLine();

                    _hasEnumeratedJoints = true;
                }

                foreach (var joint in body.Joints.Values)
                {
                    line.Append(string.Format("{0},{1},{2},{3}", stopwatch.ElapsedMilliseconds, joint.Position.X, joint.Position.Y, joint.Position.Z));
                    if (joint.TrackingState == TrackingState.NotTracked || joint.TrackingState == TrackingState.Inferred)
                    {
                        notTracked.Append("0");
                    }
                    else
                    {
                        notTracked.Append("1");
                    }
                    if (joint.JointType != JointType.ThumbRight)
                    {
                        notTracked.Append(',');
                    }
                }

                writer.Write(line + "," + notTracked);
                notTracked.Clear();
            }
            _current++;
        }

        public void Stop(string outputFile = null)
        {
            // Consolidates teh multiple .line files created into a CSV File 

            IsRecording = false;
            _hasEnumeratedJoints = false;

            stopwatch.Stop();

            if (outputFile == null)
            {
                Result = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss") + ".csv";
            }
            else
            {
                Result = outputFile + ".csv";
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

            // Takes raw data file and adds node velocity in each coordinate axis
            calculateDerivatives(bd, Result, Path.Combine(CSVWriteDirectory, outputFile + "_vel.csv"), nodes, 0);
            
            // Takes the velocity file and adds node acceleration in each coordinate axis
            calculateDerivatives(bd, Path.Combine(CSVWriteDirectory, outputFile + "_vel.csv"), Path.Combine(CSVWriteDirectory, outputFile + "_vel_acc.csv"), nodes, nodes * 3 + 25);
            
            // Gets rid of the .line folder 
            Directory.Delete(Folder, true);
        }

        private void calculateDerivatives(Body b, string inFile, string outFile, int nodes, int offset)
        {
            using (StreamReader sr = new StreamReader(inFile))
            {
                String HeaderLine = sr.ReadLine();
                StringBuilder newLines = new StringBuilder();
                using (StreamWriter s = new StreamWriter(outFile))
                {
                    newLines.Append(",");
                    foreach (var joint in b.Joints.Values)
                    {
                        if (offset == 0)
                        {
                            newLines.Append(string.Format("{0}_Xvel,{0}_Yvel,{0}_Zvel", joint.JointType.ToString()));
                        }
                        else
                        {
                            newLines.Append(string.Format("{0}_Xacc,{0}_Yacc,{0}_Zacc", joint.JointType.ToString()));
                        }
                        if (joint.JointType != JointType.ThumbRight)
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
                        if (firstLine)
                        {
                            for (int i = 0; i < nodes; i++)
                            {
                                newLines.Append("0,0,0");
                                if (i != nodes - 1)
                                {
                                    newLines.Append(',');
                                }

                            }
                            firstLine = false;
                        }
                        else
                        {

                            for (int node = 0; node < nodes * 3; node = node + 3)
                            {
                                var deltaTime = (float.Parse(line_split[0]) - float.Parse(PreviousLine[0])) / 1000.0f;
                                newLines.Append(string.Format("{0},{1},{2}",
                                    ((float.Parse(line_split[1 + node + offset]) - (float.Parse(PreviousLine[1 + node + offset]))) / deltaTime),
                                    ((float.Parse(line_split[2 + node + offset]) - (float.Parse(PreviousLine[2 + node + offset]))) / deltaTime),
                                    ((float.Parse(line_split[3 + node + offset]) - (float.Parse(PreviousLine[3 + node + offset]))) / deltaTime)
                                    ));
                                if (node < (nodes - 1) * 3)
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


        public void CalculateRatios(string pathToDataFolder, string DataSheet, string outputFile)
        {
            string[] nodes;
            // is the index of the ThumbRightZ Tracking cell in the csv
            // 1 - Timestamp
            // 2 - 76  -> Node X, Node Y, Node Z for the of 25 nodes 
            // 76 - 101 -> Node Tracking state for the 25 nodes
            // Later this should be found by reading the file
            int velocityIndex = 101;
            
            // comboIndex holds all indicies for X positional nodes (e.g. 0, 3, 6, .. , 72)
            // All the possible pairs are found using Combinatorics Library from NuGet
            if (!combosCalculated)
            {
                x_ratios = new Combinations<int>(comboIndex, 2); 
                combosCalculated = true;
            }
            
            string userFolder = Path.Combine(DesktopFolder, pathToDataFolder);

            using (StreamReader sr = new StreamReader(Path.Combine(userFolder, DataSheet)))
            {
                // Write Header
                string header = sr.ReadLine();
                nodes = header.Split(',');
                StringBuilder s = new StringBuilder();
                s.Append(header + ',');

                // Velocity Combinations
                for (int j = 0; j < 2; j++)
                {
                    int offset = velocityIndex + j * 2;
                    foreach (var v in x_ratios)
                    {
                        // 101 + j*2 results in +76 for x velocities and + 101 for Z velocities
                        s.Append(nodes[v[0] + offset] + "/" + nodes[v[1] + offset]);
                        s.Append(',');
                    }
                }

                // Acceleration Combinations
                for (int j = 0; j < 2; j++)
                {
                    int offset = velocityIndex + 75 + j * 2;
                    foreach (var v in x_ratios)
                    {
                        s.Append(nodes[v[0] + offset] + "/" + nodes[v[1] + offset]);
                        if (j != 1)
                        {
                            s.Append(',');
                        } else if (j == 1 && v[0] != comboIndex[comboIndex.Count - 2])
                        {
                            s.Append(',');
                        }

                    }
                }
                var target = Path.Combine(userFolder, "ratios_" + DataSheet);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                using (StreamWriter sw = new StreamWriter(Path.Combine(userFolder, outputFile)))
                {

                    while (true)
                    {
                        var instance = sr.ReadLine();
                        if (instance == null) break;
                        sw.WriteLine(s);
                        s.Clear();
                        s.Append(instance + ',');
                        string[] data = new ArraySegment<string>(instance.Split(','), velocityIndex, 150).ToArray();

                        // Ratios between pairs
                        // Ratios only done in the same axis (e.g. NeckX/SpineMidX or NeckZ/SpineMidZ)
                        // 0 or (2) Offset gets each X or Z Pair
                        // +75 + (0 or 2) acceleration X or Z Pairs
                        for (int j = 0; j < 4; j++)
                        {
                            int offset = 0;
                            if (j < 2)
                                offset = j * 2;
                            else
                                offset = 75 + (j - 2) * 2;

                            foreach (var v in x_ratios)
                            {
                                s.Append(float.Parse(data[v[0] + offset]) / float.Parse(data[v[1] + offset]));
                                if(j != 3)
                                {
                                    s.Append(',');
                                }
                                else if (v[0] != comboIndex[comboIndex.Count - 2] && j == 3) {
                                    s.Append(',');
                                }
                            }
                        }
                    }
                }

            }
            
        }


    }
}
