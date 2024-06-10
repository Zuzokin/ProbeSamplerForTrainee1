using ProbeSampler.Core.Services;
using ProbeSampler.Core.Services.Camera;
using ProbeSampler.Core.Services.Opc;

namespace ProbeSampler.Core
{
    public class RegisterDependency : RegisterDependencyBase
    {
        public override void ConfigService()
        {
            mutable.Register(() => new OpcUaClientHelper(), typeof(IOpcUaClient));
            mutable.Register(() => new DebugStaticImageCamera(), typeof(ICamera), "DEBUG");
            mutable.Register(() => new RtspCameraService(), typeof(ICamera), "RTSP");
            mutable.RegisterLazySingleton(() => new AdminAccessService(), typeof(IAdminAccessService));
            // mutable.Register(() => new BoxSearchService(), typeof(IImageProcessingService), "boxSearchOCV");
        }
    }
}
