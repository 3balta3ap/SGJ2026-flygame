using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// 4박자 노래. 카운트인 2박 뒤 4박에 맞춰 Space. needed개 이상 맞히면 성공.
    /// 박자 소리는 코드로 만든 비프음 (사운드 에셋 불필요).
    public class CourtshipQTE : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text beatsText;
        [SerializeField] TMP_Text judgeText;
        [SerializeField] float bpm = 110f;
        [SerializeField] int notes = 4;
        [SerializeField] int countIn = 2;
        [SerializeField] float hitWindow = 0.15f;
        [SerializeField] int needed = 3;

        AudioSource audioSource;
        AudioClip tickClip, hitClip;

        public bool Running { get; private set; }

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            tickClip = MakeBeep(660f, 0.06f);
            hitClip = MakeBeep(990f, 0.09f);
        }

        public void Begin(Action<bool> onDone)
        {
            if (Running) return;
            StartCoroutine(Run(onDone));
        }

        IEnumerator Run(Action<bool> onDone)
        {
            Running = true;
            panel.SetActive(true);
            judgeText.text = "SING!";

            float beat = 60f / bpm;
            bool[] judged = new bool[notes];
            int hits = 0;
            int lastTick = -1;
            float start = Time.unscaledTime + 0.4f;
            float end = start + (countIn + notes) * beat + hitWindow;

            while (Time.unscaledTime < end)
            {
                float now = Time.unscaledTime - start;
                int beatIdx = Mathf.FloorToInt(now / beat);

                if (now >= 0f && beatIdx > lastTick && beatIdx < countIn + notes)
                {
                    lastTick = beatIdx;
                    audioSource.PlayOneShot(tickClip);
                }

                if (InputCompat.DodgePressed())
                {
                    int n = Mathf.RoundToInt(now / beat) - countIn;
                    bool ok = n >= 0 && n < notes && !judged[n]
                              && Mathf.Abs(now - (countIn + n) * beat) <= hitWindow;
                    if (ok)
                    {
                        judged[n] = true;
                        hits++;
                        audioSource.PlayOneShot(hitClip);
                        judgeText.text = "GOOD!";
                    }
                    else judgeText.text = "MISS";
                }

                int countdown = now < countIn * beat ? Mathf.Clamp(countIn - beatIdx, 1, countIn) : 0;
                beatsText.text = BuildBeats(beatIdx - countIn, judged, countdown);
                yield return null;
            }

            bool success = hits >= needed;
            judgeText.text = success ? "<3 LOVE <3" : "She ran away...";
            yield return new WaitForSecondsRealtime(0.9f);
            panel.SetActive(false);
            Running = false;
            onDone?.Invoke(success);
        }

        static string BuildBeats(int current, bool[] judged, int countdown)
        {
            var sb = new StringBuilder();
            sb.Append(countdown > 0 ? $"<size=60%>READY {countdown}</size>\n" : "\n");
            for (int i = 0; i < judged.Length; i++)
            {
                if (judged[i]) sb.Append("<color=#FF6FA5>X</color>");
                else if (i < current) sb.Append("<color=#666666>-</color>");
                else if (i == current) sb.Append("<color=#FFFFFF><b>O</b></color>");
                else sb.Append("<color=#AAAAAA>o</color>");
                sb.Append("   ");
            }
            return sb.ToString();
        }

        static AudioClip MakeBeep(float freq, float duration)
        {
            const int sampleRate = 44100;
            int n = Mathf.Max(1, (int)(sampleRate * duration));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float env = 1f - (float)i / n;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * 0.25f * env;
            }
            var clip = AudioClip.Create("beep", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
