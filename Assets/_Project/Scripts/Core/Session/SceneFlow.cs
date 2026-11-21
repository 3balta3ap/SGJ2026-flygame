using UnityEngine.SceneManagement;

namespace FlyJam
{
    public static class SceneFlow
    {
        public const string Title = "Title";
        public const string Game = "Game";
        public const string Result = "Result";

        public static void GoToTitle() => SceneManager.LoadScene(Title);
        public static void GoToGame() => SceneManager.LoadScene(Game);
        public static void GoToResult() => SceneManager.LoadScene(Result);
    }
}
