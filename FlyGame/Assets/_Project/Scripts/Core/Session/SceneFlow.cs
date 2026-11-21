using UnityEngine.SceneManagement;

namespace FlyJam
{
    /// Two scenes: Title and Game. Results are a panel inside Game (no Result scene).
    public static class SceneFlow
    {
        public const string Title = "Title";
        public const string Game = "Game";

        public static void GoToTitle()
        {
            TimeControl.ResetAll();
            SceneManager.LoadScene(Title);
        }

        /// Also used for "retry": reloading the scene resets player, HP, enemies, apples and events (S05).
        public static void GoToGame()
        {
            TimeControl.ResetAll();
            SceneManager.LoadScene(Game);
        }
    }
}
