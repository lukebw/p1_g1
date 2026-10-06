using UnityEngine;
[RequireComponent(typeof(ChopHitbox))]
[DisallowMultipleComponent]
public sealed class TreeHitAudio : MonoBehaviour
{
    public void Play() {
        TownAudio.Instance?.PlayAxe();
    }
}
