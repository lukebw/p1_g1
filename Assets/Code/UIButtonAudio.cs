using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public sealed class UIButtonAudio : MonoBehaviour
{
    public bool shopButton;
    Button button;
    void Start() { button = GetComponent<Button>(); button.onClick.AddListener(Play); }
    void Play() { if (!shopButton) TownAudio.Instance?.PlayUI(); }
    void OnDestroy() { if (button != null) button.onClick.RemoveListener(Play); }
}
