using Microsoft.Kinect;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HelloKinectWPF20170705
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainWindow : Window
    {
        /// <summary>
        /// 私有字段
        /// </summary>
        private KinectSensor _kinect;

        private WriteableBitmap _colorImageBitmap;
        private int _colorImageStride;
        private Int32Rect _colorImageBitmapRect;
        private byte[] _colorImagePixelData;
        private BackgroundWorker _worker;

        //#region 事件模式

        ///// <summary>
        ///// _kinect 的属性
        ///// </summary>
        //public KinectSensor Kinect
        //{
        //    get => _kinect;
        //    set
        //    {
        //        if (_kinect != value)
        //        {
        //            if (_kinect != null)
        //            {
        //                UninitializeKinectSensor(_kinect);
        //                _kinect = null;
        //            }
        //            if (value != null && value.Status == KinectStatus.Connected)
        //            {
        //                _kinect = value;
        //                InitializeKinectSensor(_kinect);
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        /////
        ///// </summary>
        //public MainWindow()
        //{
        //    InitializeComponent();
        //    Loaded += (s, e) => DiscoverKinectSensor();
        //    Unloaded += (s, e) => Kinect = null;
        //}

        ///// <summary>
        ///// 查找可用的 KinectSensor
        ///// </summary>
        //private void DiscoverKinectSensor()
        //{
        //    KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
        //    Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        //}

        ///// <summary>
        ///// 处理KinectSensor状态的改变
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        //{
        //    switch (e.Status)
        //    {
        //        case KinectStatus.Connected:
        //            if (_kinect == null)
        //                _kinect = e.Sensor;
        //            break;
        //        case KinectStatus.Disconnected:
        //            if (_kinect == e.Sensor)
        //            {
        //                _kinect = null;
        //                _kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        //                if (_kinect == null)
        //                {
        //                    // TODO:通知Kinect已经被拔出

        //                }
        //            }
        //            break;
        //    }
        //}

        ///// <summary>
        ///// 初始化KinectSensor
        ///// </summary>
        ///// <param name="kinectSensor"></param>
        //private void InitializeKinectSensor(KinectSensor kinectSensor)
        //{
        //    if (kinectSensor != null)
        //    {
        //        kinectSensor.ColorStream.Enable();
        //        ColorImageStream colorStream = kinectSensor.ColorStream;
        //        //创建一个WriteableBitmap，设置其属性
        //        this._colorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
        //            96, 96, PixelFormats.Bgr32, null);
        //        //创建图形的矩形框
        //        this._colorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
        //        //pixels 中更新区域的步幅。表示影像中一行的像素所占的字节数
        //        this._colorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
        //        //将可写Bitmap赋值给图片资源，以后更改colorImageBitmap时具体图片资源也会变化
        //        ColorImageElement.Source = this._colorImageBitmap;

        //        kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;
        //        kinectSensor.Start();
        //    }
        //}

        ///// <summary>
        ///// 反初始化KinectSensor
        ///// </summary>
        ///// <param name="kinectSensor"></param>
        //private void UninitializeKinectSensor(KinectSensor kinectSensor)
        //{
        //    if (kinectSensor != null)
        //    {
        //        kinectSensor.Stop();
        //        kinectSensor.ColorFrameReady -= kinectSensor_ColorFrameReady;
        //    }
        //}

        ///// <summary>
        ///// 处理彩色帧变换事件
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        //{
        //    using (ColorImageFrame frame = e.OpenColorImageFrame())
        //    {
        //        if (frame != null)
        //        {
        //            byte[] pixelData = new byte[frame.PixelDataLength];
        //            frame.CopyPixelDataTo(pixelData);

        //            //对像素数据进行处理
        //            //for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel) //增幅是每一个像素点所占据的字节数
        //            //{
        //            //    //每个像素的第一个字节是蓝色通道，第二个字节是绿色通道，第三个字节是红色通道，（格式是Bgr32,即RGB32位）
        //            //    pixelData[i] = 0x00;//
        //            //    pixelData[i + 1] = 0x00;
        //            //}
        //            //PixelDisposal.HighSaturation(pixelData,frame.BytesPerPixel);

        //            //根据像素信息直接更改图片（通过写入的方式）
        //            this._colorImageBitmap.WritePixels(this._colorImageBitmapRect, pixelData, this._colorImageStride, 0);
        //        }
        //    }
        //}

        //#endregion

        #region “拉取”模式

        public MainWindow()
        {
            InitializeComponent();
            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerAsync();
            // CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            DiscoverKinectSensor();
            PollColorImageStream();
        }

        private void PollColorImageStream()
        {
            if (this._kinect == null)
            {
                //TODO: Display a message to plug-in a Kinect.
            }
            else
            {
                try
                {
                    using (ColorImageFrame frame = this._kinect.ColorStream.OpenNextFrame(100))
                    {
                        if (frame != null)
                        {
                            frame.CopyPixelDataTo(this._colorImagePixelData);
                            //PixelDisposal.InvertedColor(_colorImagePixelData, _colorImagePixelData.Length); TODO: 不能对像素进行处理
                            this.ColorImageElement.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this._colorImageBitmap.WritePixels(this._colorImageBitmapRect, this._colorImagePixelData, this._colorImageStride, 0);
                            }));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private void DiscoverKinectSensor()
        {
            if (this._kinect != null && this._kinect.Status != KinectStatus.Connected)
            {
                this._kinect = null;
            }
            if (this._kinect == null)
            {
                this._kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);

                if (this._kinect != null)
                {
                    this._kinect.ColorStream.Enable();
                    this._kinect.Start();

                    ColorImageStream colorStream = this._kinect.ColorStream;
                    this.ColorImageElement.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this._colorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight, 96,
                            96, PixelFormats.Bgr32, null);
                        this._colorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                        this._colorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                        this.ColorImageElement.Source = this._colorImageBitmap;
                        this._colorImagePixelData = new byte[colorStream.FramePixelDataLength];
                    }));
                }
            }
        }

        private void Worker_DoWork(Object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if (worker != null)
            {
                while (!worker.CancellationPending)
                {
                    DiscoverKinectSensor();
                    PollColorImageStream();
                }
            }
        }

        #endregion “拉取”模式

        /// <summary>
        /// 截图事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TakePictureButton_Click(object sender, RoutedEventArgs e)
        {
            String fileName = "snapshot.jpg";
            if (File.Exists(fileName))
                File.Delete(fileName);

            using (FileStream savedSnapshot = new FileStream(fileName, FileMode.Create))
            {
                BitmapSource image = (BitmapSource)ColorImageElement.Source;
                JpegBitmapEncoder jpegBitmapEncoder = new JpegBitmapEncoder();
                jpegBitmapEncoder.QualityLevel = 100;
                jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(image));
                jpegBitmapEncoder.Save(savedSnapshot);

                savedSnapshot.Flush();
                savedSnapshot.Close();
                savedSnapshot.Dispose();
            }
        }
    }
}