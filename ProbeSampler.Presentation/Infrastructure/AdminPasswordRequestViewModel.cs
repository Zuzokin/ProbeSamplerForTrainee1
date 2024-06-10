namespace ProbeSampler.Presentation
{
    public class AdminPasswordRequestViewModel : ViewModelBase
    {
        private readonly IAdminAccessService adminAccessService;

        /// <summary>
        /// Пароль
        /// </summary>
        [Reactive]
        public string Password { get; set; } = string.Empty;

        public AdminPasswordRequestViewModel(IAdminAccessService? adminAccessService = null) 
        {
            this.adminAccessService = adminAccessService ?? resolver.GetRequiredService<IAdminAccessService>();
        }
    }
}
