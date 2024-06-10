using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ProbeSampler.Core.Services.Processing.Core;
using System.Drawing;

namespace ProbeSampler.Core.Services.Processing
{
    public class BoxSearchProcessor : ImageProcessor, IEnableLogger
    {
        private HashSet<ColoredRect> results = new HashSet<ColoredRect>();
        private readonly BoundedQueue<Mat> queue = new BoundedQueue<Mat>(maxSize: 10);
        private readonly BoundedQueue<List<ColoredRect>> queueResultRects = new BoundedQueue<List<ColoredRect>>(maxSize: 10);
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private BufferedProcessor<ColoredRect> bufferedRectangles;
        // double activeROIX, activeROIY, activeROIHeight;
        int mismatchesCount;

        public double ActiveROIX { get; set; }

        public double ActiveROIY { get; set; }

        public double ActiveROIHeight { get; set; }

        public BoxSearchProcessor()
        {
            bufferedRectangles = new BufferedProcessor<ColoredRect>(20, OnBufferFull);

            Task.Run(() => ProcessQueue(cts.Token));
            Task.Run(() => ProcessResultQueue(cts.Token));
        }

        protected override Mat OnImageReceived(Mat image)
        {
            AddImage(image.Clone());
            CleanupOldResults();
            DrawResultsOnImage(image);
            return image;
        }

        private void DrawResultsOnImage(Mat image)
        {
            foreach (ColoredRect coloredRect in results)
            {
                MCvScalar drawColor;

                if (coloredRect.PartType == MachinePartType.Cabine)
                {
                    drawColor = new MCvScalar(0, 255, 0); // Green
                }
                else if (coloredRect.PartType == MachinePartType.Body)
                {
                    drawColor = new MCvScalar(0, 165, 255); // Orange
                }
                else
                {
                    drawColor = coloredRect.Color;
                }

                CvInvoke.Rectangle(image, coloredRect.Rect, drawColor, 2);
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
            using (Mat preparedSrc = new Mat())
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

        private void AddImage(Mat image)
        {
            queue.Enqueue(image);
        }

        private void ProcessQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var image))
                {
                    MedianBlur(image);
                    MorphologyEx(image);
                    MorphologyEx(image, value: 3);
                    MedianBlur(image);
                    GetGradient(image);
                    // MorphologyEx(image, value: 3, iterations: 5, morphType: MorphOp.Close);
                    GaussianBlur(image);
                    MorphologyEx(image, value: 2, iterations: 8);

                    var blobs = FindRectangles(image, searchType: SearchType.Blob);
                    var contours = FindRectangles(image, searchType: SearchType.Contour);

                    var rects = FilterRectangles(blobs.Concat(contours));

                    var queueFilteredRects = AveragingRectangles(rects);

                    foreach (var rect in queueFilteredRects)
                    {
                        bufferedRectangles.Add(rect);
                    }

                    image.Dispose();
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void ProcessResultQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (queueResultRects.TryDequeue(out var allResults))
                {
                    // Отбор кабин
                    List<ColoredRect> cabinsResult = allResults.Where(p => p.PartType == MachinePartType.Cabine).ToList();
                    int maxMatchesCabine = 0;
                    int maxMatchesBodys = 0;
                    ColoredRect cabine = null;
                    List<(int countMatches, ColoredRect body, bool right)> bodys = new List<(int countMatches, ColoredRect body, bool right)>();
                    double deltaWidth = MatWidth * 0.01;
                    double deltaHeight = MatHeight * 0.01;
                    for (int i = 0; i < allResults.Count; i++)
                    {
                        if (allResults[i].PartType == MachinePartType.Cabine)
                        {
                            // Определяется самый частовстречающийся прямоугольник
                            // среди всех кабин
                            int countMatches = allResults.Where(p =>
                                (p?.Rect.Left - deltaWidth) < allResults[i].Rect.Left &&
                                (p?.Rect.Left + deltaWidth) > allResults[i].Rect.Left &&
                                (p?.Rect.Right - deltaWidth) < allResults[i].Rect.Right &&
                                (p?.Rect.Right + deltaWidth) > allResults[i].Rect.Right &&
                                (p?.Rect.Top - deltaHeight) < allResults[i].Rect.Top &&
                                (p?.Rect.Top + deltaHeight) > allResults[i].Rect.Top &&
                                (p?.Rect.Bottom - deltaHeight) < allResults[i].Rect.Bottom &&
                                (p?.Rect.Bottom + deltaHeight) > allResults[i].Rect.Bottom
                                ).Count();
                            if (maxMatchesCabine < countMatches)
                            {
                                // Он и становится кабиной
                                maxMatchesCabine = countMatches;
                                cabine = allResults[i];
                            }
                        }

                        if (allResults[i].PartType == MachinePartType.Body)
                        {
                            // Здесь поиск основан на том, с какой стороны чаще встречаются кабины
                            int countMatchesLeft = 0;
                            int countMatchesRight = 0;
                            bool right = false;
                            List<ColoredRect> leftRects = allResults.Where(p => p.Rect.Right < allResults[i].Rect.Left && p.PartType == MachinePartType.Body).ToList();
                            if (leftRects.Count > 0)
                            {
                                right = true;
                                List<ColoredRect> rightRects = allResults.Where(p => !leftRects.Contains(p) && p.PartType == MachinePartType.Body).ToList();
                                countMatchesRight = rightRects.Where(p =>
                                     (p?.Rect.Left - deltaWidth) < allResults[i].Rect.Left &&
                                     (p?.Rect.Left + deltaWidth) > allResults[i].Rect.Left &&
                                     (p?.Rect.Right - deltaWidth) < allResults[i].Rect.Right &&
                                     (p?.Rect.Right + deltaWidth) > allResults[i].Rect.Right &&
                                     (p?.Rect.Top - deltaHeight) < allResults[i].Rect.Top &&
                                     (p?.Rect.Top + deltaHeight) > allResults[i].Rect.Top &&
                                     (p?.Rect.Bottom - deltaHeight) < allResults[i].Rect.Bottom &&
                                     (p?.Rect.Bottom + deltaHeight) > allResults[i].Rect.Bottom
                                   ).Count();
                            }
                            else
                            {
                                List<ColoredRect> rightRects = allResults.Where(p => p.Rect.Left > allResults[i].Rect.Right && p.PartType == MachinePartType.Body).ToList();
                                leftRects = allResults.Where(p => !rightRects.Contains(p) && p.PartType == MachinePartType.Body).ToList();
                                countMatchesLeft = leftRects.Where(p =>
                                     (p?.Rect.Left - deltaWidth) < allResults[i].Rect.Left &&
                                     (p?.Rect.Left + deltaWidth) > allResults[i].Rect.Left &&
                                     (p?.Rect.Right - deltaWidth) < allResults[i].Rect.Right &&
                                     (p?.Rect.Right + deltaWidth) > allResults[i].Rect.Right &&
                                     (p?.Rect.Top - deltaHeight) < allResults[i].Rect.Top &&
                                     (p?.Rect.Top + deltaHeight) > allResults[i].Rect.Top &&
                                     (p?.Rect.Bottom - deltaHeight) < allResults[i].Rect.Bottom &&
                                     (p?.Rect.Bottom + deltaHeight) > allResults[i].Rect.Bottom
                                   ).Count();
                            }

                            if (countMatchesRight > 0)
                            {
                                var body = bodys.FirstOrDefault(p => p.right);
                                if (body.countMatches < countMatchesRight)
                                {
                                    if (!body.Equals(default(ValueTuple<int, ColoredRect, bool>)))
                                    {
                                        bodys.Remove(body);
                                    }

                                    body.right = true;
                                    body.countMatches = countMatchesRight;
                                    body.body = allResults[i];
                                    bodys.Add(body);
                                }
                            }

                            if (countMatchesLeft > 0)
                            {
                                var body = bodys.FirstOrDefault(p => !p.right);
                                if (body.countMatches < countMatchesLeft)
                                {
                                    if (!body.Equals(default(ValueTuple<int, ColoredRect, bool>)))
                                    {
                                        bodys.Remove(body);
                                    }

                                    body.right = false;
                                    body.countMatches = countMatchesLeft;
                                    body.body = allResults[i];
                                    bodys.Add(body);
                                }
                            }
                        }
                    }

                    // Если кабины до сих пор не было и она была найдена выше, то она же и стаовится новой кабиной
                    if (cabine != null && !results.Where(p => p.PartType == MachinePartType.Cabine).Any())
                    {
                        UpdateResults(cabine);
                        // results.Add(cabine);
                    }
                    else
                    {
                        // Иначе существующая кабина сравнивается с найденной и заменяется в списке
                        ColoredRect currentCabine = results.FirstOrDefault(p => p.PartType == MachinePartType.Cabine);
                        if (currentCabine.Rect.Left - deltaWidth > cabine.Rect.Left ||
                                 (currentCabine.Rect.Left + deltaWidth) < cabine.Rect.Left ||
                                 (currentCabine.Rect.Right - deltaWidth) > cabine.Rect.Right ||
                                 (currentCabine.Rect.Right + deltaWidth) < cabine.Rect.Right ||
                                 (currentCabine.Rect.Top - deltaHeight) > cabine.Rect.Top ||
                                 (currentCabine.Rect.Top + deltaHeight) < cabine.Rect.Top ||
                                 (currentCabine.Rect.Bottom - deltaHeight) > cabine.Rect.Bottom ||
                                 (currentCabine.Rect.Bottom + deltaHeight) < cabine.Rect.Bottom)
                        {
                            if (++mismatchesCount > 2)
                            {
                                mismatchesCount = 0;
                                results.Remove(currentCabine);
                                UpdateResults(cabine);
                            }
                        }
                        else
                            mismatchesCount = 0;
                    }

                    // Примерно такой же процесс происходит с кузовом
                    if (bodys.Count > 0 && !results.Where(p => p.PartType == MachinePartType.Body).Any())
                    {
                        foreach (var body in bodys.Select(p => p.body))
                        {
                            UpdateResults(body);
                        }
                    }
                    else
                    {
                        List<ColoredRect> currentBodys = results.Where(p => p.PartType == MachinePartType.Body).ToList();
                        if (currentBodys.TrueForAll(p => bodys.TrueForAll(b => p.Rect.Left - deltaWidth > b.body.Rect.Left ||
                                  (p.Rect.Left + deltaWidth) < b.body.Rect.Left ||
                                  (p.Rect.Right - deltaWidth) > b.body.Rect.Right ||
                                  (p.Rect.Right + deltaWidth) < b.body.Rect.Right ||
                                  (p.Rect.Top - deltaHeight) > b.body.Rect.Top ||
                                  (p.Rect.Top + deltaHeight) < b.body.Rect.Top ||
                                  (p.Rect.Bottom - deltaHeight) > b.body.Rect.Bottom ||
                                  (p.Rect.Bottom + deltaHeight) < b.body.Rect.Bottom)))
                        {
                            if (++mismatchesCount > 2)
                            {
                                mismatchesCount = 0;
                                results.RemoveWhere(p => currentBodys.Contains(p));
                                foreach (var body in bodys.Select(p => p.body))
                                {
                                    UpdateResults(body);
                                }
                            }
                        }
                        else
                            mismatchesCount = 0;
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void OnBufferFull(IEnumerable<ColoredRect> bufferedColoredRects)
        {
            var queueFilteredRects = bufferedColoredRects.ToList();
            List<ColoredRect> allRects = new List<ColoredRect>();

            if (ActiveROIX > 0 && ActiveROIY > 0 && ActiveROIHeight > 0)
            {
                var insideRoiRects = queueFilteredRects.Where(r => r.Rect.Top >= ActiveROIY && r.Rect.Bottom <= ActiveROIY + ActiveROIHeight);
                allRects.AddRange(insideRoiRects);
            }
            else
            {
                allRects.AddRange(queueFilteredRects);
            }

            if (allRects.Count == 0)
            {
                return;
            }

            allRects = AveragingRectangles(allRects).ToList();

            #region Нахождение частей камаза
            // кабина будет находится правее кузова и прицепа, значит отсеиваем лишние прямоугольники
            List<ColoredRect> selectedPart = new List<ColoredRect>(); // Для выбраных(?)
            List<ColoredRect> selectedCabins = new List<ColoredRect>(); // Для кабин
            List<ColoredRect> selectedBodys = new List<ColoredRect>(); // Для кузовов

            var thirdOfAllRects = allRects.Count / 3;
            selectedCabins = allRects.Where(p => allRects.Count(cr => cr.Rect.Right <= p.Rect.Left) >= thirdOfAllRects &&
                !allRects.Any(cr => cr.Rect.Left >= p.Rect.Right)).ToList();

            if (selectedCabins.Count == 0)
            {
                // Список всех прямоугольников allRects сортируется по убыванию координаты Left (левой границы прямоугольника).
                // Для каждого прямоугольника p в отсортированном списке allRects выполняются следующие действия:
                // Подсчитывается количество прямоугольников в allRects, которые расположены слева от p и чья правая граница находится левее правой границы p.
                // Если это количество больше или равно трети общего числа прямоугольников в allRects, и не существует прямоугольников, расположенных справа от p, то p классифицируется как кабина.
                // Классифицированный как кабина прямоугольник p добавляется в список selectedCabins, и его тип устанавливается в DataTypes.Enums.MachinePartTypes.Cabine.

                selectedCabins = allRects
                    .OrderByDescending(p => p.Rect.Left)
                    .Where(p => allRects.Count(cr => cr.Rect.Left <= p.Rect.Left && cr.Rect.Right < p.Rect.Right) >= thirdOfAllRects &&
                                !allRects.Any(cr => cr.Rect.Left >= p.Rect.Right)).ToList();
                selectedCabins.ForEach(p => p.PartType = MachinePartType.Cabine);
            }

            if (selectedCabins.Count > 0)
            {
                // Создается кабина.
                // Координаты и размеры кабины определяются как средние значения соответствующих параметров прямоугольников из списка selectedCabins.
                Rectangle rect = new Rectangle(
                    (int)Math.Round(selectedCabins.Average(p => p.Rect.Left)),
                    (int)Math.Round(selectedCabins.Average(p => p.Rect.Top)),
                    (int)Math.Round(selectedCabins.Average(p => p.Rect.Width)),
                    (int)Math.Round(selectedCabins.Average(p => p.Rect.Height)));
                ColoredRect cabin = new ColoredRect(rect, new MCvScalar(0, 0, 255), MachinePartType.Cabine);

                selectedPart.Add(cabin);
                double inaccuracy = cabin.Rect.Height * 0.2;
                // Осуществляется поиск кузова
                allRects.Where(p => !selectedCabins.Contains(p)).OrderByDescending(p => p.Rect.Right).ToList().ForEach(p =>
                {
                    // Выполняются проверки на соответствие условиям
                    // для кузова (расположение относительно кабины, высота, ширина и т.д.).
                    if (p.Rect.Right < cabin.Rect.Left && // Кузов предположительно должен находится левее кабины
                    p.Rect.Top < cabin.Rect.Top + inaccuracy &&
                    p.Rect.Top > cabin.Rect.Top - inaccuracy &&
                    p.Rect.Bottom < cabin.Rect.Bottom + inaccuracy &&
                    p.Rect.Bottom > cabin.Rect.Bottom - inaccuracy &&
                    p.Rect.Width > cabin.Rect.Width)
                    {
                        p.PartType = MachinePartType.Body;
                        selectedBodys.Add(p);
                        selectedPart.Add(new ColoredRect(p.Rect, new MCvScalar(0, 0, 255), MachinePartType.Body));
                    }
                });
                if (selectedBodys.Count == 0)
                {
                    allRects.Where(p => !selectedCabins.Contains(p)).OrderByDescending(p => p.Rect.Right).ToList().ForEach(p =>
                    {
                        // Выполняются проверки на соответствие условиям
                        // для кузова (расположение относительно кабины, высота, ширина и т.д.).
                        if (p.Rect.Right < cabin.Rect.Right && // Кузов предположительно должен находится правее кабины
                        p.Rect.Top < cabin.Rect.Top + inaccuracy &&
                        p.Rect.Top > cabin.Rect.Top - inaccuracy &&
                        p.Rect.Bottom < cabin.Rect.Bottom + inaccuracy &&
                        p.Rect.Bottom > cabin.Rect.Bottom - inaccuracy &&
                        p.Rect.Width > cabin.Rect.Width)
                        {
                            p.PartType = MachinePartType.Body;
                            selectedBodys.Add(p);
                            selectedPart.Add(new ColoredRect(p.Rect, new MCvScalar(0, 0, 255), MachinePartType.Body));
                        }
                    });
                }

                foreach (ColoredRect body in selectedBodys.OrderByDescending(p => p.Rect.Left).ToList())
                {
                    // Проходит проверка каждого элемента из списка selectedBodys
                    // и удаляет те, что не соответствуют определенным условиям относительно других элементов в этом списке.

                    if (selectedBodys.Where(p => p.Rect.Left < body.Rect.Left &&
                        body.Rect.Top < p.Rect.Top + inaccuracy &&
                        body.Rect.Top > p.Rect.Top - inaccuracy &&
                        body.Rect.Bottom < p.Rect.Bottom + inaccuracy &&
                        body.Rect.Bottom > p.Rect.Bottom - inaccuracy &&
                        body.Rect.Width < p.Rect.Width &&
                        p.Rect.Right > body.Rect.Left - inaccuracy &&
                        p.Rect.Right < body.Rect.Right).Any())
                    {
                        selectedBodys.Remove(body);
                    }
                }
            }
            #endregion

            if (selectedPart.Any(p => p.PartType == MachinePartType.Body))
            {
                queueResultRects.Enqueue(selectedPart);
            }
        }

        private void CleanupOldResults()
        {
            var now = DateTime.Now;
            results.RemoveWhere(rect => (now - rect.LastDetected).TotalSeconds > 20);
        }

        private void UpdateResults(ColoredRect rect)
        {
            rect.LastDetected = DateTime.Now;
            results.Add(rect);
        }

        public override void Dispose()
        {
            base.Dispose();

            cts?.Cancel();
            cts?.Dispose();
        }
    }
}
