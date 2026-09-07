using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralRiverAudio : MonoBehaviour
{
    public ProceduralSoundstage soundstage;
    public Transform listenerTransform;

    [Header("Acoustic Range")]
    public float audibleDistance = 18f;
    [Range(0f, 1.5f)] public float masterWaterVolume = 0.85f;

    private float cachedDistanceGain = 0f;
    private Season cachedSeason = Season.Summer;
    private bool cachedIsMuted = false;

    private float sampleRate;
    private float filterLow1 = 0f, filterHigh1 = 0f;
    private float filterLow2 = 0f, filterHigh2 = 0f;
    private float bubblePhase = 0f;
    private float iceCrackDecay = 0f;
    private float iceCrackFreq = 800f;
    private System.Random rnd = new System.Random();

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        AudioSource src = GetComponent<AudioSource>();
        src.playOnAwake = true;
        src.loop = true;
        if (!src.isPlaying) src.Play();
    }

    void Start()
    {
        if (listenerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) listenerTransform = player.transform;
            else if (Camera.main != null) listenerTransform = Camera.main.transform;
        }

        if (soundstage == null)
        {
            soundstage = FindAnyObjectByType<ProceduralSoundstage>();
        }
    }

    void Update()
    {
        if (soundstage != null)
        {
            cachedIsMuted = soundstage.isAudioMuted;
            cachedSeason = soundstage.currentSeason;
        }

        float distance = 100f;
        if (listenerTransform != null)
        {
            distance = Mathf.Abs(listenerTransform.position.x - transform.position.x);
        }

        float gain = Mathf.Clamp01(1.0f - (distance / audibleDistance));
        cachedDistanceGain = gain * gain;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (cachedIsMuted || cachedDistanceGain <= 0.0001f)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        float dt = 1.0f / sampleRate;
        float flowSpeedFactor = 1.0f;

        if (cachedSeason == Season.Spring) flowSpeedFactor = 1.4f;
        else if (cachedSeason == Season.Summer) flowSpeedFactor = 0.65f;
        else if (cachedSeason == Season.Autumn) flowSpeedFactor = 0.9f;

        float lowPassCutoff = 2800f * flowSpeedFactor;
        float highPassCutoff = 350f * flowSpeedFactor;

        float alphaLow = dt / ((1.0f / (2.0f * Mathf.PI * lowPassCutoff)) + dt);
        float alphaHigh = dt / ((1.0f / (2.0f * Mathf.PI * highPassCutoff)) + dt);

        for (int i = 0; i < data.Length; i += channels)
        {
            float waterSample = 0f;

            if (cachedSeason == Season.Winter)
            {
                if (rnd.NextDouble() < 0.00008)
                {
                    iceCrackDecay = 1.0f;
                    iceCrackFreq = (float)(rnd.NextDouble() * 1400.0 + 400.0);
                }

                if (iceCrackDecay > 0.01f)
                {
                    bubblePhase += 2.0f * Mathf.PI * iceCrackFreq / sampleRate;
                    waterSample = Mathf.Sin(bubblePhase) * iceCrackDecay * 0.4f;
                    iceCrackDecay *= 0.997f;
                }
            }
            else
            {
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);

                filterLow1 += alphaLow * (noise - filterLow1);
                filterHigh1 += alphaHigh * (filterLow1 - filterHigh1);
                float rush = (filterLow1 - filterHigh1);

                filterLow2 += alphaLow * (rush - filterLow2);
                filterHigh2 += alphaHigh * (filterLow2 - filterHigh2);
                float streamBed = (filterLow2 - filterHigh2) * 1.8f;

                bubblePhase += 2.0f * Mathf.PI * (520f + Mathf.Sin((float)i * 0.01f) * 140f) / sampleRate;
                float trickle = Mathf.Sin(bubblePhase) * 0.12f * streamBed;

                waterSample = (streamBed + trickle) * flowSpeedFactor;
            }

            float finalOutput = waterSample * cachedDistanceGain * masterWaterVolume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] += finalOutput;
            }
        }
    }
}