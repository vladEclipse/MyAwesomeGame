using UnityEngine;

/// <summary>
/// Слои физики. Игрок и мобы лежат на слое Actors, всё остальное считается миром (пол, стены, мосты).
/// </summary>
public static class Layers
{
    private static int actorsMask = -1;

    /// <summary>Маска слоя Actors (игрок и мобы).</summary>
    public static int Actors
    {
        get
        {
            if (actorsMask < 0)
            {
                int layer = LayerMask.NameToLayer("Actors");
                actorsMask = layer >= 0 ? 1 << layer : 0;
                if (layer < 0) Debug.LogError("Layers: нет слоя Actors (Project Settings > Tags and Layers)");
            }
            return actorsMask;
        }
    }

    /// <summary>Всё, кроме игрока и мобов: для лучей «земля», «стена», «прямая видимость».</summary>
    public static int World => Physics.DefaultRaycastLayers & ~Actors;
}
