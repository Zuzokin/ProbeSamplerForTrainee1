using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSampler.Core.Enums
{
    /// <summary>
    /// состояние пробоотбрника
    /// </summary>
    public enum SpsStatus
    {
        //Disconnected = 0,
        /// <summary>
        /// Завершил отбор
        /// </summary>
        Completed = 1,
        /// <summary>
        /// В процессе отбора
        /// </summary>
        InProgress = 2,
        /// <summary>
        /// ожидает передачи значений в OPC сервер
        /// </summary>
        Waiting = 255
    }
}
