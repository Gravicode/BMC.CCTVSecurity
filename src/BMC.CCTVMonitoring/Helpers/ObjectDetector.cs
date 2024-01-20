using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yolov7net;

namespace BMC.CCTVMonitoring.Helpers
{
    public class DetectedObject:EventArgs
    {
        public int No { get; set; }
        public DateTime DetectedTime { set; get; } = DateTime.Now;
        public List<YoloPrediction> Predictions { get; set; }
        public Image AnotatedImage { get; set; }
    }
    public class ObjectDetector
    {
        const int MaxItem = 100;
        ConcurrentQueue<DetectedObject> ListDetections = new ConcurrentQueue<DetectedObject>();
        public EventHandler<DetectedObject> OnDetectedObject;
        bool IsReady { set; get; }
        Yolov8 yolo { set; get; }
        public ObjectDetector()
        {
            Setup();
        }

        void Reset()
        {
            ListDetections.Clear();
        }
        void Setup()
        {
            try
            {
                // init Yolov8 with onnx (include nms results)file path
                yolo = new Yolov8("./Assets/yolov7-tiny_640x640.onnx", true);
                // setup labels of onnx model 
                yolo.SetupYoloDefaultLabels();   // use custom trained model should use your labels like: yolo.SetupLabels(string[] labels)
                IsReady = true;
            }
            catch (Exception ex)
            {
                IsReady = false;
                Console.WriteLine("setup failed: "+ ex);
            }
         
        }

        public List<DetectedObject> GetHistory()
        {
            return ListDetections.Reverse().ToList();
        }

        public void Detect(byte[] ImageBytes, int No = 0)
        {
            if (!IsReady) return;
            if(ImageBytes is null) return;
            var ms = new MemoryStream(ImageBytes);
            using var image = Image.FromStream(ms);
            var predictions = yolo.Predict(image);  // now you can use numsharp to parse output data like this : var ret = yolo.Predict(image,useNumpy:true);
                                                    // draw box
            using var graphics = Graphics.FromImage(image);
           
            foreach (var prediction in predictions) // iterate predictions to draw results
            {
                double score = Math.Round(prediction.Score, 2);
                graphics.DrawRectangles(new Pen(prediction.Label.Color, 1), new[] { prediction.Rectangle });
                var (x, y) = (prediction.Rectangle.X - 3, prediction.Rectangle.Y - 23);
                graphics.DrawString($"{prediction.Label.Name} ({score})",
                                new Font("Consolas", 16, GraphicsUnit.Pixel), new SolidBrush(prediction.Label.Color),
                                new PointF(x, y));
            }
            var newObj = new DetectedObject() { DetectedTime = DateTime.Now, No = No, Predictions = predictions, AnotatedImage = image };
            ListDetections.Enqueue(newObj);
            //keep the max items = 100
            while (ListDetections.Count > MaxItem)
            {
                ListDetections.TryDequeue(out var firstItem);
            }
            OnDetectedObject?.Invoke(this, newObj);
        }
    }
}
