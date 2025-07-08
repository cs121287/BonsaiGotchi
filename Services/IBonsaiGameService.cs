using System;
using BonsaiGotchiGame.Models;

namespace BonsaiGotchiGame.Services
{
    public interface IBonsaiGameService
    {
        Bonsai Bonsai { get; }
        event EventHandler<BonsaiState>? BonsaiStateChanged;
        event EventHandler<int>? ExperienceGained;
        event EventHandler<int>? LevelChanged;
        
        void Water();
        void Prune();
        void Rest();
        void Fertilize();
        void CleanArea();
        void LightExercise();
        void IntenseTraining();
        void Play();
        void Meditate();
        
        void InitializeBackgroundAnimator(object backgroundElement);
        void UpdateSettings();
        void SaveBonsai();
        void Dispose();
    }
}