using UnityEngine;

namespace BadMovieClues.Economy
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Bad Movie Clues/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Min(0)] public int StartingBalance = 100;
        [Min(0)] public int PictureHintCost = 50;
        [Min(0)] public int CharacterHintCost = 30;
        [Min(0)] public int LetterHintCost = 15;
        [Min(0)] public int LevelCompleteReward = 20;
        [Min(0)] public int SkipLevelCost = 100;
        [Min(0)] public int RewardedAdCoinReward = 50;

        [Header("Daily Challenge")]
        [Min(1)] public int DailyChallengeRewardMultiplier = 3;

        [Header("Login Streak")]
        public int[] StreakRewards = new int[] { 10, 15, 20, 30, 40, 50, 100 };
    }
}
