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
    public class CSVLogger
    {
        // Saves Kinects Data to CSV to Desktop 
        int _current = 0;

        bool _hasEnumeratedJoints = false;
        bool has_labeled_joints = false;

        public bool IsRecording { get; protected set; }

        public string Folder { get; protected set; }

        public string Result { get; protected set; }
        public Stopwatch stopwatch;
        public long timestamp = 0;
        public Body bd;
        public int nodes = 0;
        public string DesktopFolder, CSVWriteDirectory;

        public IList<int> velIndex = Enumerable.Range(76, 75).ToList();
        public IList<int> accIndex = Enumerable.Range(151, 75).ToList();
        public IList<int> comboIndex = Enumerable.Range(0, 25).Select(x => x * 3).ToList();
        public Combinations<int> x_ratios;
        public Combinations<int> vel_ratios;
        public Combinations<int> acc_ratios;
        private bool combosCalculated = false;

        public void Start(int id)
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
            StringBuilder notTracked = new StringBuilder();
            StringBuilder labeler = new StringBuilder();

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
                        labeler.Append(string.Format("{0},", joint.JointType.ToString()));
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
                string trackedpath = Path.Combine(Folder, _current.ToString() + "_tracked_state.line");
                using (StreamWriter writer_tracked = new StreamWriter(trackedpath))
                {
                    if (has_labeled_joints == false)
                    {
                        notTracked.Append(labeler);
                        has_labeled_joints = true;
                        //notTracked.AppendLine();
                    }
                    else
                    {

                        foreach (var joint in body.Joints.Values)
                        {
                            if (joint.TrackingState == TrackingState.NotTracked || joint.TrackingState == TrackingState.Inferred)
                            {
                                notTracked.Append("0");
                            }
                            else
                            {
                                notTracked.Append("1");
                            }
                            if(joint.JointType != JointType.ThumbRight)
                            {
                                notTracked.Append(',');
                            }
                        }
                    }
                    writer_tracked.Write(notTracked);
                    notTracked.Clear();
                }
                _current++;
            }
        }

        public void Stop(string s = null)
        {
            IsRecording = false;
            _hasEnumeratedJoints = false;
            has_labeled_joints = false;

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
                    string path_tracked = Path.Combine(Folder, index.ToString() + "_tracked_state.line");

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

            calculateDerivatives(bd, Result, Path.Combine(CSVWriteDirectory, s + "_vel.csv"), nodes, 0);
            calculateDerivatives(bd, Path.Combine(CSVWriteDirectory, s + "_vel.csv"), Path.Combine(CSVWriteDirectory, s + "_vel_acc.csv"), nodes, nodes * 3);
            TrackedStatesToCSV(Path.Combine(CSVWriteDirectory, s + "_vel_acc.csv"), Path.Combine(CSVWriteDirectory, s + "_vel_acc_states.csv"));
            Directory.Delete(Folder, true);
        }

        private void calculateDerivatives(Body b, string InFile, string OutFile, int nodes, int offset)
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
                        if (offset == 0)
                        {
                            newLines.Append(string.Format("{0}_Xvel,{0}_Yvel,{0}_Zvel", joint.JointType));
                        }
                        else
                        {
                            newLines.Append(string.Format("{0}_Xacc,{0}_Yacc,{0}_Zacc", joint.JointType));
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
            // Constants
            if (!combosCalculated)
            {
                x_ratios = new Combinations<int>(comboIndex, 2);

                //vel_ratios = new Combinations<int>(velIndex, 2);
                //acc_ratios = new Combinations<int>(accIndex, 2);
                combosCalculated = true;
            }

            string DesktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string userFolder = Path.Combine(DesktopFolder, pathToDataFolder);
            // End of constants
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
                    int offset = 76 + j * 2;
                    foreach (var v in x_ratios)
                    {
                        // 76 + j*2 results in +76 for x velocities and + 78 for Z velocities
                        s.Append(nodes[v[0] + offset] + "/" + nodes[v[1] + offset]);
                        s.Append(',');
                    }
                }

                // Acceleration Combinations
                for (int j = 0; j < 2; j++)
                {
                    int offset = 76 + 75 + j * 2;
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
                        string[] data = new ArraySegment<string>(instance.Split(','), 76, 150).ToArray();

                        // Velocity Combinations
                        for (int j = 0; j < 4; j++)
                        {
                            int offset = 0;
                            if (j < 2)
                                offset = j * 2;
                            else
                                offset = 75 + (j - 2) * 2;

                            foreach (var v in x_ratios)
                            {
                                // 76 + j*2 results in +76 for x velocities and + 78 for Z velocities
                                s.Append(float.Parse(data[v[0] + offset]) / float.Parse(data[v[1] + offset]));
                                //if (v[0] != comboIndex[comboIndex.Count - 2] && offset != 76 + 75 + 2) {
                                s.Append(',');
                                //}
                            }
                        }
                    }
                }

            }
            
        }

        private void TrackedStatesToCSV(string inputFile, string outputFile)
        {

            using (StreamReader sr = new StreamReader(inputFile))
            {
                var index = 0;
                using (StreamWriter writer = new StreamWriter(outputFile))
                {
                    while(true)
                    {
                        string path_tracked = Path.Combine(Folder, index.ToString() + "_tracked_state.line");
                        StringBuilder line = new StringBuilder();

                        string s = sr.ReadLine();
                        if (s == null) break;
                        line.Append(s);

                        if (File.Exists(path_tracked))
                        {

                            using (StreamReader reader = new StreamReader(path_tracked))
                            {
                                line.Append("," + reader.ReadToEnd());
                            }
                            writer.WriteLine(line);
                        }
                        index++;
                    }
                }
            }
        }

    }
}
