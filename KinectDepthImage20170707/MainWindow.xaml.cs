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
using System.Xaml.Schema;
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
            if (_lastDepthFrame != null)
            {
                _lastDepthFrame.Dispose();
                _lastDepthFrame = null;
            }
            _lastDepthFrame = e.OpenDepthImageFrame();
            if(_lastDepthFrame != null)
            {
                _depthPixelData = new short[_lastDepthFrame.PixelDataLength];
                _lastDepthFrame.CopyPixelDataTo(_depthPixelData);
                _depthImageBitmap.WritePixels(_depthImageBitmapRect, _depthPixelData, _depthImageStride,0);
                CreateColorDepthImage(_lastDepthFrame,_depthPixelData);
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

        /// <summary>
        /// 重载鼠标事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            DepthImage_MouseLeftButtonUp(this,e);
        }

        /// <summary>
        /// 处理鼠标单击事件，查看深度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DepthImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Point p = e.GetPosition(DepthImage);
            //if (_depthPixelData != null && _depthPixelData.Length > 0)
            //{
            //    Int32 pixelIndex = (Int32) (p.X + ((Int32) p.Y * this._lastDepthFrame.Width));
            //    Int32 depth = this._depthPixelData[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
            //    Int32 depthInches = (Int32) (depth * 0.0393700787);
            //    Int32 depthFt = depthInches / 12;
            //    depthInches = depthInches % 12;
            //    PixelDepth.Text = $"{depth}mm ~ {depthFt}'{depthInches}";
            //}
        }

        /// <summary>
        /// 屏蔽过高和过低
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="pixelData"></param>
        private void CreateLighterShadesOfGray(DepthImageFrame depthFrame, short[] pixelData)
        {
            Int32 depth;
            Int32 loThreahold = 0; //最低过滤值
            Int32 hiThreshold = 3500; // 最高过滤值
            short[] enhPixelData = new short[depthFrame.Width * depthFrame.Height];
            for (int i = 0; i < pixelData.Length; i++)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                if (depth < loThreahold || depth > hiThreshold)
                {
                    enhPixelData[i] = 0xff;
                }
                else
                {
                    {
                        enhPixelData[i] = (short) ~pixelData[i];
                    }
                }
            }
            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96,
                PixelFormats.Gray16, null, enhPixelData, depthFrame.Width * depthFrame.BytesPerPixel);
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
            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96,
                PixelFormats.Bgr32, null, enhPixelData, depthFrame.Width * bytePerPixel);
        }

        /// <summary>
        /// 用彩色模式显示
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="pixelData"></param>
        private void CreateColorDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            Int32 depth;
            Double hue;
            Int32 loThreshold = 1200;
            Int32 hiThreshold = 3500;
            Int32 bytesPerPixel = 4;
            byte[] rgb = new byte[3];
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel];

            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth < loThreshold || depth > hiThreshold)
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                }
                else
                {
                    hue = ((360 * depth / 0xFFF) + loThreshold);
                    ConvertHslToRgb(hue, 100, 100, rgb);

                    enhPixelData[j] = rgb[2];  //Blue
                    enhPixelData[j + 1] = rgb[1];  //Green
                    enhPixelData[j + 2] = rgb[0];  //Red
                }
            }

            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, enhPixelData, depthFrame.Width * bytesPerPixel);
        }

        /// <summary>
        /// 将H(Hue色调)S(Saturation饱和度)L(Light亮度)颜色空间转换到RGB颜色空间
        /// </summary>
        /// <param name="hue">色调</param>
        /// <param name="saturation">饱和度</param>
        /// <param name="lightness">亮度</param>
        /// <param name="rgb">RGB颜色</param>
        public void ConvertHslToRgb(Double hue, Double saturation, Double lightness, byte[] rgb)
        {
            Double red = 0.0;
            Double green = 0.0;
            Double blue = 0.0;
            hue = hue % 360.0;
            saturation = saturation / 100.0;
            lightness = lightness / 100.0;

            if (saturation == 0.0)
            {
                red = lightness;
                green = lightness;
                blue = lightness;
            }
            else
            {
                Double huePrime = hue / 60.0;
                Int32 x = (Int32)huePrime;
                Double xPrime = huePrime - (Double)x;
                Double L0 = lightness * (1.0 - saturation);
                Double L1 = lightness * (1.0 - (saturation * xPrime));
                Double L2 = lightness * (1.0 - (saturation * (1.0 - xPrime)));

                switch (x)
                {
                    case 0:
                        red = lightness;
                        green = L2;
                        blue = L0;
                        break;
                    case 1:
                        red = L1;
                        green = lightness;
                        blue = L0;
                        break;
                    case 2:
                        red = L0;
                        green = lightness;
                        blue = L2;
                        break;
                    case 3:
                        red = L0;
                        green = L1;
                        blue = lightness;
                        break;
                    case 4:
                        red = L2;
                        green = L0;
                        blue = lightness;
                        break;
                    case 5:
                        red = lightness;
                        green = L0;
                        blue = L1;
                        break;
                }
            }

            rgb[0] = (byte)(255.0 * red);
            rgb[1] = (byte)(255.0 * green);
            rgb[2] = (byte)(255.0 * blue);
        }
    }
}
