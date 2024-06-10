using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProbeSampler.WPF.Helpers
{
    public static class OverlayHelpers
    {
        /// <summary>
        /// Нарисовать сетку.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="startPosition"></param>
        /// <param name="gridSize"></param>
        /// <param name="cellSize"></param>
        /// <param name="cellPrefix"></param>
        /// <param name="rectangleClickEvent"></param>
        public static void DrawGridOverlay(
            this Canvas canvas,
            System.Drawing.Point startPosition,
            System.Drawing.Size gridSize,
            double cellSize,
            string cellPrefix = "Grid_Cell_",
            Action<object, MouseButtonEventArgs>? rectangleClickEvent = default)
        {
            for (double x = startPosition.X; x < startPosition.X + gridSize.Width; x += cellSize)
            {
                for (double y = startPosition.Y; y < startPosition.Y + gridSize.Height; y += cellSize)
                {
                    System.Windows.Shapes.Rectangle cell = new System.Windows.Shapes.Rectangle
                    {
                        Name = $"{cellPrefix}{y}_{x}",
                        Width = cellSize,
                        Height = cellSize,
                        Stroke = System.Windows.Media.Brushes.Crimson,
                        Fill = System.Windows.Media.Brushes.Transparent,
                    };

                    Canvas.SetLeft(cell, x);
                    Canvas.SetTop(cell, y);
                    canvas.Children.Add(cell);

                    cell.MouseLeftButtonDown += (sender, e) =>
                    {
                        rectangleClickEvent?.Invoke(sender, e);
                    };
                }
            }
        }

        /// <summary>
        /// Удалить сетку.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="cellPrefix"></param>
        public static void EraseGridOverlay(this Canvas canvas, string cellPrefix = "Grid_Cell_")
        {
            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (UIElement child in canvas.Children)
            {
                if (child is System.Windows.Shapes.Rectangle rect && rect.Name.StartsWith(cellPrefix))
                {
                    elementsToRemove.Add(child);
                }
            }

            foreach (UIElement element in elementsToRemove)
            {
                canvas.Children.Remove(element);
            }
        }

        public static System.Windows.Shapes.Rectangle DrawCellOverlay(this Canvas canvas, Cell cell, string cellPrefix = "Grid_Cell_")
        {
            return canvas.DrawCellOverlay(
                x: cell.X,
                y: cell.Y,
                width: cell.Width,
                height: cell.Height,
                cellPrefix: cellPrefix
                );
        }

        public static System.Windows.Shapes.Rectangle DrawCellOverlay(this Canvas canvas, double x, double y, double width, double height, string cellPrefix = "Grid_Cell_")
        {
            System.Windows.Shapes.Rectangle cell = new System.Windows.Shapes.Rectangle
            {
                Name = $"{cellPrefix}{y:2}_{x:2}",
                Width = width,
                Height = height,
                // Stroke = System.Windows.Media.Brushes.Crimson,
                Fill = System.Windows.Media.Brushes.Transparent,
            };

            Canvas.SetLeft(cell, x);
            Canvas.SetTop(cell, y);
            canvas.Children.Add(cell);

            return cell;
        }
    }
}
