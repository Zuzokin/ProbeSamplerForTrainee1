using Dragablz;
using System.Windows;

namespace ProbeSampler.WPF
{
    public class CustomInterTabClient : IInterTabClient
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly WindowsInstanceManager windowsInstanceManager;

        public CustomInterTabClient()
        {
            windowsInstanceManager = resolver.GetRequiredService<WindowsInstanceManager>();
        }

        public INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source)
        {
            var childWindow = windowsInstanceManager.Create();

            childWindow.TabablzControl.Items.Clear();

            return new NewTabHost<Window>(childWindow, childWindow?.TabablzControl);
        }

        public TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window)
        {
            if (window is ChildWindow)
            {
                return TabEmptiedResponse.CloseWindowOrLayoutBranch;
            }

            return TabEmptiedResponse.DoNothing;
        }
    }
}
