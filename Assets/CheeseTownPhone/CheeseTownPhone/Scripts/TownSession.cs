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
            revision = Settings.Revision;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (FindAnyObjectByType<CheeseTownDemo>() == null)
                new GameObject("Town tablet").AddComponent<CheeseTownDemo>();
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
            Progress.Tick(Time.deltaTime);
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance != this) return;
            if (ownsSettings) Destroy(Settings);
            instance = null;
        }
    }
}