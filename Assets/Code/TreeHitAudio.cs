using UnityEngine;
[RequireComponent(typeof(ChopHitbox))]
[DisallowMultipleComponent]
public sealed class TreeHitAudio : MonoBehaviour
{
    public void Play() { 
        Debug.Log("TreeHitAudio.Play() called");
        TownAudio.Instance?.PlayAxe();
    }
}
