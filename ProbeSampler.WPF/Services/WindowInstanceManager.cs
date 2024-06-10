using Dragablz;

namespace ProbeSampler.WPF
{
    public class WindowsInstanceManager
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly HashSet<ChildWindow> childWindows = new();

        private MainWindow? mainWindow;

        public MainWindow MainWindow
        {
            get
            {
                if (mainWindow == null)
                {
                    mainWindow = new MainWindow();
                    mainWindow.Closed += MainWindow_Closed;
                }

                return mainWindow;
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            foreach (var childWindow in childWindows)
            {
                childWindow.Close();
            }

            DialogHelper.ChildWindows.Clear();
        }

        public ChildWindow GetOrCreate()
        {
            var window = childWindows.Any() ?
                childWindows.First() : Create();

            return window;
        }

        public ChildWindow Create()
        {
            var window = resolver.GetRequiredService<ChildWindow>();
            window.Closed += Window_Closed;
            childWindows.Add(window);
            return window;
        }

        private bool CheckWindowExistence(ChildWindow windowToCheck)
        {
            bool a = childWindows.Any(w => w == windowToCheck);
            return a;
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            if (sender is not ChildWindow window)
            {
                return;
            }

            window.Closed -= Window_Closed;
            (window.ViewModel as IDisposable)?.Dispose();
            childWindows.Remove(window);
            if (window.ViewModel != null)
            {
                DialogHelper.ChildWindows.Remove(window.ViewModel);
            }

            var redundantTabs = TabablzControl.GetLoadedInstances()
                /*                .SelectMany(
                                    tabControl => tabControl.Items)*/
                .ToList();
        }
    }
}
