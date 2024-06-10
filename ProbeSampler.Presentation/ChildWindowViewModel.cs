using ProbeSampler.Presentation.Helpers;
using System.Collections.ObjectModel;

namespace ProbeSampler.Presentation
{
    public class ChildWindowViewModel : ViewModelBase, ISBMessageSender, IDisposable
    {
        private string identifier;

        public ObservableCollection<ITabContent> Tabs { get; }

        public string Identifier => identifier;

        public ChildWindowViewModel(string identifier)
        {
            this.identifier = identifier;
            Tabs = new ObservableCollection<ITabContent>();
            DialogHelper.ChildWindows.Add(this, new System.Collections.Generic.HashSet<ViewModelBase> { });

            Tabs.CollectionChanged += Tabs_CollectionChanged;
        }

        private void Tabs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is ViewModelBase viewModel)
                    {
                        DialogHelper.ChildWindows[this].Remove(viewModel);
                    }
                }
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is ITabContent tabContent)
                    {
                        tabContent.MessageSender = this;
                    }

                    if (newItem is ViewModelBase viewModel)
                    {
                        DialogHelper.ChildWindows[this].Add(viewModel);
                    }
                }
            }
        }

        public void Dispose()
        {
            Tabs.CollectionChanged -= Tabs_CollectionChanged;
            foreach (var item in Tabs)
            {
                (item as IDisposable)?.Dispose();
            }
        }
    }
}
