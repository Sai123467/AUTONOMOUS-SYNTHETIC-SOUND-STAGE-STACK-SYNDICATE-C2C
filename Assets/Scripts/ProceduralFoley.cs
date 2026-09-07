using UnityEngine;

public enum ImpactMaterial { Wood, Rock, Foliage }

[RequireComponent(typeof(AudioSource))]
public class ProceduralFoley : MonoBehaviour
{
    public static string LastSoundTriggered = "Idle";
    public static string LastSynthesisFormula = "Idle";
    public static float LastFundamentalFreq = 0f;
    public static float LastDecayTimeMs = 0f;
    public static int TotalSynthesizedWaves = 0;

    private AudioSource audioSource;
    private System.Random rnd = new System.Random();

    private float variationTimer = 0f;
    private int groundSubVariation = 0;
    private bool isLeftFoot = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0.0f;
    }

    void Update()
    {
        variationTimer += Time.deltaTime;
        if (variationTimer >= 2.0f)
        {
            groundSubVariation = (groundSubVariation + 1) % 4;
            variationTimer = 0f;
        }
    }

    public void TriggerSeasonalStep(Season activeSeason)
    {
        isLeftFoot = !isLeftFoot;

        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.25f);
        float[] pcm = new float[sampleLength];

        float footBias = isLeftFoot ? 0.94f : 1.05f;
        float perStepJitter = (float)(rnd.NextDouble() * 0.2 + 0.9);
        float heelToeDelayMs = (float)(rnd.NextDouble() * 16.0 + 32.0);
        int toeOffset = (int)(sampleRate * (heelToeDelayMs / 1000f));

        float lpHeel = 0f;
        float bpSurf = 0f;
        float hpSurf = 0f;

        float heelCutoff = (isLeftFoot ? 105f : 118f) * perStepJitter;
        float alphaHeel = CalculateAlpha(heelCutoff, sampleRate);

        LastFundamentalFreq = heelCutoff;
        LastDecayTimeMs = sampleLength * 1000f / sampleRate;
        LastSynthesisFormula = $"Heel: {heelCutoff:F0}Hz LP + {activeSeason} Patch #{groundSubVariation + 1}";

        for (int i = 0; i < sampleLength; i++)
        {
            float t = (float)i / sampleLength;
            float whiteNoise = (float)(rnd.NextDouble() * 2.0 - 1.0);

            float heelEnv = Mathf.Exp(-t * 28.0f);
            lpHeel += alphaHeel * (whiteNoise - lpHeel);
            float heelThud = lpHeel * heelEnv * 1.5f;

            float surfaceSample = 0f;

            if (i >= toeOffset)
            {
                int toeIdx = i - toeOffset;
                float tToe = (float)toeIdx / (sampleLength - toeOffset);

                switch (activeSeason)
                {
                    case Season.Winter:
                        {
                            float decayRate = 10f + groundSubVariation * 3.5f;
                            float snowEnv = Mathf.Exp(-tToe * decayRate);

                            float crystalCutoff = 2200f + groundSubVariation * 650f;
                            float crackChance = 0.08f + groundSubVariation * 0.06f;

                            float granularCrack = (rnd.NextDouble() < crackChance) ? (float)(rnd.NextDouble() * 2.0 - 1.0) * 1.9f : 0f;
                            bpSurf += CalculateAlpha(crystalCutoff, sampleRate) * (whiteNoise - bpSurf);
                            hpSurf += CalculateAlpha(1100f, sampleRate) * (bpSurf - hpSurf);

                            surfaceSample = (hpSurf + granularCrack) * snowEnv * 1.15f;
                            break;
                        }

                    case Season.Summer:
                        {
                            float earthEnv = Mathf.Exp(-tToe * (16.0f + groundSubVariation * 2.5f));
                            float earthBand = 650f + groundSubVariation * 220f;
                            float grit = (groundSubVariation >= 2 && rnd.NextDouble() < 0.15) ? (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.8f : 0f;

                            bpSurf += CalculateAlpha(earthBand, sampleRate) * (whiteNoise - bpSurf);
                            surfaceSample = (bpSurf + grit) * earthEnv * 0.95f;
                            break;
                        }

                    case Season.Spring:
                        {
                            float mudEnv = Mathf.Exp(-tToe * (7.5f + groundSubVariation * 2.0f));

                            float startFreq = 420f - groundSubVariation * 50f;
                            float endFreq = 120f + groundSubVariation * 25f;
                            float suctionFreq = Mathf.Lerp(startFreq, endFreq, tToe);
                            float mudPhase = 2.0f * Mathf.PI * suctionFreq * tToe;
                            float suctionOsc = Mathf.Sin(mudPhase);

                            bpSurf += CalculateAlpha(1600f + groundSubVariation * 300f, sampleRate) * (whiteNoise - bpSurf);
                            surfaceSample = (suctionOsc * 0.65f + bpSurf * 0.35f) * mudEnv * 1.1f;
                            break;
                        }

                    case Season.Autumn:
                        {
                            float leafEnv = Mathf.Exp(-tToe * (12.0f + groundSubVariation * 2.5f));
                            float snapDensity = 0.10f + groundSubVariation * 0.08f;
                            float snapMultiplier = 1.4f + groundSubVariation * 0.3f;

                            float snap = (rnd.NextDouble() < snapDensity) ? (float)(rnd.NextDouble() * 2.0 - 1.0) * snapMultiplier : 0f;
                            bpSurf += CalculateAlpha(3600f + groundSubVariation * 500f, sampleRate) * (whiteNoise - bpSurf);

                            surfaceSample = (bpSurf * 0.55f + snap * 0.85f) * leafEnv * 1.05f;
                            break;
                        }
                }
            }

            pcm[i] = (heelThud + surfaceSample) * footBias * perStepJitter * 0.82f;
        }

        PlaySynthesizedClip($"{activeSeason}_Step", pcm, sampleRate);
    }

    public void TriggerWaterStep()
    {
        isLeftFoot = !isLeftFoot;
        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.28f);
        float[] pcm = new float[sampleLength];

        float phase1 = 0f, phase2 = 0f;
        float lpNoise = 0f;

        float depthMod = 1.0f + groundSubVariation * 0.15f;
        float jitter = (float)(rnd.NextDouble() * 0.15 + 0.92);

        LastFundamentalFreq = 540f * depthMod;
        LastDecayTimeMs = sampleLength * 1000f / sampleRate;
        LastSynthesisFormula = $"Water: FM Plunge ({LastFundamentalFreq:F0}Hz->150Hz) + Cavitation Slosh";

        for (int i = 0; i < sampleLength; i++)
        {
            float t = (float)i / sampleLength;

            float entryEnv = Mathf.Exp(-t * (24.0f / depthMod));
            float sloshEnv = Mathf.Exp(-t * (6.5f / depthMod));

            float freq1 = Mathf.Lerp(540f * depthMod, 150f, t);
            float freq2 = Mathf.Lerp(900f, 280f * depthMod, t * 1.2f);
            phase1 += 2.0f * Mathf.PI * freq1 / sampleRate;
            phase2 += 2.0f * Mathf.PI * freq2 / sampleRate;

            float bubble = (Mathf.Sin(phase1) * 0.6f + Mathf.Sin(phase2) * 0.4f) * entryEnv;

            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lpNoise += CalculateAlpha(1500f, sampleRate) * (noise - lpNoise);
            float fluidSlosh = lpNoise * sloshEnv * 0.55f;

            pcm[i] = (bubble + fluidSlosh) * jitter * 0.95f;
        }

        PlaySynthesizedClip("WaterSplashStep", pcm, sampleRate);
    }

    public void TriggerImpact(ImpactMaterial material, float force = 1.0f)
    {
        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.22f);
        float[] pcm = new float[sampleLength];

        float f0 = 110f, f1 = 220f;
        float decayRate = 12.0f;
        float noiseMix = 0.2f;

        switch (material)
        {
            case ImpactMaterial.Wood:
                f0 = 95f; f1 = 285f;
                decayRate = 11.0f;
                noiseMix = 0.15f;
                break;
            case ImpactMaterial.Rock:
                f0 = 480f; f1 = 1250f;
                decayRate = 28.0f;
                noiseMix = 0.45f;
                break;
            case ImpactMaterial.Foliage:
                f0 = 180f; f1 = 440f;
                decayRate = 6.5f;
                noiseMix = 0.85f;
                break;
        }

        LastFundamentalFreq = f0;
        LastDecayTimeMs = sampleLength * 1000f / sampleRate;
        LastSynthesisFormula = $"{material} Modal Hit: F0={f0:F0}Hz F1={f1:F0}Hz Decay={decayRate:F1}";

        float p0 = 0f, p1 = 0f;
        for (int i = 0; i < sampleLength; i++)
        {
            float t = (float)i / sampleLength;
            float env = Mathf.Exp(-t * decayRate);

            p0 += 2.0f * Mathf.PI * f0 / sampleRate;
            p1 += 2.0f * Mathf.PI * f1 / sampleRate;

            float modalTones = Mathf.Sin(p0) * 0.7f + Mathf.Sin(p1) * 0.3f;
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
            float click = (i < sampleRate * 0.004f) ? (float)(rnd.NextDouble() * 2.0 - 1.0) * 1.8f : 0f;

            pcm[i] = ((modalTones * (1f - noiseMix) + noise * noiseMix) * env + click) * force * 0.9f;
        }

        PlaySynthesizedClip($"Impact_{material}", pcm, sampleRate);
    }

    private void PlaySynthesizedClip(string clipName, float[] data, int sampleRate)
    {
        TotalSynthesizedWaves++;
        LastSoundTriggered = clipName;

        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, sampleRate, false);
        clip.SetData(data, 0);
        audioSource.PlayOneShot(clip);
    }

    private float CalculateAlpha(float cutoffHz, int sRate)
    {
        float dt = 1.0f / sRate;
        float rc = 1.0f / (2.0f * Mathf.PI * cutoffHz);
        return dt / (rc + dt);
    }
}