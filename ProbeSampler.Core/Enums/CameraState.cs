namespace ProbeSampler.Core.Enums
{
    /// <summary>
    /// Состояние камеры.
    /// </summary>
    public enum CameraState
    {
        Stopped,
        Connecting,
        Running,
        Pause,
        Stopping,
        Error,
    }
}
