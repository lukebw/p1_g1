using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // Visual proxies for stored cheese, not collectible objects or a second inventory.
    public sealed class TownTreeHarvestFeedback : MonoBehaviour
    {
        [Header("Editable pooled artwork")]
        public Image dropTemplate;
        public CheesePickupFlight flightPrefab;
        [Range(1, 48)] public int dropLimit = 32;
        [Range(1, 24)] public int flightLimit = 18;
        [Range(1, 20)] public int burstLimit = 12;
        [Header("Fall and harvest timing")]
        [Tooltip("Gravity in canvas units per second squared.")]
        [Min(1)] public float gravity = 700;
        [Range(0, .6f)] public float restitution = .24f;
        [Min(0)] public float dropStagger = .035f;
        [Min(0)] public float flightStagger = .035f;
        [Min(.05f)] public float pulseDuration = .22f;
        [Range(0, .5f)] public float pulseScale = .18f;
        [Header("Regions relative to the tree: (0,0) is bottom left")]
        public Vector2 canopyMin = new Vector2(.16f, .60f);
        public Vector2 canopyMax = new Vector2(.84f, .86f);
        [Tooltip("Resting height range; horizontal landing positions come from the canopy.")]
        public Vector2 groundHeights = new Vector2(.035f, .095f);

        sealed class Drop
        {
            public Image image;
            public Vector2 start, end;
            public float elapsed, delay;
            public bool falling;
        }
        sealed class Flight
        {
            public CheesePickupFlight view;
            public Vector2 start;
            public int queuedCredit;
            public float delay;
            public bool busy, queued;
        }
        Drop[] drops;
        Flight[] flights;
        TownProgress progress;
        TabletView owner;
        RectTransform area, destination;
        Vector3 iconScale, countScale;
        bool presented;
        int sequence, pending;
        float pulse;
        public int PendingCredit => pending;
        public bool IsAnimating => pending > 0 || ActiveFlights > 0 || pulse > 0;
        public int VisibleDrops { get { int n = 0; if (drops != null) foreach (var d in drops) if (d.image.gameObject.activeSelf) n++; return n; } }
        public int ActiveFlights { get { int n = 0; if (flights != null) foreach (var f in flights) if (f.busy) n++; return n; } }
        bool Visible => isActiveAndEnabled && owner != null && owner.IsOpen && owner.Page == 0
            && !owner.IsTransitioning && progress != null && !progress.GameEnded;

        public void Bind(TownProgress source, TabletView view, RectTransform target)
        {
            if (progress != null) progress.BatchProduced -= Produced;
            CancelAndSync();
            progress = source; owner = view; destination = target; area = (RectTransform)transform;
            iconScale = destination.localScale; countScale = owner.wallet.transform.localScale;
            if (drops == null)
            {
                drops = new Drop[Mathf.Clamp(dropLimit, 1, 48)];
                for (int i = 0; i < drops.Length; i++)
                {
                    var image = Instantiate(dropTemplate, transform); image.name = "Stored cheese " + (i + 1);
                    image.raycastTarget = false; image.gameObject.SetActive(false); drops[i] = new Drop { image = image };
                }
                dropTemplate.gameObject.SetActive(false);
                flights = new Flight[Mathf.Clamp(flightLimit, 1, 24)];
                for (int i = 0; i < flights.Length; i++)
                {
                    var flight = Instantiate(flightPrefab, transform); flight.Release();
                    flights[i] = new Flight { view = flight };
                }
            }
            progress.BatchProduced += Produced;
        }
        // A local hash scatters proxies without disturbing the world's random generator.
        static float Scatter(int index, uint salt)
        {
            uint value = unchecked((uint)index + salt);
            value = unchecked((value ^ (value >> 16)) * 0x7feb352du);
            value = unchecked((value ^ (value >> 15)) * 0x846ca68bu);
            value ^= value >> 16;
            return (value & 0x00ffffffu) / 16777216f;
        }
        Vector2 TreePoint(Vector2 normalized)
        {
            var rect = owner.tree.rect;
            return area.InverseTransformPoint(owner.tree.TransformPoint(rect.min + Vector2.Scale(rect.size, normalized)));
        }
        float CanopyX(int index) => Mathf.Lerp(canopyMin.x, canopyMax.x, Scatter(index, 713));
        Vector2 Ground(int index) => TreePoint(new Vector2(CanopyX(index),
            Mathf.Lerp(groundHeights.x, groundHeights.y, Scatter(index, 997))));
        Vector2 Canopy(int index) => TreePoint(new Vector2(
            CanopyX(index), Mathf.Lerp(canopyMin.y, canopyMax.y, Scatter(index, 1597))));
        static void Position(Image image, Vector2 point)
        {
            image.rectTransform.anchoredPosition = new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
        }
        int ProxyCount(int amount) => Mathf.Min(drops.Length, amount,
            Mathf.CeilToInt(3 + 3 * Mathf.Log(Mathf.Max(1, amount), 2)));
        void RestorePile()
        {
            int count = ProxyCount(Mathf.FloorToInt(progress.Stock));
            for (int i = 0; i < drops.Length; i++)
            {
                var d = drops[i]; d.falling = false; d.image.gameObject.SetActive(i < count);
                d.end = Ground(i); Position(d.image, d.end);
                d.image.rectTransform.localRotation = Quaternion.Euler(0, 0, (Scatter(i, 2371) - .5f) * 40);
            }
            presented = true;
        }
        void Produced(int amount)
        {
            if (!Visible || amount <= 0 || drops == null) return;
            if (!presented) RestorePile();
            int count = ProxyCount(Mathf.FloorToInt(progress.Stock));
            int burst = Mathf.Min(count, Mathf.Clamp(burstLimit, 1, 20), Mathf.CeilToInt(2 * Mathf.Log(amount + 1f, 2)));
            int emitted = 0;
            // First reveal new proxies; then recycle a few resting ones when the visual pile is full.
            for (int i = 0; i < count && emitted < burst; i++)
                if (!drops[i].image.gameObject.activeSelf) Fall(i, emitted++);
            for (; emitted < burst; emitted++) Fall(sequence % count, emitted);
        }
        void Fall(int index, int order)
        {
            int seed = sequence++;
            var d = drops[index]; d.start = Canopy(seed);
            // The landing point is directly below the spawn, never a separate horizontal slot.
            d.end = new Vector2(d.start.x, Mathf.Min(d.start.y, Ground(seed).y));
            d.elapsed = 0; d.delay = order * Mathf.Max(0, dropStagger); d.falling = true;
            d.image.rectTransform.localRotation = Quaternion.Euler(0, 0, (Scatter(seed, 2371) - .5f) * 40);
            d.image.gameObject.SetActive(true); Position(d.image, d.start);
        }
        float FallingHeight(Drop d, out bool settled)
        {
            settled = false;
            float g = Mathf.Max(1, gravity), t = Mathf.Max(0, d.elapsed - d.delay);
            float fallTime = Mathf.Sqrt(2 * Mathf.Max(0, d.start.y - d.end.y) / g);
            if (t < fallTime) return d.start.y - .5f * g * t * t;
            t -= fallTime;
            float rebound = g * fallTime * Mathf.Clamp(restitution, 0, .6f);
            // Solve each ballistic arc analytically, so low frame rates cannot tunnel through the floor.
            for (int bounce = 0; bounce < 8 && rebound * rebound / (2 * g) >= .5f; bounce++)
            {
                float duration = 2 * rebound / g;
                if (t < duration) return d.end.y + rebound * t - .5f * g * t * t;
                t -= duration; rebound *= Mathf.Clamp(restitution, 0, .6f);
            }
            settled = true;
            return d.end.y;
        }
        public void PlayHarvest(int credit)
        {
            if (credit <= 0 || !Visible || flights == null) { RefreshWallet(); return; }
            int desired = Mathf.Min(flights.Length, Mathf.Max(1, ProxyCount(credit)));
            int free = 0;
            foreach (var f in flights) if (!f.busy || !f.queued && f.view.Arrived && f.view.Credit == 0) free++;
            pending += credit;
            if (free == 0)
            {
                // Repeated harvests merge into an existing visual without losing displayed credit.
                foreach (var f in flights)
                    if (f.queued || f.view.Credit > 0)
                    {
                        if (f.queued) f.queuedCredit += credit; else f.view.Merge(credit);
                        break;
                    }
            }
            else
            {
                int count = Mathf.Min(desired, free), index = 0;
                foreach (var f in flights)
                {
                    if (index == count) break;
                    if (f.busy && (f.queued || !f.view.Arrived || f.view.Credit > 0)) continue;
                    f.view.Release(); f.busy = f.queued = true; f.delay = index * Mathf.Max(0, flightStagger);
                    f.queuedCredit = credit / count + (index < credit % count ? 1 : 0);
                    var d = drops[index % drops.Length];
                    f.start = d.image.gameObject.activeSelf ? d.image.rectTransform.anchoredPosition : Ground(index);
                    index++;
                }
            }
            foreach (var d in drops) { d.falling = false; d.image.gameObject.SetActive(false); }
            presented = true; RefreshWallet();
        }
        public void RefreshWallet()
        {
            if (owner == null || progress == null) return;
            owner.wallet.text = Mathf.Max(0, progress.Cheeses - (Visible ? pending : 0)).ToString();
        }
        void LateUpdate() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float delta)
        {
            if (progress == null || drops == null) return;
            if (!Visible)
            {
                if (presented || pending > 0 || pulse > 0) CancelAndSync();
                return;
            }
            if (!presented) RestorePile();
            delta = Mathf.Max(0, delta);
            foreach (var d in drops)
            {
                if (!d.falling) continue;
                d.elapsed += delta;
                float y = FallingHeight(d, out bool settled);
                Position(d.image, new Vector2(d.start.x, y));
                d.falling = !settled;
            }
            Vector2 end = area.InverseTransformPoint(destination.TransformPoint(destination.rect.center));
            foreach (var f in flights)
            {
                if (!f.busy) continue;
                float step = delta;
                if (f.queued)
                {
                    f.delay -= delta; if (f.delay > 0) continue;
                    step = -f.delay; f.queued = false;
                    f.view.Launch(Vector2.zero, dropTemplate.sprite, f.queuedCredit, sequence++); f.queuedCredit = 0;
                }
                bool done = f.view.Advance(step, f.start, end);
                if (f.view.Arrived && f.view.Credit > 0)
                {
                    pending -= f.view.TakeCredit(); pulse = pulseDuration;
                    // Each arriving proxy can play a pickup; the shared audio gate merges same-frame bursts.
                    TownAudio.Instance?.PlayGroundPickup();
                }
                if (done) { f.view.Release(); f.busy = false; }
            }
            pulse = Mathf.Max(0, pulse - delta);
            float scale = 1 + pulseScale * Mathf.Sin(Mathf.PI * pulse / Mathf.Max(.05f, pulseDuration));
            destination.localScale = iconScale * scale; owner.wallet.transform.localScale = countScale * scale;
            RefreshWallet();
        }
        public void CancelAndSync()
        {
            pending = 0; pulse = 0; presented = false;
            if (drops != null) foreach (var d in drops) { d.falling = false; d.image.gameObject.SetActive(false); }
            if (flights != null) foreach (var f in flights) { f.view.Release(); f.busy = f.queued = false; f.queuedCredit = 0; }
            if (destination != null) destination.localScale = iconScale;
            if (owner != null && owner.wallet != null)
            {
                owner.wallet.transform.localScale = countScale;
                if (progress != null) owner.wallet.text = progress.Cheeses.ToString();
            }
        }
        void OnDisable() { CancelAndSync(); }
        void OnDestroy() { if (progress != null) progress.BatchProduced -= Produced; }
    }
}
