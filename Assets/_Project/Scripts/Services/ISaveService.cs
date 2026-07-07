namespace BadMovieClues.Services
{
    public interface ISaveService
    {
        bool TryLoad<T>(string key, out T value);
        void Save<T>(string key, T value);
    }
}
