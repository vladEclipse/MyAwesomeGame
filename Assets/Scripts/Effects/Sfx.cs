using System.Collections.Generic;
using UnityEngine;

public enum SfxId
{
    UiClick,
    Swing,
    Dash,
    Step,
    EnemyHit,
    EnemyAttack,
    EnemyDeath,
    PlayerHurt,
    PlayerDeath,
    Spawn,
    Coin,
    Win
}

/// <summary>
/// Простой проигрыватель звуков. Клипы берутся из Assets/Resources/Audio (файлы Sfx_&lt;имя&gt;.wav),
/// чтобы любой звук можно было заменить, просто положив файл с тем же именем.
/// Фоновая музыка Music_Ambient запускается сама при старте сцены.
/// </summary>
public static class Sfx
{
    private const string Folder = "Audio/";
    private const float HearingDistance = 28f;

    private static readonly Dictionary<SfxId, AudioClip> Clips = new Dictionary<SfxId, AudioClip>();
    private static SfxRunner runner;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Clips.Clear();
        runner = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartMusic()
    {
        GetRunner().StartMusic();
    }

    /// <summary>Звук без привязки к месту (интерфейс, действия игрока).</summary>
    public static void Play(SfxId id, float volume = 1f, float pitchVariation = 0.08f)
    {
        AudioClip clip = GetClip(id);
        if (clip == null) return;

        GetRunner().PlayClip(clip, volume, 1f + Random.Range(-pitchVariation, pitchVariation));
    }

    /// <summary>Звук в точке мира: чем дальше от игрока, тем тише.</summary>
    public static void PlayAt(SfxId id, Vector3 position, float volume = 1f, float pitchVariation = 0.08f)
    {
        float attenuation = 1f;

        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            float distance = Vector3.Distance(position, player.transform.position);
            attenuation = Mathf.Clamp01(1f - distance / HearingDistance);
            attenuation *= attenuation;
        }

        if (attenuation < 0.02f) return;

        Play(id, volume * attenuation, pitchVariation);
    }

    private static AudioClip GetClip(SfxId id)
    {
        if (Clips.TryGetValue(id, out AudioClip cached) && cached != null) return cached;

        AudioClip clip = Resources.Load<AudioClip>(Folder + "Sfx_" + id);
        if (clip == null)
        {
            Debug.LogWarning($"Sfx: не найден звук Resources/{Folder}Sfx_{id}");
            return null;
        }

        Clips[id] = clip;
        return clip;
    }

    private static SfxRunner GetRunner()
    {
        if (runner != null) return runner;

        var go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<SfxRunner>();
        return runner;
    }
}

/// <summary>Держит несколько AudioSource для звуков и один для музыки.</summary>
public class SfxRunner : MonoBehaviour
{
    private const int VoiceCount = 12;
    private const float MusicVolume = 0.22f;

    private AudioSource[] voices;
    private AudioSource music;
    private int next;

    private void Awake()
    {
        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            voices[i] = CreateSource("Voice " + i);
        }

        music = CreateSource("Music");
        music.loop = true;
        music.volume = MusicVolume;
    }

    private AudioSource CreateSource(string sourceName)
    {
        var child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    public void PlayClip(AudioClip clip, float volume, float pitch)
    {
        AudioSource source = null;

        // Ищем свободный источник, если все заняты - перебиваем самый давний
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource candidate = voices[(next + i) % VoiceCount];
            if (!candidate.isPlaying)
            {
                source = candidate;
                next = (next + i + 1) % VoiceCount;
                break;
            }
        }

        if (source == null)
        {
            source = voices[next];
            next = (next + 1) % VoiceCount;
        }

        source.pitch = pitch;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void StartMusic()
    {
        if (music.isPlaying) return;

        AudioClip clip = Resources.Load<AudioClip>("Audio/Music_Ambient");
        if (clip == null) return;

        music.clip = clip;
        music.Play();
    }
}
