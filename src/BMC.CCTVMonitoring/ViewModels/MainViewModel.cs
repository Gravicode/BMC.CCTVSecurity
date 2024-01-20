using BMC.CCTVMonitoring.Helpers;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BMC.CCTVMonitoring.ViewModels;
public class CCTVScreen
{
    public int No { get; set; }
    public string Url { get; set; }
    public Image Content { get; set; }
}
public partial class MainViewModel : ViewModelBase
{
    public ObjectDetector detector { set; get; }
    public DataConfig dataConfig { set; get; }
    public List<CCTVScreen> Screen { get; set; }
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
        Screen = new List<CCTVScreen>();
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

    }
    public void Stop()
    {
        timer.Stop();

    }
}
