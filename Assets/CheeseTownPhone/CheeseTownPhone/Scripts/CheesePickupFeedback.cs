// BEGIN ADDED: Bounded UI pool delays only the displayed balance, never the real economy.
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed class CheesePickupFeedback : MonoBehaviour
    {
        public CheesePickupFlight flightPrefab;
        public TabletView view;
        public RectTransform targetIcon;
        [Range(1, 24)] public int poolSize = 12;
        [Min(.01f)] public float pulseDuration = .18f;
        [Range(0, .5f)] public float pulseScale = .22f;
        CheesePickupFlight[] pool;
        TownProgress progress;
        Canvas canvas;
        RectTransform area;
        Vector3 iconRestScale, countRestScale;
        int pending, sequence;
        float pulse;
        public int PendingCredit => pending;
        public bool IsAnimating => pending > 0 || ActiveFlights > 0 || pulse > 0;
        public int ActiveFlights { get { int n = 0; if (pool != null) foreach (var f in pool) if (f.Flying) n++; return n; } }

        void Awake()
        {
            progress = TownSession.Instance.Progress;
            area = (RectTransform)transform; canvas = GetComponentInParent<Canvas>();
            iconRestScale = targetIcon.localScale; countRestScale = view.hudWallet.transform.localScale;
            pool = new CheesePickupFlight[Mathf.Clamp(poolSize, 1, 24)];
            for (int i = 0; i < pool.Length; i++) { pool[i] = Instantiate(flightPrefab, transform); pool[i].Release(); }
        }

        public bool BeginPickup(Vector3 worldPosition, Sprite sprite, int credit)
        {
            if (credit <= 0 || !isActiveAndEnabled || pool == null || !HudVisible || Camera.main == null) return false;
            var camera = Camera.main;
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            if (viewport.z <= 0) return false;
            // Viewport coordinates also work when the camera renders to a resized target texture.
            Vector2 normalized = camera.rect.min + Vector2.Scale((Vector2)viewport, camera.rect.size);
            normalized = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
            CheesePickupFlight free = null;
            foreach (var flight in pool) if (!flight.Flying) { free = flight; break; }
            pending += credit;
            if (free == null)
            {
                foreach (var flight in pool) if (!flight.Arrived) { flight.Merge(credit); return true; }
                free = pool[0]; // All slots are finishing their arrival sparks; recycle one safely.
            }
            free.Launch(normalized, sprite, credit, sequence++);
            free.Advance(0, StartPoint(free), EndPoint());
            return true;
        }
        bool HudVisible => view != null && view.worldHud.gameObject.activeInHierarchy && !view.IsVisible && !progress.GameEnded;
        Camera UICamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 StartPoint(CheesePickupFlight flight)
        {
            Rect viewport = UICamera != null ? UICamera.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
            var screen = viewport.min + Vector2.Scale(flight.ScreenStart, viewport.size);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, UICamera, out Vector2 local);
            return local;
        }
        Vector2 EndPoint() => area.InverseTransformPoint(targetIcon.TransformPoint(targetIcon.rect.center));

        public void RefreshWallet()
        {
            if (progress == null || view == null) return;
            if (!HudVisible && pending > 0) { CancelAndSync(); return; }
            view.hudWallet.text = Mathf.Max(0, progress.Cheeses - pending).ToString();
        }
        void LateUpdate()
        {
            Advance(Time.unscaledDeltaTime);
        }
        // Public stepping also lets editor previews exercise exactly the same animation path.
        public void Advance(float delta)
        {
            if (!HudVisible) { if (pending > 0 || pulse > 0 || ActiveFlights > 0) CancelAndSync(); return; }
            Vector2 end = EndPoint();
            foreach (var flight in pool)
            {
                if (!flight.Flying) continue;
                bool done = flight.Advance(delta, StartPoint(flight), end);
                if (flight.Arrived && flight.Credit > 0)
                {
                    pending -= flight.TakeCredit(); pulse = pulseDuration; RefreshWallet();
                }
                if (done) flight.Release();
            }
            pulse = Mathf.Max(0, pulse - delta);
            float scale = 1 + pulseScale * Mathf.Sin(Mathf.PI * pulse / Mathf.Max(.01f, pulseDuration));
            targetIcon.localScale = iconRestScale * scale;
            view.hudWallet.transform.localScale = countRestScale * scale;
        }
        public void CancelAndSync()
        {
            pending = 0; pulse = 0;
            if (pool != null) foreach (var flight in pool) if (flight != null) flight.Release();
            if (targetIcon != null) targetIcon.localScale = iconRestScale;
            if (view != null && view.hudWallet != null)
            {
                view.hudWallet.transform.localScale = countRestScale;
                if (progress != null) view.hudWallet.text = progress.Cheeses.ToString();
            }
        }
        void OnDisable() { CancelAndSync(); }
    }
}
// END ADDED
