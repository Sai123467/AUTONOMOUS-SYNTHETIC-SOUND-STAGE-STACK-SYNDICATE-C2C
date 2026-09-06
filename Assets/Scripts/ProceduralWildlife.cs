using UnityEngine;

public enum AnimalSpecies { ArcticWolf, SummerCicada, Songbird, WoodlandElk, RiverFish, RiverCrocodile }

[RequireComponent(typeof(AudioSource))]
public class ProceduralWildlife : MonoBehaviour
{
    [Header("Identity & Tuning")]
    public AnimalSpecies species;
    public Season associatedSeason;
    public float triggerDistance = 6.0f;

    private Transform playerTransform;
    private AudioSource audioSource;
    private Vector3 targetWanderPoint;
    private float wanderTimer = 0f;
    private float vocalCooldown = 0f;
    private float moveSpeed = 2.2f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;
        audioSource.minDistance = 2.0f;
        audioSource.maxDistance = 25.0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) playerObj = Camera.main != null ? Camera.main.gameObject : null;
        if (playerObj != null) playerTransform = playerObj.transform;

        if (species == AnimalSpecies.RiverFish) moveSpeed = 3.5f;
        else if (species == AnimalSpecies.RiverCrocodile) moveSpeed = 1.2f;

        PickNewWanderPoint();
    }

    void Update()
    {
        if (vocalCooldown > 0f) vocalCooldown -= Time.deltaTime;

        HandleWandering();
        CheckPlayerProximity();
    }

    void HandleWandering()
    {
        if (species == AnimalSpecies.SummerCicada) return;

        wanderTimer += Time.deltaTime;
        if (wanderTimer > 5.0f || Vector3.Distance(transform.position, targetWanderPoint) < 1.0f)
        {
            PickNewWanderPoint();
            wanderTimer = 0f;
        }

        Vector3 moveDir = (targetWanderPoint - transform.position).normalized;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 3.5f);
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            // Fish subtle swimming wiggle
            if (species == AnimalSpecies.RiverFish)
            {
                transform.position += transform.right * Mathf.Sin(Time.time * 8.0f) * 0.02f;
            }
        }
    }

    void PickNewWanderPoint()
    {
        // Aquatic creatures stay constrained within river bounds (X between -2.8 and +2.8)
        if (species == AnimalSpecies.RiverFish || species == AnimalSpecies.RiverCrocodile)
        {
            float rx = Random.Range(-2.6f, 2.6f);
            float rz = Random.Range(-42f, 42f);
            targetWanderPoint = new Vector3(rx, transform.position.y, rz);
        }
        else
        {
            float rx = Random.Range(-35f, 35f);
            float rz = Random.Range(-35f, 35f);
            targetWanderPoint = new Vector3(rx, transform.position.y, rz);
        }
    }

    void CheckPlayerProximity()
    {
        if (playerTransform == null || vocalCooldown > 0f) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= triggerDistance)
        {
            SynthesizeAndPlayCall();
            vocalCooldown = Random.Range(3.0f, 5.5f);
        }
    }

    public void SynthesizeAndPlayCall()
    {
        int sampleRate = 44100;
        int length = 0;
        float[] pcm = null;

        switch (species)
        {
            case AnimalSpecies.ArcticWolf:
                length = (int)(sampleRate * 2.2f);
                pcm = SynthesizeWolfHowl(length, sampleRate);
                break;
            case AnimalSpecies.SummerCicada:
                length = (int)(sampleRate * 1.5f);
                pcm = SynthesizeCicadaBuzz(length, sampleRate);
                break;
            case AnimalSpecies.Songbird:
                length = (int)(sampleRate * 0.8f);
                pcm = SynthesizeBirdChirp(length, sampleRate);
                break;
            case AnimalSpecies.WoodlandElk:
                length = (int)(sampleRate * 1.8f);
                pcm = SynthesizeElkBugle(length, sampleRate);
                break;
            case AnimalSpecies.RiverFish:
                length = (int)(sampleRate * 0.35f); // Quick tail splash
                pcm = SynthesizeFishSplash(length, sampleRate);
                break;
            case AnimalSpecies.RiverCrocodile:
                length = (int)(sampleRate * 1.6f); // Low guttural growl / hiss
                pcm = SynthesizeCrocodileGrowl(length, sampleRate);
                break;
        }

        if (pcm != null && length > 0)
        {
            AudioClip clip = AudioClip.Create("WildlifeCall", length, 1, sampleRate, false);
            clip.SetData(pcm, 0);
            audioSource.PlayOneShot(clip);
        }
    }

    // --- PROCEDURAL DSP VOCALIZATIONS ---

    private float[] SynthesizeFishSplash(int length, int sRate)
    {
        float[] data = new float[length];
        System.Random rnd = new System.Random();
        float phase = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float env = Mathf.Exp(-t * 9.0f);
            float freq = Mathf.Lerp(620f, 210f, t);
            phase += 2.0f * Mathf.PI * freq / sRate;

            float bubble = Mathf.Sin(phase);
            float waterFriction = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.35f;
            data[i] = (bubble + waterFriction) * env * 0.85f;
        }
        return data;
    }

    private float[] SynthesizeCrocodileGrowl(int length, int sRate)
    {
        float[] data = new float[length];
        System.Random rnd = new System.Random();
        float phase = 0f;
        float amPhase = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float env = Mathf.Sin(t * Mathf.PI);

            // Sub-bass throat frequency (55 Hz to 78 Hz)
            float freq = Mathf.Lerp(55f, 75f, Mathf.Sin(t * 8f) * 0.5f + 0.5f);
            phase += 2.0f * Mathf.PI * freq / sRate;
            amPhase += 2.0f * Mathf.PI * 24.0f / sRate; // Tremolo flutter

            float sub = Mathf.Sin(phase);
            float grit = (Mathf.Sin(amPhase) + 1.0f) * 0.5f;
            float hiss = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.25f;

            // Drive into distortion
            float mixed = Mathf.Clamp((sub * grit + hiss) * 1.6f, -0.8f, 0.8f);
            data[i] = mixed * env * 0.95f;
        }
        return data;
    }

    private float[] SynthesizeWolfHowl(int length, int sRate)
    {
        float[] data = new float[length];
        float phase = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float env = Mathf.Sin(t * Mathf.PI);
            float freq = (t < 0.35f) ? Mathf.Lerp(200f, 480f, t / 0.35f) : Mathf.Lerp(480f, 240f, (t - 0.35f) / 0.65f);
            phase += 2.0f * Mathf.PI * freq / sRate;

            float sine = Mathf.Sin(phase);
            float overtone = Mathf.Sin(phase * 2.0f) * 0.25f;
            data[i] = (sine + overtone) * env * 0.85f;
        }
        return data;
    }

    private float[] SynthesizeCicadaBuzz(int length, int sRate)
    {
        float[] data = new float[length];
        float carrierPhase = 0f;
        float amPhase = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float env = Mathf.Sin(t * Mathf.PI);
            carrierPhase += 2.0f * Mathf.PI * 4600.0f / sRate;
            amPhase += 2.0f * Mathf.PI * 38.0f / sRate;

            float carrier = Mathf.Sin(carrierPhase);
            float mod = (Mathf.Sin(amPhase) + 1.0f) * 0.5f;
            data[i] = carrier * mod * env * 0.55f;
        }
        return data;
    }

    private float[] SynthesizeBirdChirp(int length, int sRate)
    {
        float[] data = new float[length];
        float phase = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float env = (1.0f - t);
            float freq = Mathf.Sin(t * 40f) * 400f + Mathf.Lerp(3400f, 2100f, t);
            phase += 2.0f * Mathf.PI * freq / sRate;

            data[i] = Mathf.Sin(phase) * env * 0.7f;
        }
        return data;
    }

    private float[] SynthesizeElkBugle(int length, int sRate)
    {
        float[] data = new float[length];
        float phase = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float env = Mathf.Sin(t * Mathf.PI);
            float freq = (t < 0.4f) ? Mathf.Lerp(120f, 360f, t / 0.4f) : Mathf.Lerp(360f, 140f, (t - 0.4f) / 0.6f);
            phase += 2.0f * Mathf.PI * freq / sRate;

            float fundamental = Mathf.Sin(phase);
            float h3 = Mathf.Sin(phase * 3f) * 0.35f;
            float h5 = Mathf.Sin(phase * 5f) * 0.15f;

            data[i] = (fundamental + h3 + h5) * env * 0.9f;
        }
        return data;
    }
}