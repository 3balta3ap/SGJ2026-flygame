using UnityEngine;

namespace FlyJam
{
    public class TitleScreen : MonoBehaviour
    {
        void Start() => Time.timeScale = 1f;

        void Update()
        {
            if (InputCompat.ConfirmPressed()) StartGame();
        }

        public void StartGame() => SceneFlow.GoToGame();
    }
}
