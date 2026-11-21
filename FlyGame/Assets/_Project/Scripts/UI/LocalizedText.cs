using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Puts Loc.Get(key) into this TMP text and updates it when the language changes.
    /// Use for fixed labels. Texts that code writes every frame call Loc.Get/Loc.Format directly.
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        TMP_Text label;

        public string Key
        {
            get => key;
            set { key = value; Refresh(); }
        }

        void Awake() => label = GetComponent<TMP_Text>();

        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Changed -= Refresh;

        void Refresh()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (!string.IsNullOrEmpty(key)) label.text = Loc.Get(key);
        }
    }
}
