using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectDepthImage20170707Part2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _kinect;
        private WriteableBitmap _depthImageBitmap;
        private Int32Rect _depthImageBitmapRect;
        private int _depthImageStride;
        private DepthImageFrame _lastDepthFrame;
        private short[] _depthPixelData;

        public MainWindow()
        {
            InitializeComponent();
            DiscoverSencor();
        }

        public KinectSensor Kinect
        {
            get => _kinect;
            set
            {
                if (_kinect != value)
                {
                    if (_kinect != null)
                    {
                        UnInitializeKinectSensor(_kinect);
                        _kinect = null;
                    }
                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        _kinect = value;
                        InitializeKinectSensor(_kinect);
                    }
                }
            }
        }

        #region KinectSensor的初始化和卸载

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                DepthImageStream depthStream = kinectSensor.DepthStream;
                depthStream.Enable();

                _depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                DepthImage.Source = _depthImageBitmap;
                kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
                kinectSensor.Start();
            }
        }

        private void UnInitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.Stop();
                kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
            }
        }

        void DiscoverSencor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        private void StopKinect()
        {
            if (Kinect != null)
            {
                if (Kinect.Status == KinectStatus.Connected)
                {
                    Kinect.Stop();
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StopKinect();
        }

        #endregion

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (Kinect == null)
                    {
                        Kinect = e.Sensor;
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (Kinect == e.Sensor)
                    {
                        Kinect = null;
                        Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
                    }
                    if (Kinect == null)
                    {
                        // TODO: 提示kinect已经拔出，插入kinect
                    }
                    break;
            }
        }

        private void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (_lastDepthFrame != null)
            {
                _lastDepthFrame.Dispose();
                _lastDepthFrame = null;
            }
            _lastDepthFrame = e.OpenDepthImageFrame();
            if (_lastDepthFrame != null)
            {
                _depthPixelData = new short[_lastDepthFrame.PixelDataLength];
                _lastDepthFrame.CopyPixelDataTo(_depthPixelData);
                _depthImageBitmap.WritePixels(_depthImageBitmapRect, _depthPixelData, _depthImageStride, 0);
                CreateDepthHistogram(_lastDepthFrame, _depthPixelData);
            }
        }

        private void CreateDepthHistogram(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth;
            int[] depths = new int[4096]; // 最大的深度值是4096
            double chartBarWidth = Math.Max(3, DepthHistogram.ActualWidth / depths.Length);
            int maxValue = 0;

            DepthHistogram.Children.Clear();
            int LoDepthThreshold = 500;
            int HiDepthThreshold = 4000;

            for (int i = 0; i < pixelData.Length; ++i)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (depth >= LoDepthThreshold && depth <= HiDepthThreshold)
                {
                    depths[depth]++;
                }
            }

            for (int i = 0; i < depths.Length; i++)
            {
                maxValue = Math.Max(maxValue, depths[i]);
            }

            for (int i = 0; i < depths.Length; i++)
            {
                if (depths[i] > 0)
                {
                    Rectangle r = new Rectangle();
                    r.Fill = Brushes.BlueViolet;
                    r.Width = chartBarWidth;
                    r.Height = DepthHistogram.ActualHeight * (depths[i] / (double) maxValue);
                    r.Margin = new Thickness(1,0,1,0);
                    r.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    DepthHistogram.Children.Add(r);
                }
            }
        }
    }
}
