using Dragablz;

namespace ProbeSampler.WPF
{
    public class CameraDisplayService : ICameraDisplayService
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly WindowsInstanceManager windowsInstanceManager;

        public CameraDisplayService()
        {
            windowsInstanceManager = resolver.GetRequiredService<WindowsInstanceManager>();
        }

        public void OpenInNewTab(Guid id)
        {
            var window = windowsInstanceManager.GetOrCreate();
            if (window != null)
            {
                AddNewTab(window, id);
                window.Show();
                window.Activate();
            }
        }

        public void OpenInNewWindow(Guid id)
        {
            var window = windowsInstanceManager.Create();
            if (window != null)
            {
                AddNewTab(window, id);
                window.Show();
            }
        }

        private void AddNewTab(ChildWindow window, Guid id)
        {
            foreach (var item in window.ViewModel.Tabs)
            {
                (item as SamplerViewModel).Dispose();
            }
            
            window.ViewModel?.Tabs.Clear();
            SamplerViewModel samplerViewModel = new(id, (window as IIdentifierProvider).Identifier);
            window.ViewModel?.Tabs.Add(samplerViewModel);
            
            TabablzControl.SelectItem(samplerViewModel);
        }
    }
}
