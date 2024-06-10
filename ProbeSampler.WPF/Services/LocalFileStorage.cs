using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace ProbeSampler.WPF
{
    public class LocalFileStorage : IStorageService
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;

        public IEnumerable<T> Load<T>(string pathToFolder)
            where T : ISavableEntity
        {
            ThrowIfDirectoryNotExists(pathToFolder);

            var files = Directory.GetFiles(pathToFolder);
            var objectsFromFiles = new List<T>();

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                T? objectFromFile = JsonConvert.DeserializeObject<T>(json);
                if (objectFromFile != null)
                {
                    objectsFromFiles.Add(objectFromFile);
                }
            }

            return objectsFromFiles;
        }

        public T? Load<T>(string nameToLoad, string pathToFolder)
            where T : ISavableEntity
        {
            ThrowIfDirectoryNotExists(pathToFolder);

            var file = Array.Find(Directory.GetFiles(pathToFolder), s => Path.GetFileName(s) == nameToLoad);
            if (file == null)
            {
                throw new FileNotFoundException($"Unnable find file {nameToLoad}");
            }

            var json = File.ReadAllText(file);
            T? objectFromFile = JsonConvert.DeserializeObject<T>(json);

            return objectFromFile;
        }

        public async Task<IEnumerable<T>> LoadAsync<T>(string pathToFolder)
            where T : ISavableEntity
        {
            ThrowIfDirectoryNotExists(pathToFolder);

            var files = Directory.GetFiles(pathToFolder);
            var objectsFromFiles = new List<T>();

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                T? objectFromFile = JsonConvert.DeserializeObject<T>(json);
                if (objectFromFile != null)
                {
                    objectsFromFiles.Add(objectFromFile);
                }
            }

            return objectsFromFiles;
        }

        public async Task<T?> LoadAsync<T>(string nameToLoad, string pathToFolder)
            where T : ISavableEntity
        {
            ThrowIfDirectoryNotExists(pathToFolder);

            var file = Array.Find(Directory.GetFiles(pathToFolder), s => Path.GetFileName(s) == nameToLoad);
            if (file == null)
            {
                throw new FileNotFoundException($"Unnable find file {nameToLoad}");
            }

            var json = await File.ReadAllTextAsync(file);
            T? objectFromFile = JsonConvert.DeserializeObject<T>(json);

            return objectFromFile;
        }

        public void Remove(string nameToRemove, string pathToFolder)
        {
            ThrowIfDirectoryNotExists(pathToFolder);

            string? file = Array.Find(Directory.GetFiles(pathToFolder), s => Path.GetFileName(s) == nameToRemove);

            if (file == null)
            {
                throw new FileNotFoundException($"Unnable find file {nameToRemove}");
            }

            File.Delete(file);
        }

        public async Task RemoveAsync(string nameToRemove, string pathToFolder)
        {
            ThrowIfDirectoryNotExists(pathToFolder);

            string? file = Array.Find(Directory.GetFiles(pathToFolder), s => Path.GetFileName(s) == nameToRemove);

            if (file == null)
            {
                throw new FileNotFoundException($"Unnable find file {nameToRemove}");
            }

            await Task.Run(() => File.Delete(file));
        }

        public void Save(ISavableEntity objectToSave, string pathToFolder)
        {
            Path.GetDirectoryName(pathToFolder);
            if (!Directory.Exists(pathToFolder))
            {
                Directory.CreateDirectory(pathToFolder);
            }

            var json = JsonConvert.SerializeObject(objectToSave);
            File.WriteAllText(Path.Combine(pathToFolder, objectToSave.NameOnSaving), json);
        }

        public async Task SaveAsync(ISavableEntity objectToSave, string pathToFolder)
        {
            Path.GetDirectoryName(pathToFolder);
            if (!Directory.Exists(pathToFolder))
            {
                Directory.CreateDirectory(pathToFolder);
            }

            var json = JsonConvert.SerializeObject(objectToSave);
            await File.WriteAllTextAsync(Path.Combine(pathToFolder, objectToSave.NameOnSaving), json);
        }

        private void ThrowIfDirectoryNotExists(string pathToFolder)
        {
            if (!Directory.Exists(pathToFolder))
            {
                throw new DirectoryNotFoundException();
            }
        }
    }
}
