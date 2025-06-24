using System;
using System.Windows.Forms;

namespace BonsaiGotchi.MiniGames
{
    /// <summary>
    /// Manager class for handling all mini-games
    /// </summary>
    public static class MiniGameManager
    {
        /// <summary>
        /// Available mini-game types
        /// </summary>
        public enum GameType
        {
            LeafCounting,
            PestRemoval,
            PruningPuzzle,
            SeasonalCare
        }
        
        /// <summary>
        /// Launches a mini-game of the specified type and returns the score
        /// </summary>
        public static double LaunchGame(GameType gameType, IWin32Window owner = null)
        {
            using MiniGameBase game = CreateGame(gameType);
            
            if (game == null)
                return 0;
            
            // Show the game dialog
            game.ShowDialog(owner);
            
            // Return the score (0-100)
            return game.Score;
        }
        
        /// <summary>
        /// Creates a new instance of the specified mini-game
        /// </summary>
        private static MiniGameBase CreateGame(GameType gameType)
        {
            return gameType switch
            {
                GameType.LeafCounting => new LeafCountingGame(),
                GameType.PestRemoval => new PestRemovalGame(),
                GameType.PruningPuzzle => new PruningPuzzleGame(),
                GameType.SeasonalCare => new SeasonalCareGame(),
                _ => null
            };
        }
    }
}