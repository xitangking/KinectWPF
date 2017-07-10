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

namespace KinectDrawDotsGame
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Member Variables

        private KinectSensor _kinectSensor;
        private Skeleton[] _frameSkeletons;
        private DotPuzzle puzzle;
        private int puzzleDotIndex;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            puzzle = new DotPuzzle();
            this.puzzle.Dots.Add(new Point(200, 300));
            this.puzzle.Dots.Add(new Point(1600, 300));
            this.puzzle.Dots.Add(new Point(1650, 400));
            this.puzzle.Dots.Add(new Point(1600, 500));
            this.puzzle.Dots.Add(new Point(1000, 500));
            this.puzzle.Dots.Add(new Point(1000, 600));
            this.puzzle.Dots.Add(new Point(1200, 700));
            this.puzzle.Dots.Add(new Point(1150, 800));
            this.puzzle.Dots.Add(new Point(750, 800));
            this.puzzle.Dots.Add(new Point(700, 700));
            this.puzzle.Dots.Add(new Point(900, 600));
            this.puzzle.Dots.Add(new Point(900, 500));
            this.puzzle.Dots.Add(new Point(200, 500));
            this.puzzle.Dots.Add(new Point(150, 400));

            this.puzzleDotIndex = -1;

            this.Loaded += (s, e) =>
            {
                KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
                DiscoverKinect();
                DrawPuzzle(this.puzzle);
            };

        }
        
        #endregion

        #region Methods

        private void DiscoverKinect()
        {
            KinectSensor = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }

        private void InitializeKinect(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.DepthStream.Enable();
                kinectSensor.SkeletonStream.Enable();
                kinectSensor.SkeletonFrameReady += KinectSensor_SkeletonFrameReady;

                this._frameSkeletons = new Skeleton[KinectSensor.SkeletonStream.FrameSkeletonArrayLength];
                SkeletonViewerElement.KinectDevice = this._kinectSensor;
                kinectSensor.Start();
            }
        }
        
        private void UninitializeKinect(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.Stop();
                kinectSensor.DepthStream.Disable();
                kinectSensor.SkeletonStream.Disable();
                kinectSensor.SkeletonFrameReady -= KinectSensor_SkeletonFrameReady;
                SkeletonViewerElement.KinectDevice = null;
                this._frameSkeletons = null;
            }
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectSensor = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    // TODO: Give the user feedback to plug-in Kinect Device
                    this.KinectSensor = null;
                    break;
                default:
                    // TODO: Show on error state
                    break;
            }
        }

        private void KinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this._frameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this._frameSkeletons);

                    Skeleton[] dataSet2 = new Skeleton[this._frameSkeletons.Length];
                    frame.CopySkeletonDataTo(dataSet2);

                    if (skeleton == null)
                    {
                        HandCursorElement.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Joint primaryHand = GetPrimaryHand(skeleton);
                        TrackHand(primaryHand);
                        TrackPuzzle(primaryHand.Position);
                    }
                }
            }
        }

        /// <summary>
        /// 获取距离最近的手
        /// </summary>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        private Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();
            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint rightHand = skeleton.Joints[JointType.HandRight];
                if (rightHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                        primaryHand = rightHand;
                    else
                    {
                        if (primaryHand.Position.Z > rightHand.Position.Z)
                            primaryHand = rightHand;
                    }
                }
            }
            return primaryHand;
        }

        /// <summary>
        /// 获取最近的游戏者骨骼
        /// </summary>
        /// <param name="skeletons"></param>
        /// <returns></returns>
        private Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }

        /// <summary>
        /// 追踪手部
        /// </summary>
        /// <param name="hand"></param>
        private void TrackHand(Joint hand)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
                HandCursorElement.Visibility = Visibility.Collapsed;
            else
            {
                HandCursorElement.Visibility = Visibility.Visible;
                DepthImagePoint point =
                    this.KinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(hand.Position,
                        this.KinectSensor.DepthStream.Format);
                point.X = (int) ((int) (point.X * LayoutRoot.ActualWidth / KinectSensor.DepthStream.FrameWidth) -
                                 (HandCursorElement.ActualWidth / 2.0));
                point.Y = (int) ((int) (point.Y * LayoutRoot.ActualHeight / KinectSensor.DepthStream.FrameHeight) -
                                 (HandCursorElement.ActualHeight / 2.0));

                Canvas.SetLeft(HandCursorElement,point.X);
                Canvas.SetTop(HandCursorElement,point.Y);

                if (hand.JointType == JointType.HandRight)
                    HandCursorScale.ScaleX = 1;
                else
                {
                    HandCursorScale.ScaleX = -1;
                }
            }
        }

        /// <summary>
        /// 绘制问题的点
        /// </summary>
        /// <param name="dotPuzzle"></param>
        private void DrawPuzzle(DotPuzzle dotPuzzle)
        {
            PuzzleBoardElement.Children.Clear();

            if (puzzle != null)
            {
                for (int i = 0; i < puzzle.Dots.Count; i++)
                {
                    Grid dotContainer = new Grid();
                    dotContainer.Width = 50;
                    dotContainer.Height = 50;
                    dotContainer.Children.Add(new Ellipse { Fill = Brushes.Gray});

                    TextBlock dotLabel = new TextBlock();
                    dotLabel.Text = (i+1).ToString();
                    dotLabel.Foreground = Brushes.White;
                    dotLabel.FontSize = 24;
                    dotLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    dotContainer.Children.Add(dotLabel);

                    // 在UI界面上绘制点
                    Canvas.SetTop(dotContainer,puzzle.Dots[i].Y - (dotContainer.Height / 2));
                    Canvas.SetLeft(dotContainer,puzzle.Dots[i].X - (dotContainer.Width / 2));
                    PuzzleBoardElement.Children.Add(dotContainer);
                }
            }
        }

        private void TrackPuzzle(SkeletonPoint position)
        {
            if (this.puzzleDotIndex == this.puzzle.Dots.Count)
            {

            }
            else
            {
                Point dot;
                if (this.puzzleDotIndex + 1 < this.puzzle.Dots.Count)
                {
                    dot = this.puzzle.Dots[this.puzzleDotIndex + 1];
                }
                else
                {
                    dot = this.puzzle.Dots[0];
                }

                DepthImagePoint point =
                    KinectSensor.CoordinateMapper.MapSkeletonPointToDepthPoint(position,
                        KinectSensor.DepthStream.Format);
                point.X = (int) (point.X * LayoutRoot.ActualWidth / KinectSensor.DepthStream.FrameWidth);
                point.Y = (int) (point.Y * LayoutRoot.ActualHeight / KinectSensor.DepthStream.FrameHeight);
                Point handPoint = new Point(point.X,point.Y);
                Point dotDiff = new Point(dot.X - handPoint.X,dot.Y - handPoint.Y);
                double length = Math.Sqrt(dotDiff.X + dotDiff.Y * dotDiff.Y);

                int lastPoint = this.CrayonElement.Points.Count - 1;

                if (length < 25)
                {
                    if (lastPoint > 0)
                    {
                        this.CrayonElement.Points.RemoveAt(lastPoint);
                    }

                    this.CrayonElement.Points.Add(new Point(dot.X,dot.Y));

                    this.CrayonElement.Points.Add(new Point(dot.X,dot.Y));

                    this.puzzleDotIndex++;
                    if (this.puzzleDotIndex == this.puzzle.Dots.Count)
                    {
                        
                    }
                }
                else
                {
                    if (lastPoint > 0)
                    {
                        Point lineEndPoint = this.CrayonElement.Points[lastPoint];
                        this.CrayonElement.Points.RemoveAt(lastPoint);

                        lineEndPoint.X = handPoint.X;
                        lineEndPoint.Y = handPoint.Y;
                        this.CrayonElement.Points.Add(lineEndPoint);
                    }
                }
            }
        }

        #endregion

        #region Properties

        public KinectSensor KinectSensor
        {
            get { return _kinectSensor; }
            set
            {
                if (_kinectSensor != value)
                {
                    // Uninitialize
                    if (_kinectSensor != null)
                    {
                        UninitializeKinect(_kinectSensor);
                        _kinectSensor = null;
                    }

                    _kinectSensor = value;

                    // Initialize
                    if (_kinectSensor != null && _kinectSensor.Status == KinectStatus.Connected)
                    {
                        InitializeKinect(_kinectSensor);
                    }

                }
            }
        }

        #endregion
    }
}
