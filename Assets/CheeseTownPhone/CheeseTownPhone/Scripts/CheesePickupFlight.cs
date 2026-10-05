// BEGIN ADDED: Reusable UI-only flight; currency is never awarded by an animation callback.
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed class CheesePickupFlight : MonoBehaviour
    {
        [Header("Timing and arc - logical UI pixels")]
        [Min(.1f)] public float duration = .62f;
        [Min(0)] public float popDuration = .12f;
        [Min(0)] public float popHeight = 14;
        [Min(0)] public float arcHeight = 55;
        [Min(.05f)] public float arrivalDuration = .18f;
        [Header("Editable graphics - all ignore raycasts")]
        public Image icon;
        public Image[] trail;
        public Image[] particles;
        [Min(0)] public float trailSpacing = .032f;
        [Min(0)] public float particleSpread = 24;
        public Color particleColor = new Color(1, .82f, .25f, 1);
        public Color trailColor = new Color(1, .85f, .35f, .4f);
        public int Credit { get; private set; }
        public Vector2 ScreenStart { get; private set; }
        public bool Flying { get; private set; }
        public bool Arrived => elapsed >= Mathf.Max(.1f, duration);
        float elapsed, side;
        Vector2 iconSize;

        void Awake() { iconSize = icon.rectTransform.sizeDelta; }
        public void Launch(Vector2 screenStart, Sprite sprite, int credit, int sequence)
        {
            ScreenStart = screenStart; Credit = credit; elapsed = 0; side = sequence % 2 == 0 ? 1 : -1;
            Flying = true; gameObject.SetActive(true);
            if (sprite != null) icon.sprite = sprite;
            foreach (var image in trail) { image.sprite = icon.sprite; image.color = Color.clear; }
            foreach (var image in particles) image.color = Color.clear;
        }
        public void Merge(int credit) { Credit += credit; }
        public int TakeCredit() { int value = Credit; Credit = 0; return value; }
        public void Release()
        {
            Flying = false; Credit = 0; gameObject.SetActive(false);
        }
        public bool Advance(float delta, Vector2 start, Vector2 end)
        {
            elapsed += Mathf.Max(0, delta);
            float length = Mathf.Max(.1f, duration), time = Mathf.Clamp01(elapsed / length);
            Pose(icon.rectTransform, Position(time, start, end), Mathf.Lerp(1.16f, .5f, Mathf.Pow(time, 3)));
            icon.rectTransform.localRotation = Quaternion.Euler(0, 0, side * Mathf.Sin(time * Mathf.PI) * 18);
            icon.color = Arrived ? Color.clear : Color.white;
            for (int i = 0; i < trail.Length; i++)
            {
                float behind = time - (i + 1) * trailSpacing;
                float fade = 1 - (float)(i + 1) / (trail.Length + 1);
                Pose(trail[i].rectTransform, Position(Mathf.Max(0, behind), start, end), .85f * fade);
                trail[i].rectTransform.sizeDelta = iconSize;
                trail[i].color = behind > 0 && !Arrived ? new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * fade) : Color.clear;
            }
            // Two bursts share the same pool: takeoff sparks followed by a small arrival sparkle.
            for (int i = 0; i < particles.Length; i++)
            {
                bool arrival = i >= particles.Length / 2;
                float t = arrival ? (elapsed - length) / Mathf.Max(.05f, arrivalDuration) : time / .46f;
                float angle = i * 2.399963f + side;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float radius = particleSpread * (arrival ? .6f : 1) * Mathf.Clamp01(t);
                Vector2 origin = arrival ? end : start;
                Pose(particles[i].rectTransform, origin + direction * radius, Mathf.Lerp(1, .25f, Mathf.Clamp01(t)));
                particles[i].color = t >= 0 && t < 1 ? new Color(particleColor.r, particleColor.g, particleColor.b, 1 - t) : Color.clear;
            }
            return elapsed >= length + Mathf.Max(.05f, arrivalDuration);
        }
        Vector2 Position(float time, Vector2 start, Vector2 end)
        {
            float pop = Mathf.Clamp(popDuration / Mathf.Max(.1f, duration), 0, .45f);
            Vector2 lifted = start + new Vector2(side * 6, popHeight);
            if (pop > 0 && time < pop) return Vector2.Lerp(start, lifted, Mathf.Sin(time / pop * Mathf.PI * .5f));
            float t = Mathf.InverseLerp(pop, 1, time); t *= t;
            Vector2 control = Vector2.Lerp(lifted, end, .4f) + Vector2.up * arcHeight + Vector2.right * side * 15;
            return (1 - t) * (1 - t) * lifted + 2 * (1 - t) * t * control + t * t * end;
        }
        static void Pose(RectTransform rect, Vector2 position, float scale)
        {
            rect.anchoredPosition = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
            rect.localScale = Vector3.one * scale;
        }
    }
}
// END ADDED
