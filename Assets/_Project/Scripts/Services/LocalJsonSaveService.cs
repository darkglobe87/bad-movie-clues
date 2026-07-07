using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BadMovieClues.Services
{
    /// <summary>Local JSON persistence, one file per key. Defaults to
    /// Application.persistentDataPath; a directory can be injected for
    /// testing so tests never touch the real save location.</summary>
    public class LocalJsonSaveService : ISaveService
    {
        private readonly string _directory;

        public LocalJsonSaveService(string directory = null)
        {
            _directory = directory ?? Application.persistentDataPath;
        }

        public bool TryLoad<T>(string key, out T value)
        {
            var path = PathFor(key);
            if (!File.Exists(path))
            {
                value = default;
                return false;
            }

            value = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            return true;
        }

        public void Save<T>(string key, T value)
        {
            File.WriteAllText(PathFor(key), JsonConvert.SerializeObject(value));
        }

        private string PathFor(string key) => Path.Combine(_directory, $"{key}.json");
    }
}
