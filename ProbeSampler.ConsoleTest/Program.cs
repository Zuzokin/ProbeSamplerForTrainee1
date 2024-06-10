using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ProbeSampler.Core.Entities;
using ProbeSampler.Core.Enums;
using ProbeSampler.Core.Services.Camera;
using ProbeSampler.Core.Services.Contract;
using ProbeSampler.Core.Services.Processing.Core;
using System.Drawing;

namespace ProbeSampler.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new TestSample();
        }
    }

    internal class TestSample
    {
        private BufferedProcessor<ColoredRect> bufferedRectangles;
        Mat frame;
        Mat processResult;
        Mat processResult322;
        ICamera camera;
        object locker = new();
        BoundedQueue<Mat> queue = new(maxSize: 10);
        CancellationTokenSource cts = new();
        int matHeight, matWidth;

        int MatHeight
        {
            get => matHeight;
            set
            {
                if (matHeight != value)
                {
                    matHeight = value;
                }
            }
        }

        int MatWidth
        {
            get => matWidth;
            set
            {
                if (matWidth != value)
                {
                    matWidth = value;
                }
            }
        }

        public TestSample()
        {
            // Статическая картинка
            camera = new DebugStaticImageCamera();
            // camera = new RtspCameraService();
            camera.FrameReceived += Camera_FrameReceived;
            frame = new Mat();
            processResult = new Mat();
            processResult322 = new Mat();
            Task.Run(() => ProcessQueue(cts.Token));
            Start();
        }

        private void Start()
        {
            // camera.ConnectAsync("rtsp://root:951753@192.168.88.248/axis-media/media.amp").ConfigureAwait(false);

            camera.ConnectAsync("C:\\Users\\nikit\\Desktop\\probnik-real-photos\\image_2023-07-27_14-54-52.png").ConfigureAwait(false);

            while (true)
            {
                if (!frame.IsEmpty)
                {
                    queue.Enqueue(frame.Clone());
                    // Отображаем результаты
                    CvInvoke.Imshow("Original", frame);

                    lock (locker)
                    {
                        if (!processResult.IsEmpty)
                        {
                            CvInvoke.Imshow("Result", processResult);
                        }
                    }
                }

                // CvInvoke.Imshow("Edges", edges);
                if (CvInvoke.WaitKey(30) == 27)
                {
                    break;  // Если нажата клавиша Esc - выходим
                }

                Thread.Sleep(33);
            }

            while (true)
            {
                CvInvoke.Imshow("Result322", processResult322);
                if (CvInvoke.WaitKey(30) == 27)
                {
                    break;  // Если нажата клавиша Esc - выходим
                }

                Thread.Sleep(330);
            }

            camera.Dispose();
        }

        private void Camera_FrameReceived(object? sender, FrameEventArgs e)
        {
            frame = e.Frame;
        }

        private void ProcessQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var image))
                {
                    // MedianBlur(image);
                    // MorphologyEx(image);
                    // MorphologyEx(image, value: 3);
                    // MedianBlur(image);
                    // GetGradient(image);
                    // MorphologyEx(image, value: 3, iterations: 5, morphType: MorphOp.Close);
                    // GaussianBlur(image);
                    // MorphologyEx(image, value: 2, iterations: 8);

                    // Преобразование изображения в оттенки серого
                    var grayImage = new Mat();
                    CvInvoke.CvtColor(image, grayImage, ColorConversion.Bgr2Gray);

                    // Применение порогового фильтра для выделения контуров
                    double thresholdValue = 125;
                    double maxThresholdValue = 255;
                    /*Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle,
                                                        new Size(5, 5),
                                                        new Point(-1, -1));
                    CvInvoke.MorphologyEx(grayImage, grayImage, MorphOp.Open, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());*/
                    CvInvoke.Threshold(grayImage, grayImage, thresholdValue, maxThresholdValue, ThresholdType.Binary);
                    processResult322 = image.Clone();

                    var blobs = FindRectangles(image, searchType: SearchType.Blob);
                    var contours = FindRectangles(image, searchType: SearchType.Contour);

                    var rects = FilterRectangles(blobs.Concat(contours));

                    var queueFilteredRects = AveragingRectangles(rects);
                    image = grayImage;

                    foreach (ColoredRect coloredRect in queueFilteredRects)
                    {
                        MCvScalar drawColor;
                        drawColor = new MCvScalar(0, 165, 255); // Orange
                        CvInvoke.Rectangle(processResult322, coloredRect.Rect, drawColor, 2);
                    }

                    lock (locker)
                    {
                        processResult.Dispose();
                        processResult = image.Clone();
                    }

                    image.Dispose();
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        #region CV methods

        private void MedianBlur(Mat src, int value = 5)
        {
            CvInvoke.MedianBlur(src, src, (value % 2 != 1) ? ++value : value);
        }

        private void MorphologyEx(Mat src, double value = 4, int iterations = 1, MorphOp morphType = MorphOp.Dilate)
        {
            if (value > 0)
            {
                using (Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size((int)value, (int)value), new Point(-1, -1)))
                {
                    CvInvoke.MorphologyEx(
                        src,
                        src,
                        morphType,
                        structuringElement,
                        new Point(-1, -1),
                        (iterations > 0) ? iterations : 1,
                        BorderType.Constant,
                        CvInvoke.MorphologyDefaultBorderValue
                        );
                }
            }
        }

        private void GetGradient(Mat src)
        {
            using (var preparedSrc = new Mat())
            {
                CvInvoke.CvtColor(src, preparedSrc, ColorConversion.Bgr2Gray);
                preparedSrc.ConvertTo(preparedSrc, DepthType.Cv32F, 1.0 / 255);
                // Рассчёт матрицы в зависимости от размера изображения
                Mat Derivative(Int32 dx, Int32 dy)
                {
                    Int32 resolution = preparedSrc.Width * preparedSrc.Height;
                    Int32 kernelSize =
                        resolution < 1280 * 1280 ? 3 : // Больше изображение --> больше матрица
                        resolution < 2000 * 2000 ? 5 :
                        resolution < 3000 * 3000 ? 9 :
                                                   15;
                    Single kernelFactor = kernelSize == 3 ? 1 : 2; // Компенсация плохого контраста на больших изображениях
                    using (Mat kernelRows = new Mat())
                    using (Mat kernelColumns = new Mat())
                    {
                        CvInvoke.GetDerivKernels(kernelRows, kernelColumns, dx, dy, kernelSize, normalize: true);
                        using (Mat multipliedKernelRows = kernelRows * kernelFactor)
                        using (Mat multipliedKernelColumns = kernelColumns * kernelFactor)
                        {
                            CvInvoke.SepFilter2D(preparedSrc, preparedSrc, DepthType.Cv32F, multipliedKernelRows, multipliedKernelColumns, new Point(-1, -1));
                            return preparedSrc;
                        }
                    }
                }

                using (Mat gradX = Derivative(1, 0))
                using (Mat gradY = Derivative(0, 1))
                using (Mat magnitude = new Mat())
                using (Mat result = new Mat())
                {
                    CvInvoke.CartToPolar(gradX, gradY, magnitude, result, false);
                    result.ConvertTo(src, DepthType.Cv8U, 11, 1);
                    // The commented line below needs further implementation if you want to use it
                    // lock(locker)
                    // win.Image = src.Resize(new OpenCvSharp.Size(640, 320)).Threshold(0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
                }
            }
        }

        private void GaussianBlur(Mat src, double value = 5)
        {
            if (value > 0)
            {
                CvInvoke.GaussianBlur(src, src, new Size((int)value, (int)value), value);
            }
        }

        private IEnumerable<ColoredRect> FindRectangles(Mat src, SearchType searchType, double thresh = 0, double maxVal = 255)
        {
            IEnumerable<ColoredRect> FromBlobs(Mat src, ThresholdType type, double thresh = 0, double maxVal = 255)
            {
                List<ColoredRect> rectangles = new List<ColoredRect>();
                if (src == null || src.IsEmpty)
                {
                    return rectangles;
                }

                using (Mat gray = src.Clone())
                using (Mat binary = new Mat())
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                using (Mat hierarchy = new Mat())
                {
                    if (src.NumberOfChannels != 1)
                    {
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    }

                    CvInvoke.Threshold(gray, binary, thresh, maxVal, type | ThresholdType.Otsu);
                    CvInvoke.FindContours(binary, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                    for (int i = 0; i < contours.Size; i++)
                    {
                        Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                        MCvScalar color = (type == ThresholdType.BinaryInv) ? new MCvScalar(0, 255, 0) : new MCvScalar(0, 0, 255);
                        rectangles.Add(new ColoredRect(rect, color));
                    }
                }

                return rectangles;
            }

            IEnumerable<ColoredRect> FromContours(Mat src, ThresholdType type, double thresh = 0, double maxVal = 255)
            {
                List<ColoredRect> rectangles = new List<ColoredRect>();

                if (src == null || src.IsEmpty)
                {
                    return rectangles;
                }

                using (Mat gray = src.Clone())
                using (Mat binary = new Mat())
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                using (Mat hierarchy = new Mat())
                {
                    if (src.NumberOfChannels != 1)
                    {
                        CvInvoke.CvtColor(src, gray, ColorConversion.Bgr2Gray);
                    }

                    CvInvoke.Threshold(gray, binary, thresh, maxVal, type | ThresholdType.Otsu);
                    CvInvoke.FindContours(binary, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxTc89L1);

                    for (int i = 0; i < contours.Size; i++)
                    {
                        using (VectorOfPoint approxContour = new VectorOfPoint())
                        {
                            CvInvoke.ApproxPolyDP(contours[i], approxContour, 3, true);

                            Rectangle rect = CvInvoke.BoundingRectangle(approxContour);

                            rectangles.Add(new ColoredRect(rect, new MCvScalar(255, 0, 0))); // Blue color
                        }
                    }
                }

                return rectangles;
            }

            List<ColoredRect> rectangles = new List<ColoredRect>();

            if (src == null || src.IsEmpty)
            {
                return rectangles;
            }

            switch (searchType)
            {
                case SearchType.Blob:
                    rectangles.AddRange(FromBlobs(src, ThresholdType.Binary, thresh, maxVal));
                    rectangles.AddRange(FromBlobs(src, ThresholdType.BinaryInv, thresh, maxVal));
                    break;

                case SearchType.Contour:
                    rectangles.AddRange(FromContours(src, ThresholdType.Binary, thresh, maxVal));
                    rectangles.AddRange(FromContours(src, ThresholdType.BinaryInv, thresh, maxVal));
                    break;
            }

            return rectangles;
        }

        private IEnumerable<ColoredRect> FilterRectangles(IEnumerable<ColoredRect> rects, double minArea = 50000, double maxArea = 1500000)
        {
            if (minArea <= 0)
            {
                minArea = 50000;
            }

            if (maxArea <= 0)
            {
                maxArea = 150000;
            }

            rects = rects.Where(p =>
            {
                var s = p.Rect.Width * p.Rect.Height;
                return s >= minArea && s <= maxArea;
            }).ToList();
            /*            if (minArea > 0)
                            rects = rects.Where(p => (p.Rect.Width * p.Rect.Height) >= minArea).ToList();
                        if (maxArea > 0)
                            rects = rects.Where(p => (p.Rect.Width * p.Rect.Height) <= maxArea).ToList();*/
            return rects;
        }

        private IEnumerable<ColoredRect> AveragingRectangles(IEnumerable<ColoredRect> rects)
        {
            List<ColoredRect> selections = new List<ColoredRect>();
            double deltaWidth = MatWidth * 0.05;
            double deltaHeight = MatHeight * 0.05;

            foreach (ColoredRect currentRect in rects)
            {
                if (!selections.Any())
                {
                    selections.Add(currentRect);
                    continue;
                }

                var closeRects = selections.Where(p =>
                    (p.Rect.Left - deltaWidth) < currentRect.Rect.Left &&
                    (p.Rect.Left + deltaWidth) > currentRect.Rect.Left &&
                    (p.Rect.Right - deltaWidth) < currentRect.Rect.Right &&
                    (p.Rect.Right + deltaWidth) > currentRect.Rect.Right &&
                    (p.Rect.Top - deltaHeight) < currentRect.Rect.Top &&
                    (p.Rect.Top + deltaHeight) > currentRect.Rect.Top &&
                    (p.Rect.Bottom - deltaHeight) < currentRect.Rect.Bottom &&
                    (p.Rect.Bottom + deltaHeight) > currentRect.Rect.Bottom).ToList();

                if (closeRects.Any())
                {
                    foreach (var closeRect in closeRects)
                    {
                        selections.Remove(closeRect);
                    }

                    int totalCount = closeRects.Count + 1;
                    double avgLeft = (closeRects.Sum(r => r.Rect.Left) + currentRect.Rect.Left) / totalCount;
                    double avgTop = (closeRects.Sum(r => r.Rect.Top) + currentRect.Rect.Top) / totalCount;
                    double avgWidth = (closeRects.Sum(r => r.Rect.Width) + currentRect.Rect.Width) / totalCount;
                    double avgHeight = (closeRects.Sum(r => r.Rect.Height) + currentRect.Rect.Height) / totalCount;

                    Rectangle averagedRect = new Rectangle((int)avgLeft, (int)avgTop, (int)avgWidth, (int)avgHeight);
                    selections.Add(new ColoredRect(averagedRect, currentRect.Color));
                }
                else
                {
                    selections.Add(currentRect);
                }
            }

            return selections;
        }

        #endregion
    }
}
