using BMC.CCTVMonitoring.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BMC.CCTVMonitoring.ViewModels;

public class LabelItem
{
    public string Name { get; set; }
    public bool Selected { get; set; } = false;
}
public class CCTVScreen
{
    public int No { get; set; }
    public string Url { get; set; }
    public Bitmap Content { get; set; }
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

    private bool _PlaySound = true;
    public bool PlaySound
    {
        get => _PlaySound;
        set => SetProperty(ref _PlaySound, value);
    }
    
    public ObjectDetector detector { set; get; }
    public DataConfig dataConfig { set; get; }
    public ObservableCollection<CCTVScreen> Screen { get; set; }
    public ObservableCollection<LabelItem> Labels { get; set; }
    public string Greeting => "Welcome to Avalonia!";
    System.Timers.Timer timer { set; get; }
    HttpClient client { set; get; }
    ObjectContext context { set; get; }

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
        timer = new(interval: Delay);
        timer.Elapsed += (sender, e) => CaptureCCTV();

        client = new();
        context = new ObjectContext();
        context.Database.EnsureCreated();
        detector.OnDetectedObject += (Object? a, DetectedObject b) => {
            var item = Screen.Where(x => x.No == b.No).FirstOrDefault();
            if (item != null)
            {
                item.Content = b.AnotatedImage;

                Console.WriteLine("Write to DB");
                var obj = new ObjectDetect() { CCTVNo = item.No, DetectedTime = b.DetectedTime, Url = item.Url, ObjectCount = b.Predictions.Count };
                context.ObjectDetects.Add(obj);
                context.SaveChanges();
            }
        };
        //labels
        Labels = new();
        
        var labels = detector.GetLabels();
        labels.ForEach(x => {
            Labels.Add(new LabelItem() { Name = x, Selected = (x == "person") });
        });
    }
    void CaptureCCTV()
    {
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
                              detector.Detect(bytes, index);
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
