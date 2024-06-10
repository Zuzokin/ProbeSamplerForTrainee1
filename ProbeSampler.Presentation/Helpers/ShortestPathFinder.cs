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
        private int n;
        public int VehicleNumber = 1; // Количество транспортных средств
        public int[] Starts; // Начальные узлы пути
        public int[] Ends; // Конечные узлы пути
        private int maxCellIndex;
        public long[][] InitialRoute =
        {
            new long[] { }
        };
        private long InitialRouteTime;
        private long ShortPathRouteTime;
        private long DijkstraPathRouteTime;
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
            //cells.Add(new Cell() { Offset = 100, Rotation = 100 });
            cells.Insert(0, new Cell() { Offset = 100, Rotation = 100 });
            maxCellIndex = cells.FindIndex(cell => cell.Offset == cells.Max(c => c.Offset));
            //Starts = new int[] { cells.Count - 1 };
            //Ends = new int[] { maxCellIndex };            
            Starts = new int[] { 0 };
            Ends = new int[] { maxCellIndex };
            Weights = new long[cells.Count, cells.Count];
            n = cells.Count;

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

            for (int i = sortedIndices.Length - 1; i >= 1; i--)
            {
                InitialRouteTime += (long)Math.Round(
                        Math.Max(
                                Math.Abs((decimal)((cells[sortedIndices[i]].Offset.Value - cells[sortedIndices[i - 1]].Offset.Value) / this.linearSpeed)),
                                Math.Abs((decimal)((cells[sortedIndices[i]].Rotation.Value - cells[sortedIndices[i - 1]].Rotation.Value) / this.rotationSpeed)))
                            );
            }          
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
                for (int i = 1; i < resultPath.Count; i++)
                {
                    resultCells.Add(cells[resultPath[i]]);
                }
            }
            else
            {
                // Обратное восстановление правильного порядка клеток
                for (int i = resultPath.Count - 1; i > 0; i--)
                {
                    resultCells.Add(cells[resultPath[i]]);
                }
            }

            CheckAndReplaceLastMaxCell(resultCells);

            ShortPathRouteTime += (long)Math.Round(
                        Math.Max(
                                Math.Abs((decimal)((cells[0].Offset.Value - 0) / this.linearSpeed)),
                                Math.Abs((decimal)((cells[0].Rotation.Value - 0) / this.rotationSpeed)))
                            );

            for (int i = 1; i < resultCells.Count(); i++)
            {
                ShortPathRouteTime += (long)Math.Round(
                        Math.Max(
                                Math.Abs((decimal)((cells[i].Offset.Value - cells[i - 1].Offset.Value) / this.linearSpeed)),
                                Math.Abs((decimal)((cells[i].Rotation.Value - cells[i - 1].Rotation.Value) / this.rotationSpeed)))
                            );
            }


            // todo get back this shit
            //if (ShortPathRouteTime > InitialRouteTime)
            //{
            resultCells = cells.OrderBy(cell => cell.Offset).ToList();
            resultCells.RemoveAt(0);
            //}

            

            return resultCells;
        }

        public List<Cell> FindShortestPathDijkstra()
        {
            List<int> resultPath = new();
            List<Cell> resultCells = new List<Cell>();

            int startVertex = Starts[0];
            int destinationVertex = Ends[0];
            //int startVertex = 0;
            //int destinationVertex = 12;

            long[,] dp = new long[1 << n, n];
            long[,] path = new long[1 << n, n];
            for (int i = 0; i < (1 << n); i++)
                for (int j = 0; j < n; j++)
                    dp[i, j] = -1;

            long DijkstraPathRouteTime = tsp(1, startVertex, destinationVertex, dp, path);
            // todo remove temp debug
            Console.WriteLine($"Минимальное расстояние: {DijkstraPathRouteTime}");

            //resultPath.Add(startVertex);
            GetPath(1 | (1 << startVertex), startVertex, path, resultPath);
            //Console.WriteLine(destinationVertex);

            for (int i = 0; i < resultPath.Count; i++)
            {
                resultCells.Add(cells[resultPath[i]]);
            }

            return resultCells;
        }

        private long tsp(int mask, int pos, int destination, long[,] dp, long[,] path)
        {
            if (mask == (1 << n) - 1)
                return Weights[pos, destination]; // Вернуть расстояние от последней вершины к конечной

            if (dp[mask, pos] != -1)
                return dp[mask, pos];

            long minDistance = int.MaxValue;
            int nextVertex = -1;

            for (int v = 0; v < n; v++)
            {
                if ((mask & (1 << v)) == 0)
                {
                    long newDistance = Weights[pos, v] + tsp(mask | (1 << v), v, destination, dp, path);
                    if (newDistance < minDistance)
                    {
                        minDistance = newDistance;
                        nextVertex = v;
                    }
                }
            }

            dp[mask, pos] = minDistance;
            path[mask, pos] = nextVertex;
            return minDistance;
        }

        private void GetPath(int mask, int pos, long[,] path, List<int> resultPath)
        {
            if (mask == (1 << n) - 1)
                return;            

            int nextVertex = (int)path[mask, pos];
            resultPath.Add(nextVertex);

            GetPath(mask | (1 << nextVertex), nextVertex, path, resultPath);
        }        

        /// <summary>
        /// Поиск кратчайшего пути с использованием алгоритма маршрутизации.
        /// </summary>
        public List<Cell> FindShortestPathORTools()
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
            // routing.SetFirstSolutionEvaluator(transitCallbackIndex);

            // Добавление ограничения на расстояние
            routing.AddDimension(transitCallbackIndex, 0, 150, true, "Distance");
            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(100);

            // Get initial solution from routes.
            Assignment initialSolution = routing.ReadAssignmentFromRoutes(InitialRoute, true);

            // Установка эвристики для первого решения
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
            searchParameters.UseDepthFirstSearch = true;
            //searchParameters.UseFullPropagation = true;
            searchParameters.OptimizationStep = 5;
            searchParameters.FirstSolutionOptimizationPeriod = 5;

            searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = 20 };

            // Решение задачи
            Assignment solution = routing.SolveWithParameters(searchParameters);

            var status = routing.GetStatus();
            
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