namespace ProbeSampler.Presentation
{
    public abstract class RoutableViewModelBase : ViewModelBase, IRoutableViewModel
    {
        public virtual string? UrlPathSegment
        {
            get
            {
                return this.GetType().Name.Replace("ViewModel", "").ToLower();
            }
        }

        public IScreen HostScreen { get; protected set; }

        public RoutableViewModelBase(IScreen? hostScreen = null)
        {
            HostScreen = hostScreen ?? Locator.Current.GetRequiredService<IScreen>();
        }
    }
}
