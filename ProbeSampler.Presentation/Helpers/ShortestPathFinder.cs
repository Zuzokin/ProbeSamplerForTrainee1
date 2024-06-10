using Google.OrTools.ConstraintSolver;
using System.Collections.Generic;
using System.Linq;

namespace ProbeSampler.Presentation.Helpers
{
    /// <summary>
    /// Класс, отвечающий за поиск кратчайшего пути с использованием библиотеки OR-Tools.
    /// </summary>
    public class ShortestPathFinder
    {
        private readonly double linearSpeed; // Линейная скорость для расчета веса
        private readonly double rotationSpeed; // Скорость вращения для расчета веса

        private List<Cell> cells;
        public long[,] Weights; // Веса между клетками
        public int VehicleNumber = 1; // Количество транспортных средств
        public int[] Starts; // Начальные узлы пути
        public int[] Ends; // Конечные узлы пути
        private int maxCellIndex;
        public long[][] InitialRoute =
        {
            new long[] { }
        };

        /// <summary>
        /// Конструктор класса ShortestPathFinder.
        /// </summary>
        /// <param name="SelectedCells">Список выбранных клеток для поиска пути.</param>
        /// <param name="linearSpeed">Линейная скорость для расчета веса.</param>
        /// <param name="rotationSpeed">Скорость вращения для расчета веса.</param>
        public ShortestPathFinder(List<Cell> SelectedCells, double linearSpeed = 1, double rotationSpeed = 1.2)
        {
            cells = SelectedCells;
            this.linearSpeed = linearSpeed;
            this.rotationSpeed = rotationSpeed;

            // Добавление фиктивной клетки для расчета начального и конечного узлов
            cells.Add(new Cell() { Offset = 100, Rotation = 100 });
            maxCellIndex = cells.FindIndex(cell => cell.Offset == cells.Max(c => c.Offset));
            Starts = new int[] { cells.Count - 1 };
            Ends = new int[] { maxCellIndex };
            Weights = new long[cells.Count, cells.Count];

            // Вычисление весов между клетками
            for (int i = 0; i < cells.Count; i++)
            {
                for (int j = 0; j < cells.Count; j++)
                {
                    if (i != j)
                    {
                        Weights[i, j] = (int)Math.Round(
                            Math.Max(
                                Math.Abs((decimal)((cells[i].Offset.Value - cells[j].Offset.Value) / this.linearSpeed)),
                                Math.Abs((decimal)((cells[i].Rotation.Value - cells[j].Rotation.Value) / this.rotationSpeed)))
                            );
                    }
                }
            }           

            var sortedIndices = cells
                .Select((cell, i) => new KeyValuePair<Cell, int>(cell, i))
                .OrderBy(cell => cell.Key.Offset)
                .Select(x => x.Value)
                .ToArray();

            InitialRoute[0] = sortedIndices.Select(item => (long)item).ToArray();
        }

        /// <summary>
        /// Получение списка клеток из решения пути.
        /// </summary>
        private List<Cell> GetSolution(RoutingModel routing, RoutingIndexManager manager, Assignment solution)
        {
            // Изучение решения
            List<Cell> resultCells = new List<Cell>();
            List<int> resultPath = new();

            var index = routing.Start(0);
            while (routing.IsEnd(index) == false)
            {
                resultPath.Add((int)index);
                index = solution.Value(routing.NextVar(index));
            }
            resultPath.Add((int)index);


            if (maxCellIndex != resultPath[0])
            {
                // todo разобраться в каком порядке возвращается результат
                for (int i = 0; i <= resultPath.Count - 2; i++)
                {
                    resultCells.Add(cells[resultPath[i]]);
                }
            }
            else
            {
                // Обратное восстановление правильного порядка клеток
                for (int i = resultPath.Count - 2; i >= 0; i--)
                {
                    resultCells.Add(cells[resultPath[i]]);
                }
            }         

            return CheckAndReplaceLastMaxCell(resultCells);
        }

        /// <summary>
        /// Поиск кратчайшего пути с использованием алгоритма маршрутизации.
        /// </summary>
        public List<Cell> FindShortestPath()
        {
            // Создание индексного менеджера
            RoutingIndexManager manager = new RoutingIndexManager(Weights.GetLength(0), VehicleNumber, Starts, Ends);

            // Создание модели маршрутизации
            RoutingModel routing = new RoutingModel(manager);

            // Создание и регистрация коллбэка для расчета переходов
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return Weights[fromNode, toNode];
            });

            // Определение стоимости каждого ребра маршрута
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Добавление ограничения на расстояние
            routing.AddDimension(transitCallbackIndex, 0, int.MaxValue, true, "Distance");
            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(100);

            // Get initial solution from routes.
            Assignment initialSolution = routing.ReadAssignmentFromRoutes(InitialRoute, true);

            // Установка эвристики для первого решения
            RoutingSearchParameters searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 10 };

            // Решение задачи
            Assignment solution = routing.SolveWithParameters(searchParameters);

            // Получение результата
            return GetSolution(routing, manager, solution);
        }

        private List<Cell> CheckAndReplaceLastMaxCell(List<Cell> resultCells)
        {
            maxCellIndex = resultCells.FindIndex(cell => cell.Offset == cells.Max(c => c.Offset));
            if (maxCellIndex != resultCells.Count-1)
            {
                (resultCells[maxCellIndex], resultCells[resultCells.Count - 1]) = (resultCells[resultCells.Count - 1], resultCells[maxCellIndex]);
            }
            return resultCells;           
        }
    }
}