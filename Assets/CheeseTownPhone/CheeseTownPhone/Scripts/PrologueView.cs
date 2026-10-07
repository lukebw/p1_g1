using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // Editable presentation only; session ownership prevents replay when scenes reload.
    public sealed class PrologueView : MonoBehaviour
    {
        [Serializable] public sealed class Chapter
        {
            public string title;
            [TextArea(2, 6)] public string body;
            public Sprite background;
        }
        public Chapter[] chapters;
        public Image background;
        public Text title, body, counter, hint;
        public CanvasGroup page;
        public Button advance, skip;
        public string finalHint = "CLICK / SPACE TO SET OUT";
        [Min(.05f)] public float fadeSeconds = .45f;
        [Min(0)] public float minimumReadSeconds = .6f;
        public int ChapterIndex { get; private set; }
        public bool IsTransitioning => state != 0;
        Action completed;
        int state;
        float elapsed, readable;

        void LateUpdate() { RefreshCoverage(); }
        public void RefreshCoverage()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null || advance == null) return;
            // Full-screen art needs bleed at fractional canvas scales and odd viewport sizes.
            // Extend only the backdrop, leaving the authored text and button anchors intact.
            float bleed = 2 / Mathf.Max(.01f, canvas.scaleFactor);
            var backdrop = (RectTransform)advance.transform;
            backdrop.offsetMin = Vector2.one * -bleed;
            backdrop.offsetMax = Vector2.one * bleed;
        }

        public void Begin(Action onComplete)
        {
            RefreshCoverage();
            completed = onComplete;
            if (chapters == null || chapters.Length == 0) { Finish(); return; }
            advance.onClick.RemoveListener(Advance); advance.onClick.AddListener(Advance);
            skip.onClick.RemoveListener(Skip); skip.onClick.AddListener(Skip);
            ChapterIndex = 0; Present(); page.alpha = 0; state = 1; elapsed = 0;
        }
        void Present()
        {
            var chapter = chapters[ChapterIndex];
            background.sprite = chapter.background;
            title.text = chapter.title; body.text = chapter.body;
            // Opening and ending stories read continuously, without numbered slides.
            title.gameObject.SetActive(!string.IsNullOrWhiteSpace(chapter.title));
            counter.text = ""; counter.gameObject.SetActive(false);
            hint.text = ChapterIndex == chapters.Length - 1 ? finalHint : "CLICK / SPACE TO CONTINUE";
            readable = 0;
        }
        public void Advance()
        {
            if (state != 0 || readable < minimumReadSeconds) return;
            state = ChapterIndex == chapters.Length - 1 ? 3 : 2; elapsed = 0;
        }
        public void Skip() { if (state == 3) return; state = 3; elapsed = (1 - page.alpha) * fadeSeconds; }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) Skip();
                else if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) Advance();
            }
            Step(Time.unscaledDeltaTime);
        }
        // Exposed for deterministic transition checks; gameplay never adjusts Time.timeScale.
        public void Step(float seconds)
        {
            if (state == 0) { readable += seconds; return; }
            elapsed += seconds;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.05f, fadeSeconds));
            page.alpha = state == 1 ? t : 1 - t;
            if (t < 1) return;
            if (state == 3) { Finish(); return; }
            if (state == 2) { ChapterIndex++; Present(); state = 1; page.alpha = 0; }
            else state = 0;
            elapsed = 0;
        }
        void Finish()
        {
            var callback = completed; completed = null;
            gameObject.SetActive(false); callback?.Invoke();
        }
    }
}
