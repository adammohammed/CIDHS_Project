using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CIHDS_Project
{
    /// <summary>
    /// Interaction logic for Config.xaml
    /// </summary>
    public partial class Config : Window
    {
        public float LRDist_float = 0.8f;
        public float FDist_float = 1.0f;
        public float StartDist_float = 2.5f;
        public bool ValidLRDistance = false;
        public bool ValidFDistance = false;
        public bool ValidStartDistance = false;
        public bool VideoEnabled = true;
        private float minStart = 1.5f;
        private float maxStart = 5.5f;
        private float minFwd = 0.5f;
        private float maxFwd = 2.0f;
        private float minLR = 0.5f;
        private float maxLR = 2.0f;
        private Brush badInput = new SolidColorBrush(Color.FromArgb(130, 255, 0, 0));
        private Brush goodInput = new SolidColorBrush(Colors.White);

        public Config()
        {
            InitializeComponent();
            this.StartPositionConfig.TextChanged += StartPositionConfig_TextChanged;
            this.ForwardDistanceConfig.TextChanged += ForwardDistanceConfig_TextChanged;
            this.StartRange.Content = string.Format("Min: {0}m    Max: {1}m", minStart, maxStart);
            this.FwdRange.Content = string.Format("Min: {0}m    Max: {1}m", minFwd, maxFwd);
            this.LRRange.Content = string.Format("Min: {0}m    Max: {1}m", minLR, maxLR);
        }

        private void ConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.StartPositionConfig.TextChanged += StartPositionConfig_TextChanged;
            this.ForwardDistanceConfig.TextChanged += ForwardDistanceConfig_TextChanged;
            this.LeftRightDistanceConfig.TextChanged += LeftRightDistanceConfig_TextChanged;
            this.StartPositionConfig.Text = StartDist_float.ToString();
            this.ForwardDistanceConfig.Text = FDist_float.ToString();
            this.LeftRightDistanceConfig.Text = LRDist_float.ToString();
        }

        private void LeftRightDistanceConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = this.LeftRightDistanceConfig.Text;
            float pos = 0.5f;
            if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrEmpty(text))
            {
                try
                {
                    pos = float.Parse(text);
                    this.LeftRightDistanceConfig.Background = Brushes.White;
                    if (pos >= minLR  && pos <= maxLR)
                    {
                        ValidLRDistance = true;
                    }
                    else
                    {
                        ValidLRDistance = false;
                    }
                }
                catch (Exception)
                {
                    ValidLRDistance = false;
                }
                this.LeftRightDistanceConfig.Background = ValidLRDistance ? goodInput : badInput;
            }
            

        }

        private void ForwardDistanceConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = this.ForwardDistanceConfig.Text;
            float pos = 0.5f;
            if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrEmpty(text))
            {
                try
                {
                    pos = float.Parse(text);
                    this.ForwardDistanceConfig.Background = Brushes.White; 
                    if (pos >= minFwd  && pos <= maxFwd)
                    {
                        ValidFDistance = true;
                    }
                    else
                    {
                        ValidFDistance = false;
                    }
                }
                catch (Exception)
                {
                    ValidFDistance = false;
                }
                this.ForwardDistanceConfig.Background = ValidFDistance ? goodInput : badInput;
            }

        }

        private void StartPositionConfig_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = this.StartPositionConfig.Text;
            float pos = 0.5f;
            if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrEmpty(text))
            {
                try
                {
                    pos = float.Parse(text);
                    if (pos >= minStart && pos <= maxStart)
                    {
                        ValidStartDistance = true;
                    }
                    else
                    {
                        ValidStartDistance = false;
                    }
                }
                catch (Exception)
                {
                    ValidStartDistance = false;
                }
                this.StartPositionConfig.Background = ValidStartDistance ? goodInput : badInput;
            }

        }

        private void saveBtn_Clicked(object sender, RoutedEventArgs e)
        {
            if(ValidStartDistance && ValidFDistance && ValidLRDistance)
            {
                this.StartDist_float = float.Parse(this.StartPositionConfig.Text);
                this.FDist_float = float.Parse(this.ForwardDistanceConfig.Text);
                this.LRDist_float = float.Parse(this.LeftRightDistanceConfig.Text);
                if (this.VideoStreamEnabled.IsChecked.HasValue)
                {
                    VideoEnabled = (bool)this.VideoStreamEnabled.IsChecked;
                }
                if ((StartDist_float - FDist_float) < 0.8f)
                {
                    MessageBox.Show("Start Distance - Forward Distance Must be greater than 0.8m");
                }
                this.Hide();
            }

        }

        private void cancelBtn_Clicked(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void ConfigWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

    }
}
