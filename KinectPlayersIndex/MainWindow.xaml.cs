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

namespace KinectPlayersIndex
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
        private short[] _depthPixelData;
        private WriteableBitmap _enhDepthImage;

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

        public MainWindow()
        {
            InitializeComponent();
            DiscoverSencor();
        }

        
        #region KinectSensor的初始化和卸载

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                DepthImageStream depthStream = kinectSensor.DepthStream;
                depthStream.Enable();
                kinectSensor.SkeletonStream.Enable();

                _enhDepthImage = _depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                RawDepthImage.Source = _depthImageBitmap;
                EnhDepthImage.Source = _enhDepthImage;
                kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
                kinectSensor.Start();
            }
        }

        private void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    _depthPixelData = new short[depthFrame.PixelDataLength];
                    depthFrame.CopyPixelDataTo(_depthPixelData);
                    this._depthImageBitmap.WritePixels(this._depthImageBitmapRect, this._depthPixelData, this._depthImageStride, 0);
                    CreatePlayerDepthImage(depthFrame, this._depthPixelData);
                }
            }
        }

        private void CreatePlayerDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            int playerIndex;
            int depthBytePerPixel = 4;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * depthBytePerPixel];

            for (int i = 0, j = 0; i < pixelData.Length; i++, j += depthBytePerPixel)
            {
                playerIndex = pixelData[i] & DepthImageFrame.PlayerIndexBitmaskWidth;

                if (playerIndex == 0)
                {
                    enhPixelData[j] = 0xFF;
                    enhPixelData[j + 1] = 0xFF;
                    enhPixelData[j + 2] = 0xFF;
                }
                else
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                }
            }
            _enhDepthImage.WritePixels(this._depthImageBitmapRect, enhPixelData, this._depthImageStride, 0);
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

        #endregion
    }
}
