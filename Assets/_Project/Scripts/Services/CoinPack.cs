namespace BadMovieClues.Services
{
    public readonly struct CoinPack
    {
        public readonly string Id;
        public readonly int Coins;
        public readonly string Label;

        public CoinPack(string id, int coins, string label)
        {
            Id = id;
            Coins = coins;
            Label = label;
        }
    }
}
