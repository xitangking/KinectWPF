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

namespace KinectDrawSkeletonStream
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Member Variables

        private KinectSensor _kinectSensor;
        private readonly Brush[] _skeletonBrushes; // 
        private Skeleton[] _frameSkeletons;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            _skeletonBrushes = new Brush[] {Brushes.Black,Brushes.Crimson,Brushes.Indigo,Brushes.DodgerBlue,Brushes.Purple,Brushes.Pink} ;
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.KinectSensor = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }
        
        #endregion

        #region Methods

        private void InitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.DepthStream.Range = DepthRange.Default;

                this._frameSkeletons = new Skeleton[this._kinectSensor.SkeletonStream.FrameSkeletonArrayLength];
                kinectSensor.SkeletonFrameReady += KinectDevice_SkeletonFrameReady;
                kinectSensor.SkeletonStream.Enable();
                kinectSensor.DepthStream.Enable();

                kinectSensor.Start();
            }
        }

        private void KinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    Polyline figure;
                    Brush userBrush;
                    Skeleton skeleton;

                    LayoutRoot.Children.Clear();
                    frame.CopySkeletonDataTo(this._frameSkeletons);

                    for (int i = 0; i < this._frameSkeletons.Length; i++)
                    {
                        skeleton = this._frameSkeletons[i];
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            userBrush = this._skeletonBrushes[i % this._skeletonBrushes.Length];

                            //绘制头和躯干
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.Head, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.Spine,
                                JointType.ShoulderRight, JointType.ShoulderCenter, JointType.HipCenter
                            });
                            LayoutRoot.Children.Add(figure);

                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipLeft, JointType.HipRight });
                            LayoutRoot.Children.Add(figure);

                            //绘制作腿
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft });
                            LayoutRoot.Children.Add(figure);

                            //绘制右腿
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight });
                            LayoutRoot.Children.Add(figure);

                            //绘制左臂
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft });
                            LayoutRoot.Children.Add(figure);

                            //绘制右臂
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight });
                            LayoutRoot.Children.Add(figure);
                        }
                    }
                }
            }
        }

        private Polyline CreateFigure(Skeleton skeleton, Brush brush, JointType[] joints)
        {
            Polyline figure = new Polyline();

            figure.StrokeThickness = 8;
            figure.Stroke = brush;

            for (int i = 0; i < joints.Length; i++)
            {
                figure.Points.Add(GetJointPoint(skeleton.Joints[joints[i]]));
            }

            return figure;
        }

        private Point GetJointPoint(Joint joint)
        {

            DepthImagePoint point = this.KinectSensor.MapSkeletonPointToDepth(joint.Position, this.KinectSensor.DepthStream.Format);

            point.X *= (int)this.LayoutRoot.ActualWidth / KinectSensor.DepthStream.FrameWidth;
            point.Y *= (int)this.LayoutRoot.ActualHeight / KinectSensor.DepthStream.FrameHeight;

            return new Point(point.X, point.Y);
        }

        private void UninitializeKinectSensor(KinectSensor kinectSensor)
        {
            if (kinectSensor != null)
            {
                kinectSensor.Stop();
                kinectSensor.SkeletonFrameReady -= KinectDevice_SkeletonFrameReady;
                kinectSensor.SkeletonStream.Disable();
                kinectSensor.DepthStream.Disable();
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
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectSensor = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }

        #endregion

        #region Properties

        public KinectSensor KinectSensor
        {
            get { return this._kinectSensor; }
            set
            {
                if (this._kinectSensor != value)
                {
                    if (this._kinectSensor != null)
                    {
                        UninitializeKinectSensor(this._kinectSensor);
                        this._kinectSensor = null;
                    }

                    this._kinectSensor = value;

                    if (this._kinectSensor != null)
                    {
                        if (this._kinectSensor.Status == KinectStatus.Connected)
                        {
                            InitializeKinectSensor(this._kinectSensor);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
