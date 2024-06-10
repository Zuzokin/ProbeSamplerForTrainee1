using System.Reactive.Linq;

namespace ProbeSampler.Core.Services
{
    public class AdminAccessService : IAdminAccessService, IEnableLogger
    {
        internal IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly IApplicationSettingsManager applicationSettingsManager;
        private BehaviorSubject<bool> isAccessGranted = new BehaviorSubject<bool>(false);
        private BehaviorSubject<bool> isAdminPasswordSet = new BehaviorSubject<bool>(false);

        public IObservable<bool> IsAccessGrantedObservable => isAccessGranted.AsObservable();

        public IObservable<bool> IsPasswordSetObservable => isAdminPasswordSet.AsObservable();

        bool IAdminAccessService.IsPasswordSet => isAdminPasswordSet.Value;

        bool IAdminAccessService.IsAccessGranted => isAdminPasswordSet.Value;

        public AdminAccessService()
        {
            applicationSettingsManager = resolver.GetRequiredService<IApplicationSettingsManager>();
            applicationSettingsManager.ApplicationSettingsObservable.Subscribe(settings =>
            {
                isAdminPasswordSet.OnNext(!string.IsNullOrEmpty(settings.AdminPassword));
            });
        }

        public bool ChangeAdminPassword(string newPassword)
        {
            bool result = false;
            var settings = applicationSettingsManager.GetSettings();

            if (string.IsNullOrWhiteSpace(newPassword) || settings == null)
            {
                return false;
            }

            try
            {
                settings.AdminPassword = PasswordHasher.GenerateIdentityV3Hash(newPassword);
                applicationSettingsManager.UpdateSettings(settings, save: true);
                result = true;
            }
            catch (Exception ex)
            {
                this.Log().Error(ex);
                result = false;
            }

            if (result)
            {
                isAccessGranted.OnNext(false);
            }

            return result;
        }

        public bool IsPasswordValid(string password)
        {
            bool result = false;
            var settings = applicationSettingsManager.GetSettings();

            if (string.IsNullOrWhiteSpace(password)
                || settings == null
                || string.IsNullOrWhiteSpace(settings.AdminPassword))
            {
                return false;
            }

            try
            {
                result = PasswordHasher.VerifyIdentityV3Hash(password, settings.AdminPassword);
            }
            catch (Exception ex)
            {
                this.Log().Error(ex);
                result = false;
            }

            if (result)
            {
                isAccessGranted.OnNext(true);
            }

            return result;
        }
    }
}