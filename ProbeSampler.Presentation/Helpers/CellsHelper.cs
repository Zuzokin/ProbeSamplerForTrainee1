using ProbeSampler.Core.Services.Processing.Core;
using System.Collections.Generic;
using System.Linq;

namespace ProbeSampler.Presentation.Helpers
{
    public static class CellsHelper
    {
        // public static int randomCellsQuantity { get; private set; } = 1;
        const int MAX_ATTEMPTS = 1000;
        // todo refactor: replace to config and add interface for use to edit
        /// <summary>
        /// Словарь, где храняться хардкод значения количества клеток для отбора
        /// </summary>
        public static readonly Dictionary<TruckType, int> TruckTypesPoints = new Dictionary<TruckType, int>()
        {
            [TruckType.None] = 0,
            [TruckType.Little] = 6,
            [TruckType.Average] = 6,
            [TruckType.TwoTrailers] = 12,
            [TruckType.Big] = 8,
        };

        /// <summary>
        /// Выбирает случайные клетки из предоставленного списка клеток и границ (BoundingBox)
        /// </summary>
        /// <param name="cells">Список клеток</param>
        /// <param name="boxes">Список границ (BoundingBox), по которым нужно распределить клетки</param>
        public static void SelectRandomCells(IEnumerable<Cell> cells, IEnumerable<BoundingBox>? boxes)
        {
            int cellsToSelect = GetCellNumberToSelect(boxes);

            if (cells == null || cellsToSelect == 0)
            {
                return;
            }

            // Создаем список доступных для выбора клеток
            var availableCells = cells.Where(c => c.CellState == CellState.AvailableForSelect || c.CellState == CellState.Selected).ToList();

            // Обновляем статус выбранных клеток на доступные для выбора
            foreach (var cell in availableCells.Where(c => c.CellState == CellState.Selected))
            {
                cell.CellState = CellState.AvailableForSelect;
            }

            if (availableCells.Count == 0)
            {
                return;
            }

            var randomCells = new List<Cell>();
            var r = new Random();
            int adjustDistance = 3;
            int additionCells = 0;
            int cnt = 0;

            // Если доступных клеток меньше, чем нужно выбрать, уменьшаем количество для выбора
            if (availableCells.Count < cellsToSelect)
            {
                cellsToSelect = availableCells.Count - additionCells;
            }

            // Выбор клеток в зависимости от количества границ (BoundingBox)
            if (boxes == null || boxes.Count() == 1 || boxes.Count() > 2)
            {
                SelectCellsRandomly(availableCells, randomCells, cellsToSelect, additionCells, adjustDistance, MAX_ATTEMPTS, r);
            }
            else
            {
                var (firstCar, secondCar) = SplitCellsByBoundingBoxes(availableCells, boxes);

                SelectCellsRandomly(firstCar, randomCells, Math.Min(6, firstCar.Count), 0, adjustDistance, MAX_ATTEMPTS, r);
                MarkCellsAsSelected(randomCells);

                randomCells.Clear();
                adjustDistance = 3;

                SelectCellsRandomly(secondCar, randomCells, Math.Min(6, secondCar.Count), 0, adjustDistance, MAX_ATTEMPTS, r);
            }

            MarkCellsAsSelected(randomCells);
        }

        /// <summary>
        /// Случайным образом выбирает клетки из доступных для выбора
        /// </summary>
        /// <param name="availableCells">Список доступных клеток для выбора</param>
        /// <param name="randomCells">Список для хранения выбранных клеток</param>
        /// <param name="cellsToSelect">Количество клеток для выбора</param>
        /// <param name="additionCells">Дополнительное количество клеток для выбора</param>
        /// <param name="adjustDistance">Расстояние для регулировки выбора клеток</param>
        /// <param name="maxAttempts">Максимальное количество попыток</param>
        /// <param name="r">Экземпляр случайного генератора</param>
        private static void SelectCellsRandomly(List<Cell> availableCells, List<Cell> randomCells, int cellsToSelect, int additionCells, int adjustDistance, int maxAttempts, Random r)
        {
            int cnt = 0;
            while (randomCells.Count < cellsToSelect + additionCells && cnt < maxAttempts)
            {
                var cell = availableCells.OrderBy(x => r.Next()).First();
                if (randomCells.All(c => !AreCellsAdjacent(c, cell, adjustDistance)))
                {
                    randomCells.Add(cell);
                }

                cnt++;
                if (cnt >= maxAttempts && adjustDistance != 0)
                {
                    randomCells.Clear();
                    cnt = 0;
                    adjustDistance--;
                }
            }
        }

        /// <summary>
        /// Отмечает клетки как выбранные
        /// </summary>
        /// <param name="cells">Список клеток для отметки</param>
        private static void MarkCellsAsSelected(List<Cell> cells)
        {
            foreach (var cell in cells)
            {
                cell.CellState = CellState.Selected;
            }
        }

        /// <summary>
        /// Разделяет клетки по границам (BoundingBox)
        /// </summary>
        /// <param name="availableCells">Список доступных клеток</param>
        /// <param name="boxes">Список границ (BoundingBox)</param>
        /// <returns>Два списка клеток, распределенных по границам</returns>
        private static (List<Cell> firstCar, List<Cell> secondCar) SplitCellsByBoundingBoxes(List<Cell> availableCells, IEnumerable<BoundingBox> boxes)
        {
            var firstCar = new List<Cell>();
            var secondCar = new List<Cell>();

            foreach (var cell in availableCells)
            {
                if (IsWithinBoxes(cell, boxes.ElementAt(0)))
                {
                    firstCar.Add(cell);
                }
                if (IsWithinBoxes(cell, boxes.ElementAt(1)))
                {
                    secondCar.Add(cell);
                }
            }

            return (firstCar, secondCar);
        }

        /// <summary>
        /// Проверяет, являются ли клетки соседними
        /// </summary>
        /// <param name="c1">Первая клетка</param>
        /// <param name="c2">Вторая клетка</param>
        /// <param name="adjustDistance">Расстояние для регулировки выбора клеток</param>
        /// <returns>Истина, если клетки соседние, иначе ложь</returns>
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

        /// <summary>
        /// select/unselect cell
        /// </summary>
        /// <param name="cell"></param>
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

        /// <summary>
        /// reset cell state to default
        /// </summary>
        /// <param name="cell"></param>
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

        /// <summary>
        /// добавить перекладины 
        /// </summary>
        /// <param name="clickedCell">выбранные клетка</param>
        /// <param name="cells">массив всех клеток</param>
        /// <param name="isVertical">добавить вертикальную или горизонтальную клетку</param>
        public static void AddCrossbarCells(Cell clickedCell, List<Cell> cells, bool isVertical)
        {
            List<Cell> barCells = new List<Cell>();
            // выбор клеток по вертикале или горизонтали
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

        public static void UpdateCellsState(List<Cell> cells, IEnumerable<BoundingBox> boundingBodyBoxes, bool  isDetectionsFixed)
        {
            // Пройти по всем клеткам
            foreach (var cell in cells)
            {
                // Если клетка уже выделена, то пропустить ее
                if (cell.CellState == CellState.Selected)
                {
                    continue;
                }

                // если включена фиксация балок включать проверку
                if (isDetectionsFixed && cell.CellState == CellState.Crossbar)
                {
                    continue;
                }

                // Если клетка пересекается с bounding box, то выделить ее иначе убрать выделение
                //if (IsWithinBoxes(cell, boundingBodyBoxes.Where(body => body.Confidence > 0.7)))
                if (IsWithinBoxes(cell, boundingBodyBoxes))
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
            if (boundingCrossbarBoxes == null || !boundingCrossbarBoxes.Any())
                return;

            var availableCells = cells.Where(cell => cell.CellState == CellState.AvailableForSelect).ToList();

            foreach (var cell in availableCells)
            {
                if (IsWithinOrCrossingBoxes(cell, boundingCrossbarBoxes))
                    cell.CellState = CellState.Crossbar;
                else
                    cell.CellState = CellState.AvailableForSelect;
            }
        }

        /// <summary>
        /// Checks if a cell is within or crossing any of the given bounding boxes
        /// </summary>
        /// <param name="cell">The cell to check</param>
        /// <param name="boundingBoxes">The list of bounding boxes</param>
        /// <returns>True if the cell is within or crossing any bounding box, otherwise false</returns>
        private static bool IsWithinOrCrossingBoxes(Cell cell, IEnumerable<BoundingBox> boundingBoxes)
        {
            foreach (var box in boundingBoxes)
            {
                if (IsWithinOrCrossingBox(cell, box.Box))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a cell is within or crossing a specific bounding box
        /// </summary>
        /// <param name="cell">The cell to check</param>
        /// <param name="bRectangle">The bounding box</param>
        /// <returns>True if the cell is within or crossing the bounding box, otherwise false</returns>
        private static bool IsWithinOrCrossingBox(Cell cell, System.Drawing.Rectangle bRectangle)
        {
            // Create a rectangle for the cell
            var cellRectangle = new System.Drawing.Rectangle(
                Convert.ToInt32(cell.X),
                Convert.ToInt32(cell.Y),
                Convert.ToInt32(cell.Width),
                Convert.ToInt32(cell.Height)
            );

            // Check for intersection
            return bRectangle.IntersectsWith(cellRectangle);
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

        /// <summary>
        /// Проверяет, находится ли клетка внутри границы (BoundingBox)
        /// </summary>
        /// <param name="cell">Клетка</param>
        /// <param name="box">Граница (BoundingBox)</param>
        /// <returns>Истина, если клетка находится внутри границы, иначе ложь</returns>
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

        /// <summary>
        /// Уменьшить выделенную область кузовов
        /// </summary>
        /// <param name="availableCells"></param>
        /// <param name="BoxReductionSide"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Получаем список выбранных клеток
        /// </summary>
        /// <param name="cells">весь список клеток</param>
        /// <returns></returns>
        public static List<Cell> GetSelectedCells(List<Cell> cells)
        {
            // Используем LINQ для фильтрации и преобразования в список
            var selectedCells = cells.Where(c => c.CellState == CellState.Selected).ToList();

            return selectedCells;
        }

        /// <summary>
        /// Получает количество клеток для выбора на основе границ (BoundingBox)
        /// </summary>
        /// <param name="boxes">Список границ (BoundingBox)</param>
        /// <returns>Количество клеток для выбора</returns>
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
                else if (boxes.Count() == 1 && boxes.All(box => box.Width > 100 && box.Width < 650))
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
