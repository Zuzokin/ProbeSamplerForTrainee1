namespace ProbeSampler.Core.Enums
{
    public enum OPCUAConnectionStatus
    {
        None,
        Created,
        Disconnected,
        Connected,
        Connecting,
        Reconnecting,
        Reconnected,
        Error,
        ErrorNoConnectionToProbe,
        ErrorNoConnectionToGate,

    }
}
