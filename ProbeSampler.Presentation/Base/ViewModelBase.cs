using ReactiveUI.Validation.Helpers;

namespace ProbeSampler.Presentation
{
    public abstract class ViewModelBase : ReactiveValidationObject, IActivatableViewModel, IEnableLogger
    {
        /// <summary>
        /// Id ViewModel.
        /// </summary>
        public string Id { get; } = Guid.NewGuid().ToString();

        [ObservableAsProperty]
        public virtual string? ViewName { get; }

        internal IReadonlyDependencyResolver resolver = Locator.Current;

        public ViewModelActivator Activator { get; }

        public ViewModelBase()
        {
            Activator = new();

            this.WhenActivated(d =>
            {
                Disposable.Create(() => SetupDeactivate())
                    .DisposeWith(d);
                this.Log().Debug($"Seting up commands {this.GetType().Name}");
                SetupCommands();
                this.Log().Debug($"Seting up subscriptions {this.GetType().Name}");
                SetupSubscriptions(d);
                this.Log().Debug($"Seting up validation {this.GetType().Name}");
                SetupValidation(d);
                this.Log().Debug($"Seting up start {this.GetType().Name}");
                SetupStart();
            });
        }

        protected virtual void SetupCommands()
        {
        }

        protected virtual void SetupStart()
        {
        }

        protected virtual void SetupValidation(CompositeDisposable d)
        {
        }

        protected virtual void SetupSubscriptions(CompositeDisposable d)
        {
        }

        protected virtual void SetupDeactivate()
        {
        }
    }
}
