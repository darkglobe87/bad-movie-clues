using System;

namespace BadMovieClues.Services
{
    public interface IUserSettings
    {
        bool ReducedEffects { get; set; }
        bool AudioEnabled { get; set; }
        bool HapticsEnabled { get; set; }
        event Action Changed;
    }
}
