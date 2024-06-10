using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation.Helpers
{
    public static class DialogHelper
    {
        private static IReadonlyDependencyResolver resolver = Locator.Current;

        public static Dictionary<ChildWindowViewModel, HashSet<ViewModelBase>> ChildWindows { get; } = new Dictionary<ChildWindowViewModel, HashSet<ViewModelBase>> { };

        /// <summary>
        /// Открывает указанную ViewModel в диалоге по указанному идентификатору.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dialogIdentifier"></param>
        /// <returns></returns>
        public static async Task<bool> ShowDialog<T>(string dialogIdentifier = "RootDialog")
            where T : class
        {
            var view = resolver.GetRequiredService<IViewFor<T>>();
            var viewModel = resolver.GetRequiredService<T>();
            view.ViewModel = viewModel;
            return await DialogHost.Show(view, dialogIdentifier) is bool bResult && bResult;
        }
    }
}
