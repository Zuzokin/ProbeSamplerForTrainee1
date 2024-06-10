using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSampler.Core.Enums
{
    /// <summary>
    /// режим, отвечающий за то что будет делать пользователь по нажатию мыши, добавлять убирать клетку или добавлять/убирать перекладину
    /// </summary>
    public enum GridEditMode
    {
        Cells,
        Crossbars
    }
}
