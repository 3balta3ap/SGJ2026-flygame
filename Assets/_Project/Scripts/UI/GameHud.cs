using TMPro;
using UnityEngine;

namespace FlyJam
{
    public class GameHud : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] TMP_Text timerText;

        void Start()
        {
            if (session == null && GameContext.Current != null) session = GameContext.Current.Session;
        }

        void Update()
        {
            if (session == null || timerText == null) return;
            timerText.text = Mathf.CeilToInt(session.Remaining).ToString();
        }
    }
}
