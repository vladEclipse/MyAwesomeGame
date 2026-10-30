using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Вешается на Canvas: все кнопки внутри получают звук нажатия.
/// </summary>
public class UiClickSound : MonoBehaviour
{
    private void Start()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private static void PlayClick()
    {
        Sfx.Play(SfxId.UiClick, 0.8f, 0.03f);
    }
}
