using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation
{
    [SingleInstanceView]
    public class MainViewModel : ReactiveObject, IActivatableViewModel, IScreen, IEnableLogger
    {
        private IMutableDependencyResolver mutable = Locator.CurrentMutable;
        private IReadonlyDependencyResolver resolver = Locator.Current;

        public ViewModelActivator Activator { get; }

        [ObservableAsProperty] public string? CurrentViewName { get; }

        public RoutingState Router { get; private set; } = new RoutingState();

        public ReactiveCommand<Unit, IRoutableViewModel?> NavigateBackCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

        public MainViewModel()
        {
            Activator = new();
            RegisterDependencies();

            this.WhenActivated(d =>
            {
                /*                Disposable.Create(() => SetupDeactivate())
                                    .DisposeWith(d);*/

                this.Router.CurrentViewModel
                    .Select(vm => vm as RoutableViewModelBase)
                    .Where(vmBase => vmBase != null)
                    .SelectMany(vmBase => vmBase.WhenAnyValue(vm => vm.ViewName))
                    .ToPropertyEx(this, x => x.CurrentViewName)
                    .DisposeWith(d);
            });

            var manager = resolver.GetRequiredService<ConfigManagerViewModel>();

            var canGoBack = this.WhenAnyValue(x => x.Router.NavigationStack.Count)
                .Select(count => count > 1);
            var canGoToSettings = Router.CurrentViewModel
                .Select(current => current is not AppSettingsViewModel);
            NavigateBackCommand = ReactiveCommand.CreateFromObservable(() => Router.NavigateBack.Execute(Unit.Default), canGoBack);
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings, canGoToSettings);
            Router.NavigateAndReset.Execute(manager);

#if DEBUG

            SBMessage startMessage = new SBMessage(
                "Добро пожаловать. Если вы видете это сообщение, то вы находитесь в DEBUG режиме.",
                action: (object? argument) => this.Log().Debug(argument),
                actionArgument: "Debug message closed",
                actionCaption: "ЗАКРЫТЬ",
                durationOverride: TimeSpan.FromSeconds(10)
                );

            Task.Factory.StartNew(() => Thread.Sleep(2500)).ContinueWith(
                t =>
            {
                MessageBus.Current.SendMessageToMainApplicationWindow(startMessage);
            }, TaskScheduler.FromCurrentSynchronizationContext());

#endif
        }

        private void OpenSettings()
        {
            AppSettingsViewModel appSettingsViewModel = resolver.GetRequiredService<AppSettingsViewModel>();
            Router.Navigate.Execute(appSettingsViewModel);
        }

        private void RegisterDependencies()
        {
            mutable.RegisterLazySingleton(() => this, typeof(IScreen));

            string starupPath = Environment.CurrentDirectory;
            List<string> dlls = Directory.GetFiles(starupPath).Where(f => f.Contains(".dll"))
                .Select(f => f.Replace(starupPath + @"\", "")).Where(n => n.Contains("ProbeSampler")).ToList();
            foreach (string dll in dlls)
            {
                List<object> tmp = (
                    from t in Assembly.LoadFrom(dll).GetTypes()
                    where t.IsSubclassOf(typeof(RegisterDependencyBase))
                    select System.Activator.CreateInstance(t)).ToList();
                tmp.Clear();
            }
        }

        /*        private void SetupDeactivate()
                {

                }*/
    }
}
