namespace BadMovieClues.Services
{
    public interface IHapticsService
    {
        bool Enabled { get; set; }
        void VibrateClick();
        void VibrateWin();
        void VibrateFailure();
    }
}
