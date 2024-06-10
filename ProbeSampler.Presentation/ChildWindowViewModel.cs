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

            Tabs.CollectionChanged += Tabs_CollectionChanged;
        }

        private void Tabs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is ITabContent tabContent)
                    {
                        tabContent.MessageSender = this;
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
