using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace TimeLapsePhotography
{
    /// <summary>
    /// VideoPostWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoPostWindow : Window
    {
        public List<Photo> photos;
        string rootPath;
        
        public VideoPostWindow()
        {
            InitializeComponent();
            this.Closing += Window_Closing;

            rootPath = @"D:\picture\";   //默认路径，打开就获取默认路径的图片
            
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                GetAllImagePath(rootPath);
                lstImgs.ItemsSource = photos;
            }
            else
            {
                GetAllImagePath(rootPath);
                lstImgs.ItemsSource = photos;
            }
        }
        

        /// <summary>
        /// 选择新路径，获取新路径文件夹里面的图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();

            fbd.ShowDialog();

            rootPath = fbd.SelectedPath;
            
            if (rootPath != "")
            {
                GetAllImagePath(rootPath);
                lstImgs.ItemsSource = photos;
            }
            else
            {

            }

        }

        /// <summary>
        /// 将该文件夹下的图片导出成视频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = Directory.GetFiles(rootPath, "*.jpg");
                int count = files.Count();
                int fps = 12;   //帧频
                int i = 0;
                string picname = files[0];
                Bitmap map = new Bitmap(picname);
                int frameW = map.Width;
                int frameH = map.Height;
                string videoname = "./video/out.mp4";
                VideoWriter writer = new VideoWriter(videoname, VideoWriter.Fourcc('X', 'V', 'I', 'D'), fps, new System.Drawing.Size(frameW, frameH), true);
                map.Dispose();
                while (i < count)
                {
                    picname = files[i];
                    var img = CvInvoke.Imread(picname, ImreadModes.Color);
                    writer.Write(img);
                    i++;
                }
                writer.Dispose();

                MessageBox.Show("导出视频成功！", "VideoPost", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }


        public void GetAllImagePath(string path)
        {

            DirectoryInfo di = new DirectoryInfo(path);
            photos = new List<Photo>();

            FileInfo[] files = di.GetFiles("*.*", SearchOption.AllDirectories);
            
            if (files != null && files.Length > 0)
            {
                foreach (var file in files)
                {
                    if (file.Extension == (".jpg") ||
                        file.Extension == (".png") ||
                        file.Extension == (".bmp") ||
                        file.Extension == (".gif"))
                    {
                        photos.Add(new Photo()
                        {
                            FullPath = file.FullName,
                            PhotoName = file.Name
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            photos.RemoveAll(p => p.PhotoName != null);
        }
    }
}
