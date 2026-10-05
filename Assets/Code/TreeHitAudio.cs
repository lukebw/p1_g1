using UnityEngine;
[RequireComponent(typeof(CursorController))]
[DisallowMultipleComponent]
public sealed class TreeHitAudio : MonoBehaviour
{
    CursorController cursor;
    void Awake() { cursor = GetComponent<CursorController>(); }
    void OnEnable() { cursor.onTreeHit.AddListener(Play); }
    void OnDisable() { cursor.onTreeHit.RemoveListener(Play); }
    void Play() { TownAudio.Instance?.PlayAxe(); }
}
