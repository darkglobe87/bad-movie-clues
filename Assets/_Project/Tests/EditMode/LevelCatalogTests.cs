using System.Collections.Generic;
using System.IO;
using System.Linq;
using BadMovieClues.Data;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace BadMovieClues.Tests
{
    public class LevelCatalogTests
    {
        private const string TopLevelArrayJson = @"
        [
          { ""id"": ""a"", ""movieTitle"": ""Movie A"", ""badDescription"": ""desc a"", ""characterClue"": ""Someone"" },
          { ""id"": ""b"", ""movieTitle"": ""Movie B"", ""badDescription"": ""desc b"", ""characterClue"": ""Someone Else"",
            ""imageKey"": ""img_b"", ""tags"": [""comedy"", ""90s""], ""difficulty"": 3,
            ""plotHint"": ""a hint"", ""sceneHint"": ""a scene"" }
        ]";

        [Test]
        public void ParsesTopLevelJsonArray()
        {
            // JsonUtility cannot parse a top-level array; this is exactly why
            // Newtonsoft.Json is used for the catalog. Guard the behavior.
            var levels = JsonConvert.DeserializeObject<List<LevelData>>(TopLevelArrayJson);

            Assert.IsNotNull(levels);
            Assert.AreEqual(2, levels.Count);
        }

        [Test]
        public void MissingOptionalFieldsFallBackToDefaults()
        {
            var levels = JsonConvert.DeserializeObject<List<LevelData>>(TopLevelArrayJson);
            var a = levels.First(l => l.Id == "a");

            Assert.AreEqual("", a.ImageKey);
            Assert.AreEqual(0, a.Tags.Length);
            Assert.AreEqual(1, a.Difficulty);
            Assert.AreEqual("", a.PlotHint);
            Assert.AreEqual("", a.SceneHint);
        }

        [Test]
        public void PresentOptionalFieldsAreParsed()
        {
            var levels = JsonConvert.DeserializeObject<List<LevelData>>(TopLevelArrayJson);
            var b = levels.First(l => l.Id == "b");

            Assert.AreEqual("img_b", b.ImageKey);
            CollectionAssert.AreEqual(new[] { "comedy", "90s" }, b.Tags);
            Assert.AreEqual(3, b.Difficulty);
            Assert.AreEqual("a hint", b.PlotHint);
            Assert.AreEqual("a scene", b.SceneHint);
        }

        [Test]
        public void CatalogFindByIdReturnsMatchOrNull()
        {
            var levels = JsonConvert.DeserializeObject<List<LevelData>>(TopLevelArrayJson);
            var catalog = new LevelCatalog(levels);

            Assert.IsNotNull(catalog.FindById("a"));
            Assert.AreEqual("Movie A", catalog.FindById("a").MovieTitle);
            Assert.IsNull(catalog.FindById("does-not-exist"));
        }

        [Test]
        public void BundledStarterCatalogHasThirtySixUniqueSeededMovies()
        {
            var path = Path.Combine(Application.dataPath, "_Project/Content/Resources/StarterCatalog.json");
            Assert.IsTrue(File.Exists(path), $"Expected starter catalog at {path}");

            var levels = JsonConvert.DeserializeObject<List<LevelData>>(File.ReadAllText(path));

            Assert.AreEqual(36, levels.Count);
            Assert.AreEqual(levels.Count, levels.Select(l => l.Id).Distinct().Count(),
                "All movie ids should be unique");

            var expectedImageKeys = new[]
            {
                "img_jaws", "img_et", "img_shrek", "img_toy_story",
                "img_lord_of_the_rings", "img_matrix", "img_pulp_fiction"
            };
            foreach (var key in expectedImageKeys)
            {
                Assert.IsTrue(levels.Any(l => l.ImageKey == key), $"Expected a level with imageKey '{key}'");
            }
        }
    }
}
