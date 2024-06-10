using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSampler.Core.Enums
{
    /// <summary>
    /// Тип машины по ее размеру и количеству кузовов
    /// Размер брался на глаз и в CellHelper при определении типа размер измеряется в пикселях
    /// </summary>
    public enum TruckType
    {
        /// <summary>
        /// нет боксов с типом "body"
        /// </summary>
        None,
        /// <summary>
        /// обычный
        /// </summary>
        Average,
        /// <summary>
        /// два кузова
        /// </summary>
        TwoTrailers,
        /// <summary>
        /// большой
        /// </summary>
        Big,
        /// <summary>
        /// маленький
        /// </summary>
        Little,
        /// <summary>
        /// почти на весь экран кузов
        /// </summary>
        UltraLargeChonker,
    }
}
