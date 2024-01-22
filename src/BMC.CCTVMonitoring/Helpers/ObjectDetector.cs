using BMC.CCTVMonitoring.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        Yolov7 yolo { set; get; }
        public ObjectDetector()
        {
            Setup();
        }
        public List<string> GetLabels()
        {
            return yolo?.GetLabels();
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
                yolo = new Yolov7("./Assets/yolov7-tiny_640x640.onnx", false);
                
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

        public void Detect(byte[] ImageBytes, List<string> Filter, int No = 0)
        {
            if (!IsReady) return;
            if(ImageBytes is null) return;
            var ms = new MemoryStream(ImageBytes);
            var image = Image.FromStream(ms);
            var predictions = yolo.Predict(image);  // now you can use numsharp to parse output data like this : var ret = yolo.Predict(image,useNumpy:true);
                                                    // draw box
            var removed = predictions.Where(x => !Filter.Contains(x.Label.Name)).ToList();
            foreach (var item in removed)
            {
                predictions.Remove(item);
            }
            /*
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
            */
            var desc = string.Empty;
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.CompositingQuality = CompositingQuality.Default;
                graphics.SmoothingMode = SmoothingMode.Default;
                graphics.InterpolationMode = InterpolationMode.Default;

                // Define Text Options
                Font drawFont = new Font("consolas", 11, FontStyle.Regular);

                SolidBrush fontBrush = new SolidBrush(Color.Black);


                // Define BoundingBox options
                Pen pen = new Pen(Color.Yellow, 2.0f);
                SolidBrush colorBrush = new SolidBrush(Color.Yellow);
                var originalImageHeight = image.Height;
                var originalImageWidth = image.Width;
                foreach (var pred in predictions)
                {


                    var x = Math.Max(pred.Rectangle.X, 0);
                    var y = Math.Max(pred.Rectangle.Y, 0);
                    var width = Math.Min(originalImageWidth - x, pred.Rectangle.Width);
                    var height = Math.Min(originalImageHeight - y, pred.Rectangle.Height);

                    ////////////////////////////////////////////////////////////////////////////////////////////
                    // *** Note that the output is already scaled to the original image height and width. ***
                    ////////////////////////////////////////////////////////////////////////////////////////////

                    // Bounding Box Text
                    string text = $"{pred.Label.Name} [{pred.Score}]";
                    desc += text + ", ";
                    SizeF size = graphics.MeasureString(text, drawFont);
                    Point atPoint = new Point((int)x, (int)y - (int)size.Height - 1);
                    // Draw text on image 
                    graphics.FillRectangle(colorBrush, (int)x, (int)(y - size.Height - 1), (int)size.Width, (int)size.Height);
                    graphics.DrawString(text, drawFont, fontBrush, atPoint);
                    // Draw bounding box on image
                    graphics.DrawRectangle(pen, x, y, width, height);
                }
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
