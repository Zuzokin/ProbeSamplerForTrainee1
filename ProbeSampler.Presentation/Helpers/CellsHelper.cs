using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using DynamicData.Kernel;
using ProbeSampler.Core.Services.Processing.Core;

namespace ProbeSampler.Presentation.Helpers
{
    public static class CellsHelper
    {
        //public static int randomCellsQuantity { get; private set; } = 1;

        public static void SelectRandomCells(List<Cell> cells, int randomCellsQuantity)
        {
            if (cells == null) return;

            var availableCells = new List<Cell>();

            foreach (var cell in cells.Where(c => c.CellState == CellState.AvailableForSelect))
            {
                availableCells.Add(cell);
            }

            if (availableCells.Count == 0) return;

            availableCells = ReduceBox(availableCells, BoxTrim.All);
            availableCells = ReduceBox(availableCells, BoxTrim.Top);
            availableCells = ReduceBox(availableCells, BoxTrim.Right);
            availableCells = ReduceBox(availableCells, BoxTrim.Right);



            var r = new Random();
            var randomCells = new List<Cell>();
            // TODO: сделать уведомление, что было выбрано меньше точек, чем задано, если cnt > 1000
            int cnt = 0;
            while (randomCells.Count < randomCellsQuantity && cnt < 1000)
            {
                var cell = availableCells.OrderBy(x => r.Next()).First();
                if (randomCells.All(c => !AreCellsAdjacent(c, cell, 2)))
                {
                    randomCells.Add(cell);
                }
                cnt++;
            }
            //if (cnt > 999)
            //{
            //    MessageBox.Show($"Было выбрано {randomCells.Count} клетки вместо {randomCellsQuantity}, попробуйте еще раз", "Не удалось выбрать достаточно клеток случайным образом", MessageBoxButton.OK);
            //}
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
            isAdjacent = isAdjacent && (Math.Abs(cell1.X - cell2.X) != cell1.Width * distance || Math.Abs(cell1.Y - cell2.Y) != cell1.Height * distance);

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
                    cell.CellState = CellState.AvailableForSelect;
                    break;
                case CellState.Default:
                    cell.CellState = CellState.Selected;                    
                    break;

            }
        }

        public static void UpdateCellsState(List<Cell> cells, IEnumerable<BoundingBox> boundingBoxes)
        {
            // Ставим значение по умолчанию для всех клеток
            foreach (var cell in cells.Where(c => c.CellState == CellState.AvailableForSelect))
            {
                cell.CellState = CellState.Default;
            }

            // Пройти по всем bounding boxes и выделить ячейки
            foreach (var box in boundingBoxes)
            {
                // Найти ячейки, которые пересекаются с bounding box
                foreach (var cell in cells.Where(c => c.CellState == CellState.Default))
                {
                    if (IsIntersecting(cell, box))
                    {
                        cell.CellState = CellState.AvailableForSelect;
                    }
                }
            }
        }

        private static bool IsIntersecting(Cell cell, BoundingBox box)
        {
            var bRectangle = box.Box;
            bool withinXBounds = cell.X >= bRectangle.X && (cell.X + cell.Width) <= (bRectangle.X + bRectangle.Width);
            bool withinYBounds = cell.Y >= bRectangle.Y && (cell.Y + cell.Height) <= (bRectangle.Y + bRectangle.Height);

            return withinXBounds && withinYBounds;
        }

        public static void UpdateCells(List<Cell> cells, double gridOverlayControlX, double gridOverlayControlY, double gridOverlayWidth, double gridOverlayHeight, int gridOverlayCellHeight)
        {
            if (!(gridOverlayControlX > 0
                    && gridOverlayControlY > 0
                    && gridOverlayWidth > 0
                    && gridOverlayHeight > 0
                    && gridOverlayCellHeight > 0))
                return;

            for (double x = gridOverlayControlX; x < gridOverlayControlX + gridOverlayWidth; x += gridOverlayCellHeight)
            {
                for (double y = gridOverlayControlY; y < gridOverlayControlY + gridOverlayHeight; y += gridOverlayCellHeight)
                {
                    cells.Add(new(x, y, gridOverlayCellHeight, gridOverlayCellHeight));
                }
            }
        }

        public static void ResetSelectedCells(List<Cell> cells)
        {
            foreach (var cell in cells.Where(c => c.CellState == CellState.Selected))
            {
                cell.CellState = CellState.AvailableForSelect;
            }
        }

        public static List<Cell> ReduceBox(List<Cell> availableCells, BoxTrim BoxReductionSide = BoxTrim.All)
        {
            var ReducedBox = new List<Cell>();
            (double minX, double minY, double maxX, double maxY) = FindMinMaxXY(availableCells);
            switch (BoxReductionSide)
            {
                case BoxTrim.Left:
                    foreach (Cell cell in availableCells)
                        if (cell.X > minX)
                            ReducedBox.Add(cell);
                    break;
                case BoxTrim.Top:
                    foreach (Cell cell in availableCells)
                        if (cell.Y > minY)
                            ReducedBox.Add(cell);
                    break;
                case BoxTrim.Right:
                    foreach (Cell cell in availableCells)
                        if (cell.X < maxX)
                            ReducedBox.Add(cell);
                    break;
                case BoxTrim.Bottom:
                    foreach (Cell cell in availableCells)
                        if (cell.Y < maxY)
                            ReducedBox.Add(cell);
                    break;
                case BoxTrim.All:
                    foreach (Cell cell in availableCells)
                        if (cell.X > minX && cell.Y > minY && cell.X < maxX && cell.Y < maxY)
                            ReducedBox.Add(cell);
                    break;
                default:
                    break;
            }
            return ReducedBox;
        }

        private static (double, double, double, double) FindMinMaxXY(List<Cell> cells)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = 0;
            double maxY = 0;
            foreach (Cell cell in cells)
            {
                if(cell.X < minX) minX = cell.X;
                if(cell.Y < minY) minY = cell.Y;
                if(cell.X > maxX) maxX = cell.X;
                if(cell.Y > maxY) maxY = cell.Y;
            }
            return (minX, minY, maxX, maxY);
        }

        public static List<Cell> GetSelectedCells(List<Cell> cells)
        {
            var SelectedCells = new List<Cell>();
            foreach (var cell in cells.Where(c => c.CellState == CellState.Selected))
            {
                SelectedCells.Add(cell);
            }
            return SelectedCells;
        }

    }


}
