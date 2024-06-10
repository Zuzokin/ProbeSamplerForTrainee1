using Compunet.YoloV8.Data;
using Compunet.YoloV8.Plotting;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

namespace ProbeSampler.NeuroUtils.Extensions
{
    public static class PlottingExtensions
    {
        private static readonly string[] hexColors = new string[20]
        {
            "FF3838", "FF9D97", "FF701F", "FFB21D", "CFD231", "48F90A", "92CC17", "3DDB86", "1A9334", "00D4BB",
            "2C99A8", "00C2FF", "344593", "6473FF", "0018EC", "8438FF", "520085", "CB38FF", "FF95C8", "FF37C7",
        };

        private static MCvScalar HexToMCvScalar(int index)
        {
            if (index > hexColors.Length - 1 || index < 0)
            {
                index = 0;
            }

            string hexColor = hexColors[index];
            if (hexColor.StartsWith('#'))
            {
                hexColor = hexColor[1..];
            }

            int r = int.Parse(hexColor[..2], System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new MCvScalar(b, g, r);
        }

        #region Detection

        public static Mat PlotImage(this IDetectionResult result, Mat image) => result.PlotImage(image, 0, DetectionPlottingOptions.Default);

        public static Mat PlotImage(this IDetectionResult result, Mat image, double scale) => result.PlotImage(image, scale, DetectionPlottingOptions.Default);

        public static Mat PlotImage(this IDetectionResult result, Mat image, double scale, DetectionPlottingOptions options)
        {
            var processed = image.Clone();

            Size size = processed.Size;

            float ratio = Math.Max(size.Width, size.Height) / 640F;

            // EnsureSize(process.Size, result.Image);

            double fontScale = options.FontSize * ratio / 20.0;
            int fontThickness = (int)(options.FontSize * ratio / 20.0);
            var textOffset = new Point((int)(options.TextHorizontalPadding * ratio), (int)(options.FontSize * ratio));

            int thickness = (int)(options.BoxBorderThickness * ratio);

            foreach (var box in result.Boxes)
            {
                if (box.Class.Name != "body")
                {
                    continue;
                }

                string realSizeInfo = string.Empty;

                if (scale > 0)
                {
                    realSizeInfo = $"{Math.Round(box.Bounds.Width * scale)} mm x {Math.Round(box.Bounds.Height * scale)} mm";
                }

                string label = $"{box.Class.Name} {box.Confidence:N} {realSizeInfo}";
                MCvScalar color = HexToMCvScalar(box.Class.Id);

                var rect = new Rectangle(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);

                CvInvoke.Rectangle(processed, rect, color, thickness);

                var labelLocation = new Point(box.Bounds.Left, box.Bounds.Top - textOffset.Y);

                // Drawing text on the image
#if (DEBUG) 
                CvInvoke.PutText(processed, label, labelLocation, FontFace.HersheySimplex, fontScale, color, fontThickness);
#endif
            }

            /*            var textOptions = new TextOptions(options.FontFamily.CreateFont(options.FontSize * ratio));

                        var textPadding = options.TextHorizontalPadding * ratio;

                        var thickness = options.BoxBorderThickness * ratio;

                        foreach (var box in result.Boxes)
                        {
                            var label = $"{box.Class.Name} {box.Confidence:N}";
                            var color = options.ColorPalette.GetColor(box.Class.Id);

                            process.Mutate(context =>
                            {
                                DrawBoundingBox(context, box.Bounds, color, thickness, .1F, label, textOptions, textPadding);
                            });
                        }*/

            return processed;
        }

#endregion

        #region Segmentation

        /*public static Mat PlotImage(this ISegmentationResult result, Mat image) => result.PlotImage(image, SegmentationPlottingOptions.Default);

        public static Mat PlotImage(this ISegmentationResult result, Mat image, SegmentationPlottingOptions options)
        {
            var processed = image.Clone();

            Size size = processed.Size;

            float ratio = Math.Max(size.Width, size.Height) / 640F;

            double fontScale = options.FontSize * ratio / 20.0;
            int fontThickness = (int)(options.FontSize * ratio / 20.0);
            Point textOffset = new Point((int)(options.TextHorizontalPadding * ratio), (int)(options.FontSize * ratio));

            int thickness = (int)(options.BoxBorderThickness * ratio);

            #region Draw Masks

            using var masksLayer = new Image<Rgba32>(size.Width, size.Height);
            using var contoursLayer = new Image<Rgba32>(size.Width, size.Height);

            for (int i = 0; i < result.Boxes.Count; i++)
            {
                var box = result.Boxes[i];
                var color = options.ColorPalette.GetColor(box.Class.Id);

                using var mask = new Image<Rgba32>(box.Bounds.Width, box.Bounds.Height);

                for (int x = 0; x < box.Mask.Width; x++)
                {
                    for (int y = 0; y < box.Mask.Height; y++)
                    {
                        var value = box.Mask[x, y];

                        if (value > options.MaskConfidence)
                            mask[x, y] = color;
                    }
                }

                masksLayer.Mutate(x => x.DrawImage(mask, box.Bounds.Location, 1F));

                if (options.ContoursThickness > 0F)
                {
                    using var contours = CreateContours(mask, color, options.ContoursThickness * ratio);
                    contoursLayer.Mutate(x => x.DrawImage(contours, box.Bounds.Location, 1F));
                }
            }

            process.Mutate(x => x.DrawImage(masksLayer, .4F));
            process.Mutate(x => x.DrawImage(contoursLayer, 1F));

            #endregion

            #region Draw Boxes

            foreach (var box in result.Boxes)
            {
                var label = $"{box.Class.Name} {box.Confidence:N}";
                var color = options.ColorPalette.GetColor(box.Class.Id);

                process.Mutate(context =>
                {
                    DrawBoundingBox(context,
                                    box.Bounds,
                                    color,
                                    thickness,
                                    0F,
                                    label,
                                    textOptions,
                                    textPadding);
                });
            }

            #endregion

            return process;
        }*/

        #endregion

        #region Classification

        /*public static Image PlotImage(this IClassificationResult result, Mat image) => PlotImage(result, image, ClassificationPlottingOptions.Default);

        public static Image PlotImage(this IClassificationResult result, Mat image, ClassificationPlottingOptions options)
        {
            var process = originImage.Load(true);

            EnsureSize(process.Size, result.Image);

            var size = result.Image;

            var ratio = Math.Max(size.Width, size.Height) / 640F;

            var textOptions = new TextOptions(options.FontFamily.CreateFont(options.FontSize * ratio));

            var label = $"{result.Class.Name} {result.Confidence:N}";
            var fill = options.FillColorPalette.GetColor(result.Class.Id);
            var border = options.BorderColorPalette.GetColor(result.Class.Id);

            var pen = new SolidPen(border, options.BorderThickness * ratio);
            var brush = new SolidBrush(fill);
            var location = new PointF(options.XOffset * ratio, options.YOffset * ratio);

            process.Mutate(x => x.DrawText(label, textOptions.Font, brush, pen, location));

            return process;
        }*/

        #endregion

        #region Private Methods

        /*private static void DrawBoundingBox(IImageProcessingContext context,
                                            Rectangle bounds,
                                            Color color,
                                            float borderThickness,
                                            float fillOpacity,
                                            string labelText,
                                            TextOptions textOptions,
                                            float textPadding)
        {
            var polygon = new RectangularPolygon(bounds);

            context.Draw(color, borderThickness, polygon);

            if (fillOpacity > 0F)
                context.Fill(color.WithAlpha(fillOpacity), polygon);

            var rendered = TextMeasurer.MeasureSize(labelText, textOptions);
            var renderedSize = new Size((int)(rendered.Width + textPadding), (int)rendered.Height);

            var location = bounds.Location;

            location.Offset(0, -renderedSize.Height);

            //var textLocation = new Point((int)(location.X + textPadding / 2), location.Y);
            var textLocation = new PointF(location.X + textPadding / 2, location.Y);

            var textBoxPolygon = new RectangularPolygon(location, renderedSize);

            context.Fill(color, textBoxPolygon);
            context.Draw(color, borderThickness, textBoxPolygon);

            context.DrawText(labelText, textOptions.Font, Color.White, textLocation);
        }*/

        /*private static Mat CreateContours(this Mat source, Color color, float thickness)
        {
            var contours = ImageContoursHelper.FindContours(source);

            var result = new Image<Rgba32>(source.Width, source.Height);

            foreach (var points in contours)
            {
                if (points.Count < 2)
                    continue;

                var pathBuilder = new PathBuilder();
                pathBuilder.AddLines(points.Select(x => (PointF)x));

                var path = pathBuilder.Build();

                result.Mutate(x =>
                {
                    x.Draw(color, thickness, path);
                });
            }

            return result;
        }*/

        private static void EnsureSize(Size origin, Size result)
        {
            if (origin != result)
            {
                throw new InvalidOperationException("Original image size must to be equals to prediction result image size");
            }
        }

        #endregion
    }
}
