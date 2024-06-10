namespace ProbeSampler.Presentation
{
    /// <summary>
    /// Статус ячейки
    /// </summary>
    public enum CellState
    {
        Default,
        /// <summary>
        /// Доступна для выбора
        /// </summary>
        AvailableForSelect,
        Selected
    }

    /// <summary>
    /// Ячейка сетки, отображающаяся поверх изображения
    /// </summary>
    public class Cell : BindableBase
    {
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private CellState cellState;
        /// <summary>
        /// Статус ячейки
        /// </summary>
        public CellState CellState
        {
            get => cellState;
            set => SetProperty(ref cellState, value);
        }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }

        public Cell(double x, double y, double width, double height, object? relatedObj = default)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
