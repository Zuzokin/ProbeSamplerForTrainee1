using System.Reflection;

namespace ProbeSampler.WPF
{
    public class RegisterDependency : RegisterDependencyBase
    {
        public override void ConfigService()
        {
            mutable.RegisterLazySingleton(() => new LocalFileStorage(), typeof(IStorageService), "localFileStorage");
            mutable.RegisterLazySingleton(() => new ConectionConfigManager(resolver.GetRequiredService<IStorageService>("localFileStorage")), typeof(IConnectionConfigurationManager));
            mutable.RegisterLazySingleton(() => new ApplicationSettingsManager(resolver.GetRequiredService<IStorageService>("localFileStorage")), typeof(IApplicationSettingsManager));
            mutable.RegisterLazySingleton(() => new CameraDisplayService(), typeof(ICameraDisplayService));
            mutable.RegisterLazySingleton(() => new WinCameraLocator(), typeof(ICameraLocator));
            mutable.Register(() => new ChildWindow(), typeof(ChildWindow));
            mutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
        }

        public class ConventionalViewLocator : IViewLocator
        {
            public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
            {
                string viewModelName = viewModel?.GetType()?.Name ?? string.Empty;
                string viewTypeName = viewModelName.Replace("ViewModel", "View");

                if (string.IsNullOrWhiteSpace(viewTypeName) || string.IsNullOrWhiteSpace(viewModelName))
                {
                    return null;
                }

                try
                {
                    Type? viewType = Type.GetType(viewTypeName);
                    if (viewType == null)
                    {
                        this.Log().Error($"Can't found {viewTypeName} for {viewModelName}.");
                        return null;
                    }

                    return Activator.CreateInstance(viewType) as IViewFor;
                }
                catch (Exception)
                {
                    this.Log().Error($"{viewTypeName} cannot be instantiated.");
                    throw;
                }
            }
        }
    }
}
