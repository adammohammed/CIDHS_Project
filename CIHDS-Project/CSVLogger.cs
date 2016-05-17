﻿using System;
using System.Text;
using Microsoft.Kinect;
using System.IO;

namespace CIHDS_Project
{
    public class CSVLogger
    {
        int _current = 0;

        bool _hasEnumeratedJoints = false;

        public bool IsRecording { get; protected set; }

        public string Folder { get; protected set; }

        public string Result { get; protected set; }

        public void Start(int id )
        {
            IsRecording = true;
            Folder = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            Directory.CreateDirectory(Folder);
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
                    foreach (var joint in body.Joints.Values)
                    {
                        line.Append(string.Format("{0}_X,{0}_Y,{0}_Z,", joint.JointType.ToString()));
                    }
                    line.AppendLine();

                    _hasEnumeratedJoints = true;
                }

                foreach (var joint in body.Joints.Values)
                {
                    line.Append(string.Format("{0},{1},{2},", joint.Position.X, joint.Position.Y, joint.Position.Z));
                }

                writer.Write(line);

                _current++;
            }
        }

        public void Stop(string s = null)
        {
            IsRecording = false;
            _hasEnumeratedJoints = false;

            if (s == null)
            {
                Result = DateTime.Now.ToString("yyy_MM_dd_HH_mm_ss") + ".csv";
            }
            else
            {
                Result = s + ".csv";
            }

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

            Directory.Delete(Folder, true);
        }
    }
}