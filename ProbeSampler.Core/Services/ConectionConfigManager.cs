using DynamicData;
using System.Xml.Linq;

namespace ProbeSampler.Core.Services
{
    public class ConectionConfigManager : IConnectionConfigurationManager, IEnableLogger
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly IPathService pathService;
        private readonly IStorageService storageService;

        public SourceCache<ConnectionConfiguration, Guid> SourceCacheConnectionConfigurations { set; get; }

        public ConectionConfigManager(IStorageService? storageService = null)
        {
            this.storageService = storageService ?? resolver.GetRequiredService<IStorageService>();
            pathService = resolver.GetRequiredService<IPathService>();

            SourceCacheConnectionConfigurations = new SourceCache<ConnectionConfiguration, Guid>(t => t.Id);
            List<ConnectionConfiguration> connectionConfigurations = new List<ConnectionConfiguration>();

            try
            {
                var configs = storageService.Load<ConnectionConfiguration>(pathService.ConfigsPath);
                connectionConfigurations = configs.ToList();
                this.Log().Info($"{this.GetType().Name} successfully loaded {connectionConfigurations.Count}");
            }
            catch (Exception ex)
            {
                this.Log().Error($"{this.GetType().Name} error loading configurations: {ex}");
            }

            foreach (var con in connectionConfigurations)
            {
                SourceCacheConnectionConfigurations.AddOrUpdate(con);
            }
        }

        public bool Add(ConnectionConfiguration connectionConfiguration)
        {
            try
            {
                storageService.Save(connectionConfiguration, pathService.ConfigsPath);
                SourceCacheConnectionConfigurations.AddOrUpdate(connectionConfiguration);
                return true;
            }
            catch (Exception ex)
            {
                this.Log().Error(ex);
                return false;
            }
        }

        public async Task<bool> AddAsync(ConnectionConfiguration connectionConfiguration)
        {
            try
            {
                await storageService.SaveAsync(connectionConfiguration, pathService.ConfigsPath);
                SourceCacheConnectionConfigurations.AddOrUpdate(connectionConfiguration);
                return true;
            }
            catch (Exception ex)
            {
                this.Log().Error(ex);
                return false;
            }
        }

        public bool Remove(Guid id)
        {
            if (SourceCacheConnectionConfigurations.Lookup(id).HasValue)
            {
                try
                {
                    storageService.Remove(id.ToString(), pathService.ConfigsPath);
                    SourceCacheConnectionConfigurations.Remove(id);
                    return true;
                }
                catch (Exception ex)
                {
                    this.Log().Error(ex);
                    return false;
                }
            }

            return false;
        }

        public bool Remove(string name)
        {
            var connectionConfig = Get(name);
            if (connectionConfig != null)
            {
                try
                {
                    storageService.Remove(connectionConfig.Id.ToString(), pathService.ConfigsPath);
                    SourceCacheConnectionConfigurations.Remove(connectionConfig.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    this.Log().Error(ex);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> RemoveAsync(Guid id)
        {
            if (SourceCacheConnectionConfigurations.Lookup(id).HasValue)
            {
                try
                {
                    await storageService.RemoveAsync(id.ToString(), pathService.ConfigsPath);
                    SourceCacheConnectionConfigurations.Remove(id);
                    return true;
                }
                catch (Exception ex)
                {
                    this.Log().Error(ex);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> RemoveAsync(string name)
        {
            var connectionConfig = Get(name);
            if (connectionConfig != null)
            {
                try
                {
                    await storageService.RemoveAsync(connectionConfig.Id.ToString(), pathService.ConfigsPath);
                    SourceCacheConnectionConfigurations.Remove(connectionConfig.Id);
                    return true;
                }
                catch (Exception ex)
                {
                    this.Log().Error(ex);
                    return false;
                }
            }

            return false;
        }

        public bool IsExistById(Guid id)
        {
            return Get(id) != null;
        }

        public bool IsExistByName(string name)
        {
            return Get(name) != null;
        }

        public ConnectionConfiguration? Get(Guid id)
        {
            return SourceCacheConnectionConfigurations.Items.FirstOrDefault(x => x.Id.Equals(id));
        }

        public ConnectionConfiguration? Get(string name)
        {
            return SourceCacheConnectionConfigurations.Items.FirstOrDefault(x => x.Name != null && x.Name.Equals(name));
        }
    }
}
