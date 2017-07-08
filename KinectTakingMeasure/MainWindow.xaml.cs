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

namespace KinectTakingMeasure
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

                _depthImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                _depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                _depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                DepthImage.Source = _depthImageBitmap;
                kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
                kinectSensor.Start();
            }
        }

        private void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    _depthPixelData = new short[frame.PixelDataLength];
                    frame.CopyPixelDataTo(this._depthPixelData);
                    CreateBetterShadesOfGray(frame, _depthPixelData);
                    CalculatePlayerSize(frame, _depthPixelData);
                }
            }
        }

        private void CalculatePlayerSize(DepthImageFrame frame, short[] depthPixelData)
        {
            int depth;
            int playerIndex;
            int pixelIndex;
            int bytesPerPixel = frame.BytesPerPixel;
            PlayerDepthData[] players = new PlayerDepthData[6];

            for (int row = 0; row < frame.Height; row++)
            {
                for (int col = 0; col < frame.Width; col++)
                {
                    pixelIndex = col + (row * frame.Width);
                    depth = depthPixelData[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                    if (depth != 0)
                    {
                        playerIndex = (depthPixelData[pixelIndex] & DepthImageFrame.PlayerIndexBitmask) - 1;

                        if (playerIndex > -1)
                        {
                            if (players[playerIndex] == null)
                            {
                                players[playerIndex] = new PlayerDepthData(playerIndex + 1, frame.Width,frame.Height);
                            }

                            players[playerIndex].UpdateData(col, row, depth);
                        }
                    }
                }
            }
            PlayerDepthData.ItemsSource = players;
        }

        /// <summary>
        /// 用32为模式显示
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="pixelData"></param>
        private void CreateBetterShadesOfGray(DepthImageFrame depthFrame, short[] pixelData)
        {
            Int32 depth;
            Int32 gray;
            Int32 loThreashold = 0;
            Int32 bytePerPixel = 4;
            Int32 hiThreshold = 3500;
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytePerPixel];
            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytePerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (depth < loThreashold || depth > hiThreshold)
                {
                    gray = 0xFF;
                }
                else
                {
                    gray = (255 * depth / 0xFFF);
                }
                enhPixelData[j] = (byte)gray;
                enhPixelData[j + 1] = (byte)gray;
                enhPixelData[j + 2] = (byte)gray;

            }
            DepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96,
                PixelFormats.Bgr32, null, enhPixelData, depthFrame.Width * bytePerPixel);
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

    }
}
