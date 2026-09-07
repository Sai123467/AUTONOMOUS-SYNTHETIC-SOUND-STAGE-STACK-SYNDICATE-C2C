using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralFoleySynthesizer : MonoBehaviour
{
    public ProceduralSoundstage soundstage;

    [Header("Synthesizer Output Tuning")]
    [Range(0f, 1.5f)] public float masterSynthVolume = 0.75f;
    [Range(0.1f, 10f)] public float droneResonanceDecay = 4.5f;

    private float sampleRate;
    private AudioSource audioSource;

    // Resonator State Variables (Thread-Safe)
    private float resState1 = 0f;
    private float resState2 = 0f;
    private float impulseEnergy = 0f;

    // Tuned chord roots
    private float targetRootFreq = 65.41f; // C2
    private float currentCarrierPhase = 0f;

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.spatialBlend = 0.0f; // 2D synth pad
        audioSource.volume = 1.0f;

        // Force Unity to keep the DSP pipeline active by assigning a dummy silent clip
        if (audioSource.clip == null)
        {
            AudioClip silentLoop = AudioClip.Create("SilentDSPCarrier", 44100, 1, (int)sampleRate, false);
            audioSource.clip = silentLoop;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Start()
    {
        if (soundstage == null)
        {
            soundstage = FindAnyObjectByType<ProceduralSoundstage>();
        }
    }

    void Update()
    {
        if (soundstage == null) return;

        // Ensure AudioSource doesn't sleep
        if (!audioSource.isPlaying) audioSource.Play();

        // Tune target musical scale to emotion
        switch (soundstage.currentEmotion)
        {
            case EmotionMode.Panic:
                targetRootFreq = 46.25f; // Dissonant Tritone (F#1)
                break;
            case EmotionMode.Happy:
                targetRootFreq = 65.41f; // C2
                break;
            case EmotionMode.Anger:
                targetRootFreq = 38.89f; // Low Eb Sub-Bass
                break;
            case EmotionMode.Neutral:
            default:
                targetRootFreq = 55.00f; // A1
                break;
        }
    }

    // Called from ProceduralFoley when any step, splash, or strike occurs
    public void InjectFoleyImpulse(float[] pcmData)
    {
        // Calculate the physical energy of the impact transient
        float peak = 0f;
        for (int i = 0; i < Mathf.Min(pcmData.Length, 512); i++)
        {
            float absVal = Mathf.Abs(pcmData[i]);
            if (absVal > peak) peak = absVal;
        }

        // Excite the resonator
        impulseEnergy = Mathf.Clamp(impulseEnergy + peak * 1.5f, 0f, 2.5f);
    }

    // High-priority Audio DSP Thread
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (soundstage != null && soundstage.isAudioMuted)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        float dt = 1.0f / sampleRate;
        // 2-Pole Resonant Bandpass calculation (Bi-quad)
        float omega = 2.0f * Mathf.PI * targetRootFreq / sampleRate;
        float alpha = Mathf.Sin(omega) * 0.08f; // Narrow resonance bandwidth

        for (int i = 0; i < data.Length; i += channels)
        {
            // Exponential decay of the foley excitation impulse
            impulseEnergy *= 0.99985f;

            // Musical carrier drone
            currentCarrierPhase += 2.0f * Mathf.PI * targetRootFreq / sampleRate;
            if (currentCarrierPhase > 2.0f * Mathf.PI) currentCarrierPhase -= 2.0f * Mathf.PI;
            float carrierTone = Mathf.Sin(currentCarrierPhase);

            // Resonator excited by player movement/foley impulses
            float excitationSignal = (carrierTone * 0.2f) + (impulseEnergy * carrierTone * 0.8f);

            resState1 += alpha * (excitationSignal - resState1);
            resState2 += alpha * (resState1 - resState2);

            // Resynthesized output tone
            float synthOutput = resState2 * masterSynthVolume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] += synthOutput;
            }
        }
    }
}