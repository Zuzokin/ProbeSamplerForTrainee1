namespace ProbeSampler.Presentation
{
    public class RegisterDependency : RegisterDependencyBase
    {
        public override void ConfigService()
        {
            mutable.RegisterLazySingleton(() => new ConfigManagerViewModel());
            mutable.RegisterLazySingleton(() => new AppSettingsViewModel());
            mutable.Register(() => new ChangeAdminPasswordViewModel(resolver.GetRequiredService<IAdminAccessService>()));
            mutable.Register(() => new AdminPasswordRequestViewModel(resolver.GetRequiredService<IAdminAccessService>()));
            mutable.Register(() => new SetAdminPasswordViewModel(resolver.GetRequiredService<IAdminAccessService>()));
            // mutable.Register(() => new ConfigEditorViewModel());
            // mutable.Register(() => new SamplerViewModel());

            Func<bool, ICamera> cameraFactory = (isDebug) =>
            {
                return isDebug ? resolver.GetRequiredService<ICamera>("DEBUG") : resolver.GetRequiredService<ICamera>("RTSP");
            };

            mutable.Register(() => new CameraViewModel(cameraFactory, resolver.GetRequiredService<IPathService>()), typeof(CameraViewModel));
        }
    }
}
