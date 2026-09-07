using UnityEngine;

public enum Season { Winter, Summer, Spring, Autumn }
public enum EmotionMode { Neutral, Happy, Panic, Storm, Anger, Lust, Disgust }

[RequireComponent(typeof(AudioSource))]
public class ProceduralSoundstage : MonoBehaviour
{
    [Header("Engine State")]
    public Season currentSeason = Season.Winter;
    public EmotionMode currentEmotion = EmotionMode.Neutral;
    public bool isAudioMuted = false;
    public bool isComparisonMode = false;

    [Header("Balanced Volumes")]
    [Range(0f, 0.5f)] public float windBaseVolume = 0.07f;
    [Range(0f, 2.0f)] public float emotionDroneVolume = 0.85f;
    [Range(20f, 4000f)] public float windCutoff = 450f;
    [Range(0f, 1f)] public float weatherIntensity = 0.35f;

    [Header("Spring Rain Audio Settings (Balanced)")]
    [Range(0f, 1.0f)] public float springRainVolume = 0.22f;

    [Range(0f, 1f)] public float tensionLevel = 0.0f;
    [Range(-1f, 1f)] public float harmonicMode = 0.0f;

    private float sampleRate;
    private float filterMemory = 0.0f;
    private float rainFilterLow = 0.0f;
    private float rainFilterHigh = 0.0f;
    private float dropletDecay = 0.0f;
    private float dropletFreq = 1200f;
    private float dropletPhase = 0.0f;

    private float oscPhase1 = 0f, oscPhase2 = 0f, oscPhase3 = 0f;
    private float lfoPhase = 0f;
    private float mockLoopPhase = 0f;
    private System.Random random = new System.Random();
    private AudioSource audioSource;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        audioSource = GetComponent<AudioSource>();
        if (!audioSource.isPlaying)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.Play();
        }
    }

    public void CycleNextSeason()
    {
        currentSeason = (Season)(((int)currentSeason + 1) % 4);
    }

    public void SetEmotion(EmotionMode emotion)
    {
        currentEmotion = emotion;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (isAudioMuted)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        if (isComparisonMode)
        {
            for (int i = 0; i < data.Length; i += channels)
            {
                mockLoopPhase += 2.0f * Mathf.PI * 180.0f / sampleRate;
                if (mockLoopPhase > 2.0f * Mathf.PI) mockLoopPhase -= 2.0f * Mathf.PI;

                float repetitivePulse = Mathf.Sin(mockLoopPhase * 0.5f);
                float staticSample = Mathf.Sin(mockLoopPhase) * 0.25f * (0.8f + 0.2f * repetitivePulse);

                for (int c = 0; c < channels; c++) data[i + c] = staticSample;
            }
            return;
        }

        float rc = 1.0f / (2.0f * Mathf.PI * Mathf.Max(20f, windCutoff));
        float dt = 1.0f / sampleRate;
        float alpha = dt / (rc + dt);

        float rcRainLow = 1.0f / (2.0f * Mathf.PI * 6500f);
        float alphaRainLow = dt / (rcRainLow + dt);

        float rcRainHigh = 1.0f / (2.0f * Mathf.PI * 1400f);
        float alphaRainHigh = dt / (rcRainHigh + dt);

        float f1 = 130.81f, f2 = 196.00f, f3 = 261.63f;
        float lfoSpeed = 1.0f;
        bool applyHardClipping = false;

        switch (currentEmotion)
        {
            case EmotionMode.Happy:
                f1 = 130.81f; f2 = 164.81f; f3 = 196.00f;
                lfoSpeed = 0.5f;
                break;
            case EmotionMode.Panic:
                f1 = 73.42f; f2 = 103.83f; f3 = 146.83f;
                lfoSpeed = 8.0f;
                break;
            case EmotionMode.Storm:
                f1 = 45.00f; f2 = 90.00f; f3 = 135.00f;
                lfoSpeed = 0.8f;
                break;
            case EmotionMode.Anger:
                f1 = 82.41f; f2 = 87.31f; f3 = 123.47f;
                lfoSpeed = 12.0f;
                applyHardClipping = true;
                break;
            case EmotionMode.Lust:
                f1 = 110.00f; f2 = 165.00f; f3 = 207.65f;
                lfoSpeed = 0.35f;
                break;
            case EmotionMode.Disgust:
                f1 = 98.00f; f2 = 101.50f; f3 = 143.00f;
                lfoSpeed = 2.5f;
                break;
            case EmotionMode.Neutral:
            default:
                break;
        }

        for (int i = 0; i < data.Length; i += channels)
        {
            float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
            filterMemory = filterMemory + alpha * (whiteNoise - filterMemory);
            float windSample = filterMemory * windBaseVolume * (weatherIntensity + 0.1f);

            float rainSample = 0.0f;
            if (currentSeason == Season.Spring || currentEmotion == EmotionMode.Storm)
            {
                rainFilterLow = rainFilterLow + alphaRainLow * (whiteNoise - rainFilterLow);
                rainFilterHigh = rainFilterHigh + alphaRainHigh * (rainFilterLow - rainFilterHigh);
                float rainContinuous = (rainFilterLow - rainFilterHigh) * 0.18f;

                if (random.NextDouble() < 0.0008)
                {
                    dropletDecay = 0.8f;
                    dropletPhase = 0.0f;
                    dropletFreq = (float)(random.NextDouble() * 1000.0 + 1000.0);
                }

                float dropletSample = 0f;
                if (dropletDecay > 0.01f)
                {
                    dropletPhase += 2.0f * Mathf.PI * dropletFreq / sampleRate;
                    dropletSample = Mathf.Sin(dropletPhase) * dropletDecay * 0.25f;
                    dropletDecay *= 0.9975f;
                }

                rainSample = (rainContinuous + dropletSample) * springRainVolume;
            }

            lfoPhase += 2.0f * Mathf.PI * lfoSpeed / sampleRate;
            if (lfoPhase > 2.0f * Mathf.PI) lfoPhase -= 2.0f * Mathf.PI;
            float lfoVal = (Mathf.Sin(lfoPhase) + 1.0f) * 0.5f;

            float droneSample = 0.0f;
            if (currentEmotion != EmotionMode.Neutral)
            {
                oscPhase1 += 2.0f * Mathf.PI * f1 / sampleRate;
                oscPhase2 += 2.0f * Mathf.PI * f2 / sampleRate;
                oscPhase3 += 2.0f * Mathf.PI * f3 / sampleRate;

                if (oscPhase1 > 2.0f * Mathf.PI) oscPhase1 -= 2.0f * Mathf.PI;
                if (oscPhase2 > 2.0f * Mathf.PI) oscPhase2 -= 2.0f * Mathf.PI;
                if (oscPhase3 > 2.0f * Mathf.PI) oscPhase3 -= 2.0f * Mathf.PI;

                droneSample = (Mathf.Sin(oscPhase1) * 0.45f +
                               Mathf.Sin(oscPhase2) * 0.35f +
                               Mathf.Sin(oscPhase3) * 0.20f);

                if (applyHardClipping)
                {
                    droneSample = Mathf.Clamp(droneSample * 2.5f, -0.7f, 0.7f);
                }

                droneSample *= (0.6f + 0.4f * lfoVal) * emotionDroneVolume;
            }

            float finalMix = windSample + droneSample + rainSample;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] += finalMix;
            }
        }
    }
}