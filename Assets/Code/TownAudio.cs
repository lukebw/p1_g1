using UnityEngine;
using CheeseTownPhone;

// All clips, looping and balanced volumes live on editable AudioSource components.
[DisallowMultipleComponent]
public sealed class TownAudio : MonoBehaviour
{
    public AudioSource mainMusic, endingMusic, pickup, shop, uiClick, upgrade, failed, axe, footsteps;
    TownProgress progress;
    PlayerController player;
    Vector3 previousPosition;
    bool lateMusic;
    public static TownAudio Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        progress = TownSession.Instance.Progress;
        progress.Collected += PlayPickup;
        progress.Purchased += PlayUpgrade;
        progress.PurchaseFailed += PlayFailed;
        progress.ShopOpened += PlayShop;
        progress.Changed += UpdateMusic;
        UpdateMusic();
    }
    public void PlayPickup() { Play(pickup); }
    public void PlayShop() { Play(shop); }
    public void PlayUI() { Play(uiClick); }
    public void PlayUpgrade() { Play(upgrade); }
    public void PlayFailed() { Play(failed); }
    public void PlayAxe() { Play(axe); }
    static void Play(AudioSource source)
    {
        if (source != null && source.clip != null) source.PlayOneShot(source.clip);
    }
    void UpdateMusic()
    {
        if (progress.GameEnded) { mainMusic.Stop(); endingMusic.Stop(); footsteps.Stop(); return; }
        bool hasReserve = false;
        foreach (var letter in progress.Letters) if (letter.Id == "reserve") hasReserve = true;
        if (hasReserve && !lateMusic)
        {
            lateMusic = true;
            mainMusic.Stop();
            endingMusic.Play();
        }
        else if (!hasReserve && !mainMusic.isPlaying) mainMusic.Play();
    }
    void LateUpdate()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null) previousPosition = player.transform.position;
        }
        var tablet = FindAnyObjectByType<CheeseTownDemo>();
        bool moving = player != null && player.isActiveAndEnabled && progress != null && !progress.GameEnded
            && (tablet == null || !tablet.BlocksWorldInput)
            && (player.transform.position - previousPosition).sqrMagnitude > .00000001f;
        if (player != null) previousPosition = player.transform.position;
        if (moving && !footsteps.isPlaying) footsteps.Play();
        else if (!moving && footsteps.isPlaying) footsteps.Stop();
    }
    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (progress == null) return;
        progress.Collected -= PlayPickup;
        progress.Purchased -= PlayUpgrade;
        progress.PurchaseFailed -= PlayFailed;
        progress.ShopOpened -= PlayShop;
        progress.Changed -= UpdateMusic;
    }
}
