namespace ProbeSampler.Presentation.Extensions
{
    public static class ConnectionConfigurationExtensions
    {
        public static bool IsNewObject(this ConnectionConfiguration connectionConfiguration, IConnectionConfigurationManager manager)
        {
            var connection = manager.Get(connectionConfiguration.Id);
            return connection == null;
        }
    }
}
