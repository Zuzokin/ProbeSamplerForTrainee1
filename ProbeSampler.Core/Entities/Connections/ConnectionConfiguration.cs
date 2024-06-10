namespace ProbeSampler.Core.Entities
{
    /// <summary>
    /// Полная конфигурация подключения.
    /// </summary>
    public class ConnectionConfiguration : Connection, ICloneable
    {
        public string? Name { get; set; }

        public DateTime LastTimeUsed { get; set; }

        public bool IsDebug { get; set; }
        /// <summary>
        /// Подключение к камере.
        /// </summary>
        public CameraConnection CameraConnection { get; set; }
        /// <summary>
        /// Подключение к пробоотборнику.
        /// </summary>
        public SamplerConnection SamplerConnection { get; set; }
        /// <summary>
        /// коэфициент уверенности YOLO.
        /// </summary>
        public float ConfidenceFromConfig { get; set; } = 0.7f;

        public ConnectionConfiguration()
        {
            CameraConnection = new();
            SamplerConnection = new();
        }

        public object Clone()
        {
            var clone = new ConnectionConfiguration
            {
                Id = Id,
                Name = Name,
                LastTimeUsed = LastTimeUsed,
                IsDebug = IsDebug,
                CameraConnection = CameraConnection?.Clone() as CameraConnection ?? new(),
                SamplerConnection = SamplerConnection?.Clone() as SamplerConnection ?? new(),
            };

            return clone;
        }
    }
}
