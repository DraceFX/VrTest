using UnityEngine;

public class WeldEffectsManager : MonoBehaviour
{
    [Header("Частицы")]
    public ParticleSystem sparks;  // Искры
    public ParticleSystem smoke;   // Дым/газ

    [Header("Свет дуги")]
    public Light arcLight;         // Точечный свет
    public float baseIntensity = 8f;
    public float flickerSpeed = 15f;

    [Header("Звук")]
    [Tooltip("Зацикленный звук горения дуги")]
    public AudioClip arcLoopClip;
    [Tooltip("Короткие потрескивания/взрывы (опционально)")]
    public AudioClip[] crackleClips;
    [Range(0f, 1f)] public float crackleFrequency = 0.03f;

    private AudioSource audioSource;
    private float targetIntensity;
    private float flickerPhase;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1.0f; // 3D звук для VR
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.maxDistance = 3f;

        if (arcLoopClip != null)
            audioSource.clip = arcLoopClip;
    }

    public void Play()
    {
        if (sparks) sparks.Play();
        if (smoke) smoke.Play();
        if (arcLight) arcLight.enabled = true;

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void Stop()
    {
        if (sparks) sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (smoke) smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (arcLight) arcLight.enabled = false;

        audioSource.Stop();
        targetIntensity = 0f;
        flickerPhase = 0f;
    }

    public void SetPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    /// <summary>
    /// Вызывается каждый кадр сварки для обновления интенсивности
    /// </summary>
    public void UpdateEffects(float power, float optimalPower)
    {
        if (!audioSource.isPlaying) return;

        float ratio = power / optimalPower;
        targetIntensity = baseIntensity * Mathf.Clamp(ratio, 0.4f, 1.8f);
        flickerPhase += Time.deltaTime * flickerSpeed;

        // 1. Свет дуги (мерцание + плавное изменение)
        if (arcLight != null)
        {
            float flicker = Mathf.Sin(flickerPhase) * 0.3f + Mathf.Sin(flickerPhase * 2.7f) * 0.2f;
            arcLight.intensity = Mathf.Lerp(arcLight.intensity, targetIntensity * (1f + flicker), Time.deltaTime * 12f);
            arcLight.range = 0.5f + ratio * 0.8f;
        }

        // 2. Звук дуги (громкость + тон)
        audioSource.volume = Mathf.Lerp(audioSource.volume, Mathf.Clamp01(ratio), Time.deltaTime * 6f);
        audioSource.pitch = Mathf.Lerp(audioSource.pitch, 0.75f + ratio * 0.5f, Time.deltaTime * 5f);

        // 3. Потрескивание (случайные короткие звуки)
        if (crackleClips != null && crackleClips.Length > 0 && Random.value < crackleFrequency * ratio)
        {
            AudioClip clip = crackleClips[Random.Range(0, crackleClips.Length)];
            audioSource.PlayOneShot(clip, 0.4f + ratio * 0.6f);
        }
    }
}
