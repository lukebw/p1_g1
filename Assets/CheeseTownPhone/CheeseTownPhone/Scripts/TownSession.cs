using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheeseTownPhone
{
    // One economy and clock per play session, independent of any scene or tablet.
    public sealed class TownSession : MonoBehaviour
    {
        static TownSession instance;
        public static TownSession Instance
        {
            get
            {
                if (instance == null) new GameObject("Shared cheese progress").AddComponent<TownSession>();
                return instance;
            }
        }
        public TabletSettings Settings { get; private set; }
        public TownProgress Progress { get; private set; }
        bool ownsSettings;
        int revision;
        PrologueView prologue;
        bool prologueCompleted;
        int releaseInputFrame = -1;
        public bool PrologueActive => (prologue != null && prologue.gameObject.activeSelf) || Time.frameCount <= releaseInputFrame;
        public PrologueView Prologue => prologue;
        PrologueView epilogue;
        bool epilogueStarted, epilogueCompleted;
        public PrologueView Epilogue => epilogue;
        public bool EpilogueActive => epilogue != null && epilogue.gameObject.activeSelf;
        public bool NarrativeActive => PrologueActive || EpilogueActive;
        public bool EndingReady => Progress.GameEnded && !Progress.EndingResolved && epilogueCompleted && !NarrativeActive;
        public event System.Action NarrativeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap() { var session = Instance; }
        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Settings = Resources.Load<TabletSettings>("TabletSettings");
            if (Settings == null) { Settings = ScriptableObject.CreateInstance<TabletSettings>(); ownsSettings = true; }
            Progress = new TownProgress(Settings);
            Progress.Changed += BeginEndingIfNeeded;
            revision = Settings.Revision;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Keep the opening across scene changes and show it once per new play session.
            if (!Progress.GameEnded && !prologueCompleted && prologue == null && Settings.prologuePrefab != null && FindAnyObjectByType<PlayerController>() != null)
            {
                prologue = Instantiate(Settings.prologuePrefab, transform);
                prologue.Begin(() => { prologueCompleted = true; releaseInputFrame = Time.frameCount + 1; });
            }
            if (FindAnyObjectByType<CheeseTownDemo>() == null)
                new GameObject("Town tablet").AddComponent<CheeseTownDemo>();
            BeginEndingIfNeeded();
        }
        void BeginEndingIfNeeded()
        {
            if (!Progress.GameEnded || epilogueStarted) return;
            // Economy has already stopped; this session-owned overlay survives scene reloads and plays once.
            epilogueStarted = true;
            if (prologue != null && prologue.gameObject.activeSelf) { prologue.Skip(); prologue.Step(10); }
            if (Settings.epiloguePrefab == null) { epilogueCompleted = true; return; }
            epilogue = Instantiate(Settings.epiloguePrefab, transform);
            epilogue.Begin(() => {
                epilogueCompleted = true;
                releaseInputFrame = Time.frameCount;
                NarrativeChanged?.Invoke();
            });
            NarrativeChanged?.Invoke();
        }
        void LateUpdate()
        {
            if (!Progress.EndingLetterPending || NarrativeActive) return;
            var demo = FindAnyObjectByType<CheeseTownDemo>();
            var view = demo != null ? demo.View : null;
            if (view != null && ((view.pickupFeedback != null && view.pickupFeedback.IsAnimating)
                || (view.harvestFeedback != null && view.harvestFeedback.IsAnimating))) return;
            Progress.PublishEndingLetter();
        }
        public bool ResolveEndingChoice(bool keepCollecting)
        {
            if (!EndingReady) return false;
            Progress.ResolveEnding(keepCollecting);
            releaseInputFrame = Time.frameCount;
            NarrativeChanged?.Invoke();
            return true;
        }
        public void Configure(TabletSettings settings)
        {
            if (settings == null) return;
            if (ownsSettings && Settings != settings) { Destroy(Settings); ownsSettings = false; }
            Settings = settings;
            Settings.ValidateSettings();
            Progress.Reconfigure(Settings);
            revision = Settings.Revision;
        }
        void Update()
        {
            if (revision != Settings.Revision)
            {
                Progress.Reconfigure(Settings);
                revision = Settings.Revision;
            }
            // Production and automatic collection continue while walking or changing scenes.
            if (!NarrativeActive) Progress.Tick(Time.deltaTime);
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance != this) return;
            Progress.Changed -= BeginEndingIfNeeded;
            if (ownsSettings) Destroy(Settings);
            instance = null;
        }
    }
}
