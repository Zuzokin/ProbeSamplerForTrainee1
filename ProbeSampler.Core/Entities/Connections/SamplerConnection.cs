namespace ProbeSampler.Core.Entities
{
    /// <summary>
    /// Подключение к проботборнику.
    /// </summary>
    public class SamplerConnection : Connection, ICloneable
    {
        public string? URL { get; set; }
        /// <summary>
        /// Имя пользователя OPC сервера.
        /// </summary>
        public string? UserName { get; set; }
        /// <summary>
        /// Пароль пользователя OPC сервера.
        /// </summary>
        public string? Password { get; set; }
        /// <summary>
        /// Координаты нулевой позиции по X.
        /// </summary>
        // public double? PositionX { get; set; }
        ///// <summary>
        ///// Координаты нулевой позиции по Y
        ///// </summary>
        // public double? PositionY { get; set; }
        ///// <summary>
        ///// Длина клюва
        ///// </summary>
        // public double? BeakLength { get; set; }
        ///// <summary>
        ///// Скорость поворота (градусов в секунду)
        ///// </summary>
        // public double? RateOfTurn { get; set; }
        ///// <summary>
        ///// Скорость движения
        ///// </summary>
        // public double? RateOfMovement { get; set; }
        ///// <summary>
        ///// Радиус при отрисовке на экране
        ///// </summary>
        // public double? Radius { get; set; }

        public double? LinearCalculationCoeffA { get; set; }

        public double? LinearCalculationCoeffB { get; set; }

        public double? RotationCalculationCoeffA { get; set; }

        public double? RotationCalculationCoeffB { get; set; }

        public double? RotationCalculationCoeffC { get; set; }

        public int? BeakCoeff { get; set; }

        public int? UnreachablePixels { get; set; }

        public int? CellsNumberToSelect { get; set; }

        public double? CellsToSelectMultiplier { get; set; }

        public int? BigTruckWidth { get; set; }
        public double? OffsetVelocity { get; set; }
        public double? RotationVelocity { get; set; } 

        /// <summary>
        /// Отрисовывать на изображении.
        /// </summary>
        public bool Visible { get; set; }

        public object Clone()
        {
            var clone = new SamplerConnection
            {
                URL = this.URL,
                UserName = this.UserName,
                Password = this.Password,
                // PositionX = this.PositionX,
                // PositionY = this.PositionY,
                // BeakLength = this.BeakLength,
                // RateOfTurn = this.RateOfTurn,
                // RateOfMovement = this.RateOfMovement,
                // Radius = this.Radius,
                Visible = this.Visible,
                LinearCalculationCoeffA = this.LinearCalculationCoeffA,
                LinearCalculationCoeffB = this.LinearCalculationCoeffB,
                RotationCalculationCoeffA = this.RotationCalculationCoeffA,
                RotationCalculationCoeffB = this.RotationCalculationCoeffB,
                RotationCalculationCoeffC = this.RotationCalculationCoeffC,
                BeakCoeff = this.BeakCoeff,
                UnreachablePixels = this.UnreachablePixels,
                CellsNumberToSelect = this.CellsNumberToSelect,
                CellsToSelectMultiplier = this.CellsToSelectMultiplier,
                BigTruckWidth = this.BigTruckWidth,
                OffsetVelocity = this.OffsetVelocity,
                RotationVelocity = this.RotationVelocity,
            };

            return clone;
        }
    }
}
