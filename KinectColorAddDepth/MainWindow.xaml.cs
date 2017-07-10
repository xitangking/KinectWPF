﻿using System;
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

namespace KinectColorAddDepth
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Member Variables

        private KinectSensor _KinectDevice;
        private WriteableBitmap _GreenScreenImage;
        private Int32Rect _GreenScreenImageRect;
        private int _GreenScreenImageStride;
        private short[] _DepthPixelData;
        private byte[] _ColorPixelData;
        private bool _DoUsePolling;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            this._DoUsePolling = true;

            if (this._DoUsePolling)
            {
                this.ContentRendered += CompositionTarget_Rendering;
            }
            else
            {
                KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
                this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
            }
        }

        #endregion

        #region Methods

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                        this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    // TODO: Give the user feedback to plug-in Kinect Device
                    this.KinectDevice = null;
                    break;
                default:
                    // TODO: Show on error state
                    break;
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            DiscoverKinect();
            if (this._KinectDevice != null)
            {
                try
                {
                    using (ColorImageFrame colorFrame = this._KinectDevice.ColorStream.OpenNextFrame(100))
                    {
                        using (DepthImageFrame depthFrame = this._KinectDevice.DepthStream.OpenNextFrame(100))
                        {
                            RenderGreenScreen(this._KinectDevice, colorFrame, depthFrame);
                        }
                    }
                }
                catch (Exception)
                {
                    //Do nothing, because the likely result is that the Kinect has been unplugged.     
                }
            }
        }

       

        private void DiscoverKinect()
        {
            if (this._KinectDevice != null && this._KinectDevice.Status != KinectStatus.Connected)
            {
                UninitializeKinectSensor(this._KinectDevice);
                this._KinectDevice = null;
            }


            if (this._KinectDevice == null)
            {
                this._KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);


                if (this._KinectDevice != null)
                {
                    InitializeKinectSensor(this._KinectDevice);
                }
            }
        }


        private void InitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.DepthStream.Range = DepthRange.Default;

                sensor.SkeletonStream.Enable();
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

                DepthImageStream depthStream = sensor.DepthStream;
                this._GreenScreenImage = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Bgra32, null);
                this._GreenScreenImageRect = new Int32Rect(0, 0, (int)Math.Ceiling(this._GreenScreenImage.Width), (int)Math.Ceiling(this._GreenScreenImage.Height));
                this._GreenScreenImageStride = depthStream.FrameWidth * 4;
                this.GreenScreenImage.Source = this._GreenScreenImage;

                this._DepthPixelData = new short[this._KinectDevice.DepthStream.FramePixelDataLength];
                this._ColorPixelData = new byte[this._KinectDevice.ColorStream.FramePixelDataLength];

                if (!this._DoUsePolling)
                {
                    sensor.AllFramesReady += KinectDevice_AllFramesReady;
                }

                sensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.ColorStream.Disable();
                sensor.DepthStream.Disable();
                sensor.SkeletonStream.Disable();
                sensor.AllFramesReady -= KinectDevice_AllFramesReady;
            }
        }

        private void KinectDevice_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    RenderGreenScreen(this._KinectDevice, colorFrame, depthFrame);
                }
            }
        }

        private void RenderGreenScreen(KinectSensor kinectDevice, ColorImageFrame colorFrame, DepthImageFrame depthFrame)
        {
            if (kinectDevice != null && depthFrame != null && colorFrame != null)
            {
                int depthPixelIndex;
                int playerIndex;
                int colorPixelIndex;
                ColorImagePoint colorPoint;
                int colorStride = colorFrame.BytesPerPixel * colorFrame.Width;
                int bytesPerPixel = 4;
                byte[] playerImage = new byte[depthFrame.Height * this._GreenScreenImageStride];
                int playerImageIndex = 0;

                depthFrame.CopyPixelDataTo(this._DepthPixelData);
                colorFrame.CopyPixelDataTo(this._ColorPixelData);

                for (int depthY = 0; depthY < depthFrame.Height; depthY++)
                {
                    for (int depthX = 0; depthX < depthFrame.Width; depthX++, playerImageIndex += bytesPerPixel)
                    {
                        depthPixelIndex = depthX + (depthY * depthFrame.Width);
                        playerIndex = this._DepthPixelData[depthPixelIndex] & DepthImageFrame.PlayerIndexBitmask;

                        if (playerIndex != 0)
                        {
                            colorPoint = kinectDevice.MapDepthToColorImagePoint(depthFrame.Format, depthX, depthY, this._DepthPixelData[depthPixelIndex], colorFrame.Format);
                            colorPixelIndex = (colorPoint.X * colorFrame.BytesPerPixel) + (colorPoint.Y * colorStride);

                            playerImage[playerImageIndex] = this._ColorPixelData[colorPixelIndex];         //Blue    
                            playerImage[playerImageIndex + 1] = this._ColorPixelData[colorPixelIndex + 1];     //Green
                            playerImage[playerImageIndex + 2] = this._ColorPixelData[colorPixelIndex + 2];     //Red
                            playerImage[playerImageIndex + 3] = 0xFF;                                          //Alpha
                        }
                    }
                }

                this._GreenScreenImage.WritePixels(this._GreenScreenImageRect, playerImage, this._GreenScreenImageStride, 0);
            }
        }

        #endregion
        
        #region Properties

        public KinectSensor KinectDevice
        {
            get { return this._KinectDevice; }
            set
            {
                if (this._KinectDevice != value)
                {
                    // Uninitialize
                    if (this._KinectDevice != null)
                    {
                        UninitializeKinectSensor(this._KinectDevice);
                        this._KinectDevice = null;
                    }

                    this._KinectDevice = value;

                    // Initialize
                    if (this._KinectDevice != null)
                    {
                        if (this._KinectDevice.Status == KinectStatus.Connected)
                        {
                            InitializeKinectSensor(this._KinectDevice);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
