using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Courtship panel (S06, P03, E03, A05): press the shown buttons in order.
    /// All correct -> success. One wrong button (or the time limit, if set) -> failure.
    /// Each correct press plays a rising note, so it still feels like the courtship song.
    /// (Class name kept from the prototype's rhythm QTE so CourtshipFlow and the prefab links stay valid.)
    public class CourtshipQTE : MonoBehaviour
    {
        [SerializeField] CourtshipBalance balance;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;          // "順番にボタンを押そう！" -> result title
        [SerializeField] TMP_Text[] slots;            // button boxes left to right (8 boxes; extra ones are hidden)
        [SerializeField] GameObject[] slotFrames;     // highlight frame for the next button (same index as slots)
        [SerializeField] TMP_Text infoText;           // "1／4　次はX" -> failure reason
        [SerializeField] Image timerFill;             // optional, Image Type = Filled. Hidden when there is no limit
        [SerializeField] Color doneColor = new Color(0.55f, 0.35f, 0.55f, 1f);
        [SerializeField] Color nextColor = new Color(0.35f, 0.1f, 0.2f, 1f);
        [SerializeField] Color todoColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        const string DoneMark = "♪";

        AudioSource audioSource;
        AudioClip wrongClip;

        public bool Running { get; private set; }

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            wrongClip = MakeBeep(220f, 0.25f);
        }

        public void Begin(Action<bool> onDone)
        {
            if (Running) return;
            StartCoroutine(Run(onDone));
        }

        IEnumerator Run(Action<bool> onDone)
        {
            Running = true;
            int length = balance != null ? balance.Length : 4;
            int kinds = balance != null ? balance.ButtonKinds : 4;
            float limit = balance != null ? balance.TimeLimit : 0f;
            float wait = balance != null ? balance.InputDelay : 0.3f;
            float left = limit;

            var seq = ButtonSequence.Random(length, kinds);
            panel.SetActive(true);
            titleText.text = Loc.Get("court.title");
            if (timerFill != null) timerFill.gameObject.SetActive(limit > 0f);
            Refresh(seq);
            GameEvents.RaiseCourtshipStarted();

            bool success = false;
            string failKey = null;
            while (true)
            {
                yield return null;
                Refresh(seq);                                // button names follow the device in use
                if (TimeControl.InputBlocked) continue;      // pause menu open
                float dt = Time.deltaTime;
                if (wait > 0f) { wait -= dt; continue; }

                if (limit > 0f)
                {
                    left -= dt;
                    if (timerFill != null) timerFill.fillAmount = Mathf.Clamp01(left / limit);
                    if (left <= 0f) { failKey = "court.fail.time"; break; }
                }

                if (!InputCompat.FaceButtonPressed(out var pressed)) continue;
                var step = seq.Submit(pressed);
                if (step == ButtonSequence.Step.Wrong)
                {
                    GameEvents.RaiseCourtshipInput(false);
                    audioSource.PlayOneShot(wrongClip);
                    failKey = "court.fail.wrong";
                    break;
                }
                GameEvents.RaiseCourtshipInput(true);
                audioSource.PlayOneShot(MakeBeep(660f * Mathf.Pow(2f, seq.Index / 6f), 0.12f));
                if (step == ButtonSequence.Step.Completed)
                {
                    success = true;
                    break;
                }
            }

            Refresh(seq);
            if (timerFill != null) timerFill.gameObject.SetActive(false);
            titleText.text = Loc.Get(success ? "court.success" : "court.fail.title");
            infoText.text = success ? "" : Loc.Get(failKey);

            float show = balance != null ? balance.ResultShowSeconds : 1.2f;
            while (show > 0f)
            {
                yield return null;
                if (!TimeControl.Paused) show -= Time.deltaTime;
            }

            panel.SetActive(false);
            Running = false;
            onDone?.Invoke(success);
        }

        void Refresh(ButtonSequence seq)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < seq.Length;
                if (slots[i] != null)
                {
                    slots[i].transform.parent.gameObject.SetActive(used);
                    if (used)
                    {
                        bool done = i < seq.Index;
                        slots[i].text = done ? DoneMark : InputCompat.Label(seq.Buttons[i]);
                        slots[i].color = done ? doneColor : i == seq.Index ? nextColor : todoColor;
                    }
                }
                if (slotFrames != null && i < slotFrames.Length && slotFrames[i] != null)
                    slotFrames[i].SetActive(used && i == seq.Index && !seq.IsComplete);
            }
            if (!seq.IsComplete)
                infoText.text = Loc.Format("court.progress", seq.Index, seq.Length, InputCompat.Label(seq.Next));
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
