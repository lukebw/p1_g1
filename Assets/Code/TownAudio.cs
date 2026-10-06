using UnityEngine;
using CheeseTownPhone;
using UnityEngine.Serialization;

// All clips, looping and balanced volumes live on editable AudioSource components.
[DisallowMultipleComponent]
public sealed class TownAudio : MonoBehaviour
{
    public AudioSource mainMusic, endingMusic, shop, uiClick, upgrade, failed, axe, footsteps;
    // BEGIN CHANGED: Reuse the existing clip when a tree releases cheese, not when income is credited.
    [FormerlySerializedAs("pickup")]
    public AudioSource treeDrop;
    // BEGIN ADDED: Ground pickup is triggered by the physical collector, never by passive income.
    public AudioSource groundPickup;
    [Min(0)] public float pickupSoundInterval = .06f;
    float nextPickupSoundTime;
    // END ADDED
    [Header("Footstep fades (unscaled seconds)")]
    [Min(.01f)] public float footstepFadeIn = .12f;
    [Min(.01f)] public float footstepFadeOut = .22f;
    AudioBalance footstepBalance;
    float footstepVolume, footstepGain;
    // END CHANGED
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
        if (footsteps != null)
        {
            footstepVolume = footsteps.volume;
            footstepBalance = footsteps.GetComponent<AudioBalance>();
        }
    }
    void Start()
    {
        progress = TownSession.Instance.Progress;
        progress.Purchased += PlayUpgrade;
        progress.PurchaseFailed += PlayFailed;
        progress.ShopOpened += PlayShop;
        progress.Changed += UpdateMusic;
        UpdateMusic();
    }
    public void PlayTreeDrop() { Play(treeDrop); }
    // BEGIN ADDED: Merge near-simultaneous pickups to avoid stacking a loud burst of identical clips.
    public void PlayGroundPickup()
    {
        if (groundPickup == null || groundPickup.clip == null || Time.unscaledTime < nextPickupSoundTime) return;
        nextPickupSoundTime = Time.unscaledTime + pickupSoundInterval;
        Play(groundPickup);
    }
    // END ADDED
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
        if (progress.GameEnded) { mainMusic.Stop(); endingMusic.Stop(); return; }
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
        UpdateFootsteps(moving, Time.unscaledDeltaTime);
    }
    // BEGIN ADDED: Reverse smoothly from the current gain; stop the loop only after it is silent.
    void UpdateFootsteps(bool moving, float delta)
    {
        if (footsteps == null) return;
        float duration = moving ? footstepFadeIn : footstepFadeOut;
        footstepGain = Mathf.MoveTowards(footstepGain, moving ? 1 : 0, delta / Mathf.Max(.01f, duration));
        float volume = footstepBalance != null ? footstepBalance.BalancedVolume : footstepVolume;
        footsteps.volume = volume * footstepGain;
        if (moving && !footsteps.isPlaying) footsteps.Play();
        else if (!moving && footstepGain <= 0 && footsteps.isPlaying) footsteps.Stop();
    }
    void OnDisable()
    {
        footstepGain = 0;
        if (footsteps != null) { footsteps.Stop(); footsteps.volume = 0; }
    }
    // END ADDED
    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        if (progress == null) return;
        progress.Purchased -= PlayUpgrade;
        progress.PurchaseFailed -= PlayFailed;
        progress.ShopOpened -= PlayShop;
        progress.Changed -= UpdateMusic;
    }
}
