using System.Collections;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class AudioBalance : MonoBehaviour
{
    [Range(.005f,.3f)] public float targetRms = .12f;
    [Range(.1f,1f)] public float peakLimit = .7f;
    public bool automatic = true;
    // BEGIN ADDED: Preserve the normalized level separately from the footstep fade envelope.
    public float BalancedVolume { get; private set; }
    void Awake() { BalancedVolume = GetComponent<AudioSource>().volume; }
    // END ADDED
    IEnumerator Start()
    {
        var source = GetComponent<AudioSource>();
        if (!automatic || source.clip == null) yield break;
        var clip = source.clip;
        clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading) yield return null;
        if (clip.loadState != AudioDataLoadState.Loaded) yield break;
        var samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0)) yield break;
        double sum = 0; float peak = 0;
        foreach (float sample in samples) { sum += (double)sample * sample; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
        double rms = System.Math.Sqrt(sum / System.Math.Max(1, samples.Length));
        BalancedVolume = Mathf.Clamp((float)System.Math.Min(targetRms / System.Math.Max(.0001, rms), peakLimit / System.Math.Max(.0001, peak)), .01f, 1f);
        source.volume = BalancedVolume;
    }
}
