using UnityEngine;

public enum ImpactMaterial { Wood, Rock, Foliage }

[RequireComponent(typeof(AudioSource))]
public class ProceduralFoley : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Standard dry seasonal footsteps
    public void TriggerSeasonalStep(Season activeSeason)
    {
        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.12f); // 120ms
        float[] sampleData = new float[sampleLength];
        System.Random rnd = new System.Random();

        for (int i = 0; i < sampleLength; i++)
        {
            float envelope = 1.0f - ((float)i / sampleLength);
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);

            switch (activeSeason)
            {
                case Season.Winter:
                    if (i % 3 == 0) noise *= 1.8f;
                    sampleData[i] = noise * envelope * 0.75f;
                    break;
                case Season.Summer:
                    float lowPulse = Mathf.Sin(2.0f * Mathf.PI * i * 65.0f / sampleRate);
                    sampleData[i] = (lowPulse * 0.75f + noise * 0.25f) * envelope * 0.9f;
                    break;
                case Season.Spring:
                    float squelch = Mathf.Sin(2.0f * Mathf.PI * i * 190.0f / sampleRate);
                    sampleData[i] = (squelch * noise) * envelope * 0.8f;
                    break;
                case Season.Autumn:
                    if (i % 2 == 0) noise *= 2.2f;
                    sampleData[i] = noise * envelope * 0.65f;
                    break;
            }
        }

        AudioClip clip = AudioClip.Create("SeasonalStep", sampleLength, 1, sampleRate, false);
        clip.SetData(sampleData, 0);
        audioSource.PlayOneShot(clip);
    }

    // River water splash footstep (for non-winter river walking)
    public void TriggerWaterStep()
    {
        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.16f); // 160ms splash
        float[] sampleData = new float[sampleLength];
        System.Random rnd = new System.Random();

        float phase1 = 0f;
        float phase2 = 0f;

        for (int i = 0; i < sampleLength; i++)
        {
            float progress = (float)i / sampleLength;
            float envelope = Mathf.Exp(-progress * 9.5f); // Fast exponential decay

            // Dual resonant splash bubble frequencies (descending pitch sweep)
            float freq1 = Mathf.Lerp(450f, 180f, progress);
            float freq2 = Mathf.Lerp(850f, 320f, progress);

            phase1 += 2.0f * Mathf.PI * freq1 / sampleRate;
            phase2 += 2.0f * Mathf.PI * freq2 / sampleRate;

            float bubbles = (Mathf.Sin(phase1) * 0.55f + Mathf.Sin(phase2) * 0.45f);
            float noiseSquelch = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.4f;

            sampleData[i] = (bubbles + noiseSquelch) * envelope * 0.95f;
        }

        AudioClip splashClip = AudioClip.Create("WaterStepSplash", sampleLength, 1, sampleRate, false);
        splashClip.SetData(sampleData, 0);
        audioSource.PlayOneShot(splashClip);
    }

    public void TriggerImpact(ImpactMaterial material, float force = 1.0f)
    {
        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.18f);
        float[] sampleData = new float[sampleLength];
        System.Random rnd = new System.Random();

        float baseFreq = 120f;
        float pitchDecaySpeed = 8.0f;
        float noiseAmount = 0.35f;

        switch (material)
        {
            case ImpactMaterial.Wood:
                baseFreq = 110f;
                pitchDecaySpeed = 12.0f;
                noiseAmount = 0.25f;
                break;
            case ImpactMaterial.Rock:
                baseFreq = 420f;
                pitchDecaySpeed = 22.0f;
                noiseAmount = 0.45f;
                break;
            case ImpactMaterial.Foliage:
                baseFreq = 260f;
                pitchDecaySpeed = 4.0f;
                noiseAmount = 0.85f;
                break;
        }

        float phase = 0.0f;
        for (int i = 0; i < sampleLength; i++)
        {
            float progress = (float)i / sampleLength;
            float envelope = Mathf.Exp(-progress * pitchDecaySpeed);

            float currentFreq = Mathf.Max(40f, baseFreq * (1.0f - progress * 0.4f));
            phase += 2.0f * Mathf.PI * currentFreq / sampleRate;

            float tone = Mathf.Sin(phase);
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float initialClick = (i < sampleRate * 0.006f) ? (float)(rnd.NextDouble() * 2.0 - 1.0) * 1.5f : 0f;

            sampleData[i] = ((tone * (1f - noiseAmount) + noise * noiseAmount) * envelope + initialClick) * force * 0.85f;
        }

        AudioClip impactClip = AudioClip.Create("ProceduralImpact", sampleLength, 1, sampleRate, false);
        impactClip.SetData(sampleData, 0);
        audioSource.PlayOneShot(impactClip);
    }
}