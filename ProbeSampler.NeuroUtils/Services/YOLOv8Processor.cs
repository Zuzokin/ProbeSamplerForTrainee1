using Compunet.YoloV8;
using Compunet.YoloV8.Data;
using Compunet.YoloV8.Plotting;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.ML.OnnxRuntime;
using System.Drawing;

namespace ProbeSampler.NeuroUtils.Services
{
    public class YOLOv8Processor : ImageProcessor, IProvideBoxes, IEnableLogger
    {
        private readonly double scale;
        private YoloV8? model;
        private readonly Task? processTask;
        private readonly BoundedQueue<Mat> queue = new(maxSize: 10);
        private readonly CancellationTokenSource cts = new();
        private readonly DetectionAccumulator detectionAccumulator = new();

        public YOLOv8Processor(string yoloOnnxPath, double scale = 0, float confidence = 0)
        {
            this.scale = scale;
            try
            {
                var options = new SessionOptions();
                options.AppendExecutionProvider_CUDA();
                model = new YoloV8(yoloOnnxPath, options);
            }
            catch (Exception ex)
            {
                this.Log().Warn($"Can't initialize YOLO with CUDA: {ex}");
                this.Log().Warn("Using CPU inference");
                model = new YoloV8(yoloOnnxPath);
            }

            model.Parameters.Confidence = confidence;
            processTask = Task.Run(() => ProcessQueue(cts.Token));
        }

        protected override Mat OnImageReceived(Mat image)
        {
            AddImage(image);
            var result = detectionAccumulator.GetResult();
            if (result != null && result.Boxes.Count > 0)
            {
                image = result.PlotImage(image, scale);
            }

            return image;
        }

        private void AddImage(Mat image)
        {
            queue.Enqueue(image);
        }

        private void ProcessQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (queue.TryDequeue(out Mat image))
                {
                    Image<Bgr, byte> afterImage = image.ToImage<Bgr, byte>();

                    byte[] afterbytes = afterImage.ToJpegData();
                    var result = model?.Detect(afterbytes);
                    if (result != null)
                    {
                        detectionAccumulator.UpdateResult(result);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        //public override void Dispose()
        //{
        //    base.Dispose();
        //    GC.SuppressFinalize(this);

        //    cts?.Cancel();
        //    processTask?.Wait();
        //    cts?.Dispose();
        //    model = null;
        //    //model.Dispose();
        //}
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                base.Dispose();

                using (cts)
                {
                    cts?.Cancel();
                    processTask?.Wait();
                }
                
                model?.Dispose();
                model = null;
            }
        }

        public IEnumerable<BoundingBox> GetDetections()
        {
            var result = detectionAccumulator.GetResult();
            if (result != null)
            {
                // Преобразование результатов YOLOv8 в формат BoundingBox
                return result.Boxes
                    .Where(box => box.Class.Name == "body")
                    .Select(box => new BoundingBox
                    {
                        Box = new Rectangle(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height),
                        Confidence = box.Confidence,
                        Label = box.Class.Name,
                        Width = box.Bounds.Width,
                        Height = box.Bounds.Height,
                    });
            }

            return Enumerable.Empty<BoundingBox>();
        }

        class DetectionAccumulator
        {
            private readonly object lockobj = new();
            private IDetectionResult? current;

            public void UpdateResult(IDetectionResult detectionResult)
            {
                lock (lockobj)
                {
                    current = detectionResult;
                }
            }

            public IDetectionResult? GetResult()
            {
                lock (lockobj)
                {
                    return current;
                }
            }
        }
    }
}
