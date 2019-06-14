上一篇博客还是前年的事了，哈哈
之前实验室给了我们两个网络摄像头，让我们开发一个具有人脸识别功能的监控软件，平时就安装在实验室里面“监控”我们自己。

![5cd11c633c708](https://i.loli.net/2019/05/07/5cd11c633c708.jpg)

玩着玩着，今天突发奇想，“为啥不用它拍个延时摄影的视频呢？”
实验室在15楼，风景吧还将就（下面这样），写点小代码拍个视频还是很容易的嘛（实际上遇到一些小麻烦，呵呵）。。。说干就干

![5cd11c5368f84](https://i.loli.net/2019/05/07/5cd11c5368f84.jpg)

首先，这个摄像头是中视视讯的一款网络摄像头，参数如下（将就用吧，监控是没有问题的）：

![5cd11c371ab04](https://i.loli.net/2019/05/07/5cd11c371ab04.jpg)

然后就是如何完成这个工作呢？大致步骤分为两步：

1. 编写程序，利用摄像头拍摄照片

2. 将照片拼接成一个视频

第2个步骤可以自己编写程序将图片拼接成一个视频，也可以用视频剪辑软件完成。

无论如何，首先要获取图片，我用WPF写了一个最简单的程序（连MVVM都不用）。

调用摄像头还是很简单的，这些网络摄像头都支持rtsp协议，设置好IP地址就可以直接获取图像，Emgu.CV里面有函数可以直接用的。

**1.项目目录列表**

实际上，这是一个小项目，需要的代码不多，就是两个窗口，MainWindow和VideoPostWindow，还有一个Photo对象，BitmapSourceConvert类。需要依赖的包就是Emgu.CV，据我所知，只有4.0.1版本的才支持VideoCapture类，低版本的似乎支持不够。（直接抄这里的代码有可能出错哦，完整项目下载链接在下面。。。）

![5cd11c1a70ce0](https://i.loli.net/2019/05/07/5cd11c1a70ce0.png)

**2.获取摄像头数据**
Emgu.CV支持rtsp协议，通过这个协议，可以直接访问摄像头的IP地址，从而获取图像。操作还是比较简单的，但是其中也有很多坑。
首先，在页面上添加一个image控件。

```
    <!--视频-->
    <Viewbox Grid.Column="0" Name="video" Stretch="Uniform">
        <Image Name="myimage"/>
    </Viewbox>
```

然后，在后台代码中新建一个VideoCapture对象，并设置IP地址。还要新建一个私有的静态Mat对象frame，它用来存放当前图像帧。

```
VideoCapture _capture = new VideoCapture("rtsp://192.168.1.122:554/mpeg4");   //设置摄像头IP地址

private static Mat frame = new Mat();    //当前帧
```

接下来，写一个方法来将获取的图像数据“绑定”到Image控件上。

```
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

            //frame.Dispose();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
    //Thread.Sleep(100);
}
```

BtimapSourceConvert类就是在此用来将frame转换成image控件的source，这样前台就能显示图像了。

**3.截图**

延时摄影不需要每一帧都保存下来，不然就是拍视频了么。只需要每隔一段时间保留一帧图像，持续一段时间，后期处理成视频就好了。

我的设定是每10秒拍摄一张，而视频帧率是12fps。

定时器的相关代码分布在几个方法中，这里就不粘贴了，就讲讲“每10秒”是如何控制的。实际上，代码中就利用了一个判断，如果秒的个位是0就调用save_photo()函数，这样完成了时间间隔。如果把0改为00，时间间隔就是一分钟。

```
time = DateTime.Now.ToString("HH:mm:ss");//获取时分秒

//每10秒拍摄一次
if (time.EndsWith("0"))

{

    Save_photo();

}
```

定时器做好了，接下来就是保存截图的功能了，具体由save_photo()函数完成。

```
private static Mat frame_saved = new Mat();   //当前帧的副本

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
```

这里是一个大坑，我栽进去了，幸好有了解决办法。

原本我想调用_capture.QueryFrame()函数直接获取一帧图像，理论上看起来没问题嘛，实际运行的时候画面很不稳定，每次截图时画面就卡帧，最多运行5、6分钟程序就会崩溃，而且还挺占内存的。

主要原因就是这个QueryFrame()相当于去原来获取视频流的线程（或者说内存）里取图像帧，会占用原来的线程，还容易出现frame被销毁后再去取图像而造成的内存损坏bug。可以查查Emgu.CV的VideoCapture类，讲的也不怎么详细。

我的解决办法就是将frame定义为一个私有静态变量，再定义一个私有静态变量frame_saved来存放当前帧的备份。利用Clone()函数，frame会被拷贝到另一块内存中，即frame_saved，这样就不会出现内存损坏问题了。

```
frame_saved = frame.Clone();
//frame_saved = frame;   共享内存
//frame.CopyTo(frame_saved);   共享内存
```

MainWindow的实现效果（关了灯都一样）：

![5cd11d2c6af36](https://i.loli.net/2019/05/07/5cd11d2c6af36.png)

**4.合成视频**

合成视频功能放到了一个新窗口中，通过MainWindow中的一个Button打开。

“选择图片”和“图片列表”这里不说了，就讲讲合成。

![5cd11bec8299d](https://i.loli.net/2019/05/07/5cd11bec8299d.png)

合成功能是在“合成视频”这个按钮的点击事件中完成的。Emgu.CV中有一个VideoWriter类，可以将图片合成视频，参数分别是视频名字、编码器、帧频、分辨率和颜色（是否彩色）。具体可以查询Emgu.CV 4.0.1版本，老版本的不太兼容。

```
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
```

**5.效果**

对了还要安装一下摄像头嘛，找个好角度

![5cd12a5fb90d3](https://i.loli.net/2019/05/07/5cd12a5fb90d3.jpg)

生成视频过后可以用视频编辑软件稍微处理一下，还是可以看的嘛。

![5cd1246e012f4](https://i.loli.net/2019/05/07/5cd1246e012f4.jpg)

视频可以在这里看：https://weibo.com/5692989208/Ht5h2xsOW

**6.结论**

![5cd121e904e81](https://i.loli.net/2019/05/07/5cd121e904e81.png)

经过几次调试，程序总算能够稳定运行，资源占用也不多，有些时候是因为摄像头自己不淡定了，造成程序出问题，但算还好，能用。

接下来做啥呢。

完整项目资源：

链接: https://pan.baidu.com/s/1lIXIlXvm7mMl2YA3w8Wh4A 提取码: 5gh6 

*经验不足，水平有限，有错误或者可以优化的地方欢迎大家指证。*
