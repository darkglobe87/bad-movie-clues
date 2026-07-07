using System;
using Newtonsoft.Json;

namespace BadMovieClues.Data
{
    [Serializable]
    public class LevelData
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("movieTitle")] public string MovieTitle;
        [JsonProperty("badDescription")] public string BadDescription;
        [JsonProperty("characterClue")] public string CharacterClue;
        [JsonProperty("imageKey")] public string ImageKey = "";
        [JsonProperty("tags")] public string[] Tags = Array.Empty<string>();
        [JsonProperty("difficulty")] public int Difficulty = 1;
        [JsonProperty("plotHint")] public string PlotHint = "";
        [JsonProperty("sceneHint")] public string SceneHint = "";
    }
}
