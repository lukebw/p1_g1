using UnityEngine;
[RequireComponent(typeof(ChopHitbox))]
[DisallowMultipleComponent]
public sealed class TreeHitAudio : MonoBehaviour
{
    CursorController cursor;
    ChopHitbox chopHitbox;
    void Awake() { chopHitbox = GetComponent<ChopHitbox>(); }
    void OnEnable() { chopHitbox.onTreeHit.AddListener(Play); }
    void OnDisable() { chopHitbox.onTreeHit.RemoveListener(Play); }
    void Play() { TownAudio.Instance?.PlayAxe(); }
}
