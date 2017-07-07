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

namespace KinectDepthImage20170707
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

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                DepthImageStream depthStream = kinectSensor.DepthStream;
                depthStream.Enable();

                _depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth,depthStream.FrameHeight,96,96,PixelFormats.Gray16,null);
                _depthImageBitmapRect = new Int32Rect(0,0,depthStream.FrameWidth,depthStream.FrameHeight);
                _depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                DepthImage.Source = _depthImageBitmap;
                kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
                kinectSensor.Start();
            }
        }

        private void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                short[] depthPixelData = new short[depthFrame.PixelDataLength];
                depthFrame.CopyPixelDataTo(depthPixelData);
                _depthImageBitmap.WritePixels(_depthImageBitmapRect,depthPixelData,_depthImageStride,0);
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

        public MainWindow()
        {
            InitializeComponent();
            DiscoverSencor();
        }

        void DiscoverSencor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

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
                        
                    }
                    break;
            }
        }
    }
}
