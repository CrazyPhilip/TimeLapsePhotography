using System;
using System.Windows;
using System.Drawing.Imaging;
using System.Windows.Threading;
using Emgu.CV;

namespace TimeLapsePhotography
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        VideoCapture _capture = new VideoCapture("rtsp://192.168.1.122:554/mpeg4");   //新建一个VideoCapture对象，并设置摄像头IP地址
        private static Mat frame = new Mat();    //当前帧
        private static Mat frame_saved = new Mat();   //当前帧的副本
        private object lockObj = new object();  //好像没用
        public int selectedDeviceIndex = 0;
        private DispatcherTimer ShowTimer;    //定时器
        private static int ShotNumber = 0;     //计数器，拍摄数量，每次打开就置0
        private static bool flag; //摄像头是否打开，是否接收图像
        string path = @"D:\picture\";   //截图保存路径
        private static string date;   //日期
        private static string time;   //时间

        /// <summary>
        /// 窗口初始化，启动定时器，并添加窗口关闭事件
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += Window_Closing;
            
            ShowTime();    
            ShowTimer = new DispatcherTimer();
            ShowTimer.Tick += new EventHandler(ShowCurTimer);   //Timer一直获取当前时间
            ShowTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            ShowTimer.Start();
        }

        /// <summary>
        /// 打开摄像头按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrunOn_Click(object sender, RoutedEventArgs e)
        {
            _capture.ImageGrabbed += capture_ImageGrabbed;
            _capture.Start();
            flag = true;   //摄像头开启状态
        }

        /// <summary>
        /// 关闭摄像头按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrunOff_Click(object sender, RoutedEventArgs e)
        {
            //_capture.Dispose(); //停止关闭
            _capture.Stop();
            flag = false;  //摄像头关闭状态
        }


        /// <summary>
        /// 截图按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shot_Click(object sender, RoutedEventArgs e)
        {
            Save_photo();            
        }
        
        public void ShowCurTimer(object sender, EventArgs e)
        {
            ShowTime();
        }

        /// <summary>
        /// ShowTime方法
        /// </summary>
        private void ShowTime()
        {
            date = DateTime.Now.ToString("yyyy/MM/dd");
            time = DateTime.Now.ToString("HH:mm:ss");
            //获得年月日
            this.tbDateText.Text = date;   //yyyy/MM/dd
            //获得时分秒
            this.tbTimeText.Text = time;

            //每10秒拍摄一次
            if (time.EndsWith("0"))
            {
                Save_photo();
            }

        }
        
        public void Save_photo()
        {
            if (flag == false)
                return;
            try
            {
                //Mat frame_saved = new Mat();
                string fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ff") + ".jpg";
                //frame_saved = _capture.QueryFrame();
                //frame_saved = _capture.QuerySmallFrame();
                frame_saved = frame.Clone();    //将当前帧克隆到frame_saved
                if (frame_saved != null)
                {
                    frame_saved.Bitmap.Save(path + fileName, ImageFormat.Jpeg);  //保存到文件
                    ShotNumber += 1;   //计数器+1
                    this.tbNums.Text = ShotNumber.ToString();   //显示到对应的textblock
                    frame_saved.Dispose();   //释放资源
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("是否要关闭？", "TimeLapse", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (_capture != null)
                {
                    //_capture.Dispose(); //停止关闭
                    _capture.Stop();
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = false;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 打开VideoPost窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoPost_Click(object sender, RoutedEventArgs e)
        {
            VideoPostWindow VPWindow = new VideoPostWindow();
            VPWindow.Show();
        }

        
        private void capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                //Mat frame = new Mat();
                if (_capture != null)
                {
                    if (!_capture.Retrieve(frame))
                    {
                        frame.Dispose();
                        return;
                    }
                    if (frame.IsEmpty)
                        return;
                    myimage.Dispatcher.Invoke(new Action(() =>
                    {
                        myimage.Source = BitmapSourceConvert.ToBitmapSource(frame);
                    }));

                    //显示图片 可以使用Emgu CV 提供的 ImageBox显示视频, 也可以转成 BitmapSource显示。
                    //frame.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //Thread.Sleep(100);
        }
    }
}
