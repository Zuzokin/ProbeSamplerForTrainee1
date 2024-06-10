namespace ProbeSampler.Core
{
    public static class ResolverExtensions
    {
        public static T GetRequiredService<T>(this IReadonlyDependencyResolver resolver, string? contract = null)
        {
            var service = resolver.GetService<T>(contract: contract);
            if (service == null)
            {
                throw new ArgumentException($"Can't locate {typeof(T).Name}{(contract != null ? " with contract " + contract : "")} service.");
            }

            return service;
        }
    }
}
