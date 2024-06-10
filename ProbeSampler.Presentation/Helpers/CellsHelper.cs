using ProbeSampler.Core.Services.Processing.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;

namespace ProbeSampler.Presentation.Helpers
{
    public static class CellsHelper
    {
        // public static int randomCellsQuantity { get; private set; } = 1;
        const int MAX_ATTEMPTS = 1000;
        public static readonly Dictionary<TruckType, int> TruckTypesPoints = new Dictionary<TruckType, int>()
        {
            [TruckType.None] = 0,
            [TruckType.Little] = 6,
            [TruckType.Average] = 6,
            [TruckType.TwoTrailers] = 12,
            [TruckType.Big] = 8,
        };

        public static void SelectRandomCells(IEnumerable<Cell> cells, IEnumerable<BoundingBox>? boxes)
        {
            int cellsToSelect = GetCellNumberToSelect(boxes);

            if (cells == null || cellsToSelect == 0)
            {
                return;
            }

            var availableCells = new List<Cell>();

            foreach (var cell in cells.Where(c => c.CellState == CellState.Selected))
            {
                cell.CellState = CellState.AvailableForSelect;
            }

            foreach (var cell in cells.Where(c => c.CellState == CellState.AvailableForSelect))
            {
                availableCells.Add(cell);
            }

            if (availableCells.Count == 0)
            {
                return;
            }

            // уменьшаю область, где можно выбирать клетки, чтобы случайно не выходить за пределы кузова
            //availableCells = ReduceBox(availableCells, BoxTrim.All);

            var r = new Random();
            var randomCells = new List<Cell>();
            int cnt = 0;
            // todo добавить в настройки adjustDistance
            int adjustDistance = 3;

            //TODO REFACTOR THIS SHIT ASAP!!!
            var tmpRandomCells = new List<Cell>();

            if (boxes.Count() == 1)
            {
                while (randomCells.Count < cellsToSelect + 2 && cnt < MAX_ATTEMPTS)
                {
                    var cell = availableCells.OrderBy(x => r.Next()).First();
                    if (randomCells.All(c => !AreCellsAdjacent(c, cell, adjustDistance)))
                    {
                        randomCells.Add(cell);
                    }

                    cnt++;
                    if (cnt > 999 || adjustDistance != 0)
                    {
                        //сообщение для пользователя, что не было выбрано нужное количество клеток
                        //var messageRes = MessageBox.Show($"Было выбрано {randomCells.Count} клетки вместо {randomCellsToSelect}, заполнить с меньшим расстоянием между клетками?", "Не удалось выбрать достаточно клеток случайным образом", MessageBoxButton.YesNo);
                        //if (messageRes == MessageBoxResult.Yes)

                        randomCells.Clear();
                        cnt = 0;
                        adjustDistance--;
                    }
                }
            }
            else
            {
                var firstCar = new List<Cell>();
                var firstCarNumber = 6;
                var secondCar = new List<Cell>();
                var seconCarNumber = 6;

                foreach (var cell in availableCells)
                {
                    if (IsWithinBoxes(cell, boxes.ToList()[0]))
                    {
                        firstCar.Add(cell);
                    }
                    if (IsWithinBoxes(cell, boxes.ToList()[1]))
                    {
                        secondCar.Add(cell);
                    }
                }



                while (randomCells.Count < firstCarNumber + 1 && cnt < MAX_ATTEMPTS)
                {
                    var cell = firstCar.OrderBy(x => r.Next()).First();
                    if (randomCells.All(c => !AreCellsAdjacent(c, cell, adjustDistance)))
                    {
                        randomCells.Add(cell);
                    }

                    cnt++;
                    if (cnt > 999 || adjustDistance != 0)
                    {
                        //сообщение для пользователя, что не было выбрано нужное количество клеток
                        //var messageRes = MessageBox.Show($"Было выбрано {randomCells.Count} клетки вместо {randomCellsToSelect}, заполнить с меньшим расстоянием между клетками?", "Не удалось выбрать достаточно клеток случайным образом", MessageBoxButton.YesNo);
                        //if (messageRes == MessageBoxResult.Yes)

                        randomCells.Clear();
                        cnt = 0;
                        adjustDistance--;
                    }
                }

                foreach (var cell in randomCells)
                {
                    cell.CellState = CellState.Selected;
                }

                randomCells.Clear();

                while (randomCells.Count < seconCarNumber + 1 && cnt < MAX_ATTEMPTS)
                {
                    var cell = secondCar.OrderBy(x => r.Next()).First();
                    if (randomCells.All(c => !AreCellsAdjacent(c, cell, adjustDistance)))
                    {
                        randomCells.Add(cell);
                    }

                    cnt++;
                    if (cnt > 999 || adjustDistance != 0)
                    {
                        //сообщение для пользователя, что не было выбрано нужное количество клеток
                        //var messageRes = MessageBox.Show($"Было выбрано {randomCells.Count} клетки вместо {randomCellsToSelect}, заполнить с меньшим расстоянием между клетками?", "Не удалось выбрать достаточно клеток случайным образом", MessageBoxButton.YesNo);
                        //if (messageRes == MessageBoxResult.Yes)

                        randomCells.Clear();
                        cnt = 0;
                        adjustDistance--;
                    }
                }
            }

            foreach (var cell in randomCells)
            {
                cell.CellState = CellState.Selected;
            }
        }

        /// <summary>
        /// Определяет, находятся ли две клетки рядом друг с другом.
        /// </summary>
        /// <param name="cell1">Первая клетка.</param>
        /// <param name="cell2">Вторая клетка.</param>
        /// <param name="distance">Расстояние между клетками.</param>
        /// <returns>True, если клетки находятся рядом, false в противном случае.</returns>
        private static bool AreCellsAdjacent(Cell cell1, Cell cell2, int distance = 0)
        {
            // При distance == 2 расстояние между клетками равно 1, поэтому увеличиваем distance на 1.
            distance++;
            // Проверяем, находятся ли клетки рядом по горизонтали или вертикали.
            bool isAdjacent = Math.Abs(cell1.X - cell2.X) <= cell1.Width * distance && Math.Abs(cell1.Y - cell2.Y) <= cell1.Height * distance;

            // Проверяем, не находятся ли клетки в одной позиции и не находятся ли они рядом по диагонали.
            isAdjacent = isAdjacent && (Math.Abs(cell1.X - cell2.X) != cell1.Width * (distance + 1) || Math.Abs(cell1.Y - cell2.Y) != cell1.Height * (distance + 1));

            return isAdjacent;
        }

        public static void UpdateCell(Cell cell)
        {
            CellState state = cell.CellState;
            switch (state)
            {
                case CellState.AvailableForSelect:
                    cell.CellState = CellState.Selected;
                    break;
                case CellState.Selected:
                    cell.CellState = CellState.Default;
                    break;
                case CellState.Default:
                    cell.CellState = CellState.Selected;
                    break;
                default:
                    break;
            }
        }

        public static void ResetCell(Cell cell)
        {
            CellState state = cell.CellState;
            switch (state)
            {
                case CellState.Selected:
                    cell.CellState = CellState.Default;
                    break;
                default:
                    break;
            }
        }

        public static void AddCrossbarCells(Cell clickedCell, List<Cell> cells, bool isVertical)
        {
            List<Cell> barCells = new List<Cell>();
            if (isVertical)
            {
                barCells = cells.Where(cell => cell.Y == clickedCell.Y).ToList();
            }
            else
            {
                barCells = cells.Where(cell => cell.X == clickedCell.X).ToList();
            }

            foreach (Cell cell in barCells)
            {
                CellState state = cell.CellState;
                switch (state)
                {
                    case CellState.AvailableForSelect:
                        cell.CellState = CellState.Crossbar;
                        break;
                    case CellState.Selected:
                        cell.CellState = CellState.Crossbar;
                        break;
                    case CellState.Crossbar:
                        cell.CellState = CellState.Crossbar;
                        break;
                    case CellState.Default:
                        cell.CellState = CellState.Default;
                        break;
                    default:
                        break;
                }
            }
        }

        public static void UpdateCellsState(List<Cell> cells, IEnumerable<BoundingBox> boundingBodyBoxes)
        {
            // Пройти по всем клеткам
            foreach (var cell in cells)
            {
                // Если клетка уже выделена, то пропустить ее
                if (cell.CellState == CellState.Selected)
                {
                    continue;
                }

                // Если клетка пересекается с bounding box, то выделить ее иначе убрать выделение
                if (IsWithinBoxes(cell, boundingBodyBoxes.Where(body => body.Confidence > 0.7)))
                {
                    //if (cell.CellState != CellState.Crossbar)
                    cell.CellState = CellState.AvailableForSelect;
                }
                else
                {
                    cell.CellState = CellState.Default;
                }
            }                     

        }

        public static void UpdateCrossbarsState(List<Cell> cells, IEnumerable<BoundingBox> boundingCrossbarBoxes)
        {
            if (boundingCrossbarBoxes is null || boundingCrossbarBoxes.Count() == 0)
                return;

            var availableCells = cells.Where(cell => cell.CellState == CellState.AvailableForSelect);

            foreach (var cell in availableCells)
            {
                if (IsWithinBoxes(cell, boundingCrossbarBoxes, true))               
                    cell.CellState = CellState.Crossbar;                                      
                else                
                    cell.CellState = CellState.AvailableForSelect;                
            }
        }

        /// <summary>
        /// Checks if a cell is intersecting with a bounding boxes.
        /// </summary>
        /// <lm name="cell">The cell to check.</param>
        /// <param name="boundingBoxes">The bounding boxes to check.</param>
        /// <returns>True if the cell is intersecting with the bounding boxes, false otherwise.</returns>
        private static bool IsWithinBoxes(Cell cell, IEnumerable<BoundingBox> boundingBoxes)
        {
            bool withinXBounds;
            bool withinYBounds;
            bool isCellWithin = false;
            // уменьшаю область, где можно выбирать клетки, чтобы случайно не выходить за пределы кузова
            //todo 
            double boxSizeReducingValue = cell.Height * 0.7;

            foreach (var box in boundingBoxes)
            {
                var bRectangle = box.Box;
                withinXBounds = cell.X >= bRectangle.X + boxSizeReducingValue && (cell.X + cell.Width) <= (bRectangle.X - boxSizeReducingValue * 2 + bRectangle.Width);
                withinYBounds = cell.Y >= bRectangle.Y + boxSizeReducingValue && (cell.Y + cell.Height) <= (bRectangle.Y - boxSizeReducingValue + bRectangle.Height);
                
                isCellWithin |= withinXBounds && withinYBounds;
            }

            return isCellWithin;
        }

        private static bool IsWithinBoxes(Cell cell, BoundingBox box)
        {
            bool withinXBounds;
            bool withinYBounds;
            bool isCellIntersecting = false;
            // уменьшаю область, где можно выбирать клетки, чтобы случайно не выходить за пределы кузова
            //todo 
            double boxSizeReducingValue = cell.Height * 0.7;

            var bRectangle = box.Box;
            withinXBounds = cell.X >= bRectangle.X + boxSizeReducingValue && (cell.X + cell.Width) <= (bRectangle.X - boxSizeReducingValue + bRectangle.Width);
            withinYBounds = cell.Y >= bRectangle.Y + boxSizeReducingValue && (cell.Y + cell.Height) <= (bRectangle.Y - boxSizeReducingValue + bRectangle.Height);

            isCellIntersecting |= withinXBounds && withinYBounds;

            return isCellIntersecting;
        }

        private static bool IsWithinBoxes(Cell cell, IEnumerable<BoundingBox> boundingBoxes, bool isCrossbar)
        {
            bool withinXBounds;
            bool withinYBounds;
            // уменьшаю область, где можно выбирать клетки, чтобы случайно не выходить за пределы кузова
            //todo 
            double boxSizeReducingValue = -cell.Height * 0.5;

            foreach (var box in boundingBoxes)
            {
                var bRectangle = box.Box;
                withinXBounds = cell.X >= bRectangle.X + boxSizeReducingValue && (cell.X + cell.Width) <= (bRectangle.X - boxSizeReducingValue * 2 + bRectangle.Width);
                withinYBounds = cell.Y >= bRectangle.Y + boxSizeReducingValue && (cell.Y + cell.Height) <= (bRectangle.Y - boxSizeReducingValue + bRectangle.Height);

                if (withinXBounds && withinYBounds)
                    return true;
            }
            return false;
        }

        //private static bool IsIntersecting(Cell cell, IEnumerable<BoundingBox> boundingBoxes)
        //{
        //    bool isCellIntersecting = false;
        //    foreach (var box in boundingBoxes)
        //    {
        //        var bRectangle = box.Box;
        //        //(a.y < b.y1 || a.y1 > b.y || a.x1 < b.x || a.x > b.x1);

        //        isCellIntersecting = !(cell.Y < (bRectangle.Y + bRectangle.Height) 
        //            || (cell.Y + cell.Height) > bRectangle.Y 
        //            || (cell.X + cell.Width) < bRectangle.X 
        //            || cell.X > (bRectangle.X + bRectangle.Width));
        //        if (isCellIntersecting)
        //            return isCellIntersecting;
        //    }
        //    return isCellIntersecting;
        //}

        /// <summary>
        /// Updates a list of cells by adding new cells based on the parameters of a grid overlay.
        /// </summary>
        /// <param name="cells">The list of cells to update.</param>
        /// <param name="gridOverlayControlX">The X coordinate of the grid overlay.</param>
        /// <param name="gridOverlayControlY">The Y coordinate of the grid overlay.</param>
        /// <param name="gridOverlayWidth">The width of the grid overlay.</param>
        /// <param name="gridOverlayHeight">The height of the grid overlay.</param>
        /// <param name="gridOverlayCellHeight">The height of each cell in the grid overlay.</param>
        public static void UpdateCells(List<Cell> cells, double gridOverlayControlX, double gridOverlayControlY, double gridOverlayWidth, double gridOverlayHeight, int gridOverlayCellHeight)
        {
            if (!(gridOverlayControlX > 0
                    && gridOverlayControlY > 0
                    && gridOverlayWidth > 0
                    && gridOverlayHeight > 0
                    && gridOverlayCellHeight > 0))
            {
                return;
            }

            for (double x = gridOverlayControlX; x < gridOverlayControlX + gridOverlayWidth; x += gridOverlayCellHeight)
            {
                for (double y = gridOverlayControlY; y < gridOverlayControlY + gridOverlayHeight; y += gridOverlayCellHeight)
                {
                    cells.Add(new(x, y, gridOverlayCellHeight, gridOverlayCellHeight));
                }
            }
        }

        /// <summary>
        /// Resets the state of the selected cells in a list of cells.
        /// </summary>
        /// <param name="cells">The list of cells to reset.</param>
        public static void ResetSelectedCells(List<Cell> cells)
        {
            if (cells != null)
            {
                foreach (var cell in cells.Where(c => c.CellState == CellState.Selected || c.CellState == CellState.Crossbar))
                {
                    cell.CellState = CellState.AvailableForSelect;
                }
            }
        }

        public static List<Cell> ReduceBox(List<Cell> availableCells, BoxTrim BoxReductionSide = BoxTrim.All)
        {
            if (availableCells.Count == 0)
            {
                return availableCells;
            }
            // Определяем минимальное и максимальное значение по каждой координате
            (double minX, double minY, double maxX, double maxY) = FindMinMaxXY(availableCells);

            // Определяем лямбда-функции для каждого условия
            Func<Cell, bool> rightOfMinX = c => c.X > minX;
            Func<Cell, bool> aboveMinY = c => c.Y > minY;
            Func<Cell, bool> leftOfMaxX = c => c.X < maxX;
            Func<Cell, bool> belowMaxY = c => c.Y < maxY;

            // Используем LINQ для фильтрации клеток по заданной стороне
            List<Cell> reducedBox = availableCells.Where(c =>
            {
                switch (BoxReductionSide)
                {
                    case BoxTrim.Left:
                        return rightOfMinX(c);
                    case BoxTrim.Top:
                        return aboveMinY(c);
                    case BoxTrim.Right:
                        return leftOfMaxX(c);
                    case BoxTrim.Bottom:
                        return belowMaxY(c);
                    case BoxTrim.All:
                        return rightOfMinX(c) && aboveMinY(c) && leftOfMaxX(c) && belowMaxY(c);
                    default:
                        return false;
                }
            }).ToList();

            return reducedBox;
        }

        private static (double, double, double, double) FindMinMaxXY(List<Cell> cells)
        {
            var minX = cells.Select(c => c.X).Min();
            var minY = cells.Select(c => c.Y).Min();
            var maxX = cells.Select(c => c.X).Max();
            var maxY = cells.Select(c => c.Y).Max();

            return (minX, minY, maxX, maxY);
        }

        public static List<Cell> GetSelectedCells(List<Cell> cells)
        {
            // Используем LINQ для фильтрации и преобразования в список
            var selectedCells = cells.Where(c => c.CellState == CellState.Selected).ToList();

            return selectedCells;
        }

        public static int GetCellNumberToSelect(IEnumerable<BoundingBox> boxes)
        {
            var truckType = TruckType.None;
            if (boxes != null)
            {
                //var TruckBody = boxes.Where(box => box.Label == "body");
                //if (boxes.Count() > 1 && boxes.All(box => box.Width > 400 && box.Width < 1500))
                //{
                //    truckType = TruckType.TwoTrailers;
                //}                
                if (boxes.Count() > 1)
                {
                    truckType = TruckType.TwoTrailers;
                }
                else if (boxes.Count() == 1 && boxes.All(box => box.Width > 1000 && box.Width < 2000))
                {
                    truckType = TruckType.Big;
                }
                else if (boxes.Count() == 1 && boxes.All(box => box.Width > 650 && box.Width < 1000))
                {
                    truckType = TruckType.Average;
                }
                else if (boxes.Count() == 1 && boxes.All(box => box.Width > 400 && box.Width < 650))
                {
                    truckType = TruckType.Little;
                }
                else if (boxes.Count() == 0)
                {
                    truckType = TruckType.None;
                }
                return TruckTypesPoints[truckType];
            }
            else
                return 0;
        }
    }
}
