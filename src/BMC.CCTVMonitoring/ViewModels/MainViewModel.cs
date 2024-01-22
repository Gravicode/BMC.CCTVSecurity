﻿using Avalonia.Threading;
using BMC.CCTVMonitoring.Helpers;
using BMC.CCTVMonitoring.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BMC.CCTVMonitoring.ViewModels;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
public class LabelItem
{
    public string Name { get; set; }
    public bool Selected { get; set; } = false;
}
public class CCTVScreen:ObservableObject
{
    private int _No ;
    public int No
    {
        get => _No;
        set => SetProperty(ref _No, value);
    }

    private string _Url ;
    public string Url
    {
        get => _Url;
        set => SetProperty(ref _Url, value);
    }

    private Bitmap _Content ;
    public Bitmap Content
    {
        get => _Content;
        set => SetProperty(ref _Content, value);
    }

    private string _Info ;
    public string Info
    {
        get => _Info;
        set => SetProperty(ref _Info, value);
    }
   
   
}
public partial class MainViewModel : ViewModelBase
{
    private string _status = "Welcome to BMC CCTV Monitoring";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
 
    private bool _PatrolMode = true;
    public bool PatrolMode
    {
        get => _PatrolMode;
        set => SetProperty(ref _PatrolMode, value);
    }

    private bool _PushToCloud = false;
    public bool PushToCloud
    {
        get => _PushToCloud;
        set => SetProperty(ref _PushToCloud, value);
    }
    
    private bool _SaveToDisk = true;
    public bool SaveToDisk
    {
        get => _SaveToDisk;
        set => SetProperty(ref _SaveToDisk, value);
    }

    private bool _PlaySound = true;
    public bool PlaySound
    {
        get => _PlaySound;
        set => SetProperty(ref _PlaySound, value);
    }
    public bool IsSoundPlaying { get; set; }
    public ObjectDetector detector { set; get; }
    public DataConfig dataConfig { set; get; }
    public ObservableCollection<CCTVScreen> Screen { get; set; }
    public ObservableCollection<LabelItem> Labels { get; set; }
    public string Greeting => "Welcome to Avalonia!";
    System.Timers.Timer timer { set; get; }
    HttpClient client { set; get; }
    ObjectContext context { set; get; }
    StorageObjectService storageSvc { set; get; }

    int Delay = 5000;
    public MainViewModel()
    {
        Setup();
    }
    void Setup()
    {
        detector = new ObjectDetector();
        
        dataConfig = new DataConfig();
        dataConfig.Init();
        Screen = new ();
        var count = 0;
        foreach (var item in dataConfig.CCTVURL)
        {
            Screen.Add(new CCTVScreen() { No = count++, Content = null, Url = item });
        }
        Delay = dataConfig.CaptureIntervalSecs*1000;
        timer = new(interval: Delay);
        timer.Elapsed += (sender, e) => CaptureCCTV();

        client = new();
        context = new ObjectContext();
        context.Database.EnsureCreated();
        detector.OnDetectedObject += async(Object? a, DetectedObject b) => {
            var item = Screen.Where(x => x.No == b.No).FirstOrDefault();
            if (item != null && b.Predictions.Count>0)
            {
                if (!IsSoundPlaying && PlaySound)
                {
                    IsSoundPlaying = true;
                    try
                    {
                        await Sound.PlaySound("./Assets/monster.mp3");
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync("sound error..");

                    }
                    finally
                    {
                        IsSoundPlaying = false;

                    }
                }
                var FileUrl = item.Url;

                if (SaveToDisk)
                {
                    //save to disk
                    var fname = $"local_{b.DetectedTime.ToString("yyyyMMdd")}_{b.DetectedTime.ToString("HHmmss")}_CCTV{b.No}.jpg";
                    var skbmp = (b.AnotatedImage as System.Drawing.Bitmap).ToSKBitmap();
                    var path = dataConfig.SnapshotDir + "/" + fname;
                    await ImageHelper.SaveToDisk(skbmp, path);
                    FileUrl = path;
                    //b.AnotatedImage.Save(dataConfig.SnapshotDir +"/"+ fname,ImageFormat.Jpeg);
                }
                if (PushToCloud)
                {
                    //push to cloud
                    var fname = $"cloud_{b.DetectedTime.ToString("yyyyMMdd")}_{b.DetectedTime.ToString("HHmmss")}_CCTV{b.No}.png";
                    var bytes = ImageHelper.ImageToBytes(b.AnotatedImage);
                    await storageSvc.UploadFile(fname, bytes);
                    FileUrl = fname;
                }
                Dispatcher.UIThread.Post(async() => {
                    item.Content = b.AnotatedImage.ConvertToAvaloniaBitmap();
                    item.Info = "Update: " + b.DetectedTime.ToString("dd/MM/yy HH:mm:ss");
                    Console.WriteLine("Write to DB");
                    var obj = new ObjectDetect() { CCTVNo = item.No, DetectedTime = b.DetectedTime, Url = FileUrl, ObjectCount = b.Predictions.Count };
                    context.ObjectDetects.Add(obj);
                    context.SaveChanges();
                   
                });
                
            }
        };
        //labels
        Labels = new();
        
        var labels = detector.GetLabels().Order().ToList();
        labels.ForEach(x => {
            Labels.Add(new LabelItem() { Name = x, Selected = (x == "person") });
        });

        if (!Directory.Exists(dataConfig.SnapshotDir))
        {
            Directory.CreateDirectory(dataConfig.SnapshotDir);
        }
        //storage
        var setting = new StorageSetting() { };
        setting.Bucket = dataConfig.StorageBucket;
        setting.SecretKey = dataConfig.StorageSecret;
        setting.AccessKey = dataConfig.StorageAccess;
        storageSvc = new(setting);
    }
    void CaptureCCTV()
    {
        var filters = Labels.Where(x=>x.Selected).Select(x => x.Name).ToList();
        Parallel.For(0, dataConfig.CCTVCount,
                  async index => {
                      try
                      {
                          await Console.Out.WriteLineAsync("Perform capture");
                          var url = dataConfig.CCTVURL[index];
                          var bytes = await client.GetByteArrayAsync(url);
                          if (bytes != null)
                          {
                              await Console.Out.WriteLineAsync("Perform inference");
                              detector.Detect(bytes,filters,index);
                          }
                      }
                      catch (Exception ex)
                      {
                          await Console.Out.WriteLineAsync("Capture error: " +ex);
                      }
                   
                  });
    }
    public void Start()
    {
        timer.Start();
        this.Status = "App is running";

    }
    public void Stop()
    {
        timer.Stop();
        this.Status = "App has been stopped";
    }
}
