namespace ProbeSampler.Presentation
{
    /// <summary>
    /// Статус ячейки.
    /// </summary>
    public enum CellState
    {
        Default,
        /// <summary>
        /// Доступна для выбора.
        /// </summary>
        AvailableForSelect,
        Selected,
        Crossbar,
    }

    /// <summary>
    /// Ячейка сетки, отображающаяся поверх изображения.
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
        /// Статус ячейки.
        /// </summary>
        public CellState CellState
        {
            get => cellState;
            set => SetProperty(ref cellState, value);
        }
        /// <summary>
        /// Координата X (px)
        /// </summary>
        public double X { get; private set; }
        /// <summary>
        /// Координата Y (px)
        /// </summary>
        public double Y { get; private set; }
        /// <summary>
        /// Ширина клетки
        /// </summary>
        public double Width { get; private set; }
        /// <summary>
        /// Высота клетки
        /// </summary>
        public double Height { get; private set; }
        /// <summary>
        /// Рассчитанное смещение для пробоотборника
        /// </summary>
        public double? Offset { get; set; } = null;
        /// <summary>
        /// Рассчитанный поворот для пробоотборника
        /// </summary>
        public double? Rotation { get; set; } = null;

        public Cell(double x = 0, double y = 0, double width = 0, double height = 0, object? relatedObj = default)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
