using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SoundstageRig : MonoBehaviour
{
    public ProceduralSoundstage soundstage;
    public ProceduralFoley foley;
    public AudioReverbFilter reverb;
    public Camera playerCamera;

    public static bool IsInCave = false;
    public Vector3 caveCenter = new Vector3(30f, 0f, 25f);
    public float caveRadius = 15.0f; // Extended threshold for smooth cavern entry

    private CharacterController controller;
    private float rotX = 0f;
    private float stepCycle = 0f;

    private Vector3 currentInputMoveDir = Vector3.zero;
    private float collisionSoundCooldown = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        // Auto-locate reverb filter on this object or camera
        if (reverb == null) reverb = GetComponent<AudioReverbFilter>();
        if (reverb == null && playerCamera != null) reverb = playerCamera.GetComponent<AudioReverbFilter>();
        if (reverb == null && Camera.main != null) reverb = Camera.main.GetComponent<AudioReverbFilter>();

        // If none exists, dynamically attach one to the active player camera
        if (reverb == null && playerCamera != null)
        {
            reverb = playerCamera.gameObject.AddComponent<AudioReverbFilter>();
        }

        if (reverb != null)
        {
            reverb.enabled = true;
            reverb.reverbPreset = AudioReverbPreset.Off; // Default off outdoors
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (collisionSoundCooldown > 0f) collisionSoundCooldown -= Time.deltaTime;

        HandleMouseLook();
        HandleMovementAndFoley();
        HandleSeasonalGazeAcoustics();
        HandleCaveAcoustics();
        HandleHotkeys();
        HandleObstacleStrike();
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null || playerCamera == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.15f;
        rotX = Mathf.Clamp(rotX - mouseDelta.y, -85f, 85f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleMovementAndFoley()
    {
        float h = 0f, v = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
        }

        currentInputMoveDir = (transform.right * h + transform.forward * v).normalized;
        controller.SimpleMove(currentInputMoveDir * 4.5f);

        if (controller.isGrounded && controller.velocity.magnitude > 0.4f)
        {
            stepCycle += Time.deltaTime * controller.velocity.magnitude;
            if (stepCycle > 1.9f)
            {
                if (foley != null && soundstage != null && !soundstage.isAudioMuted)
                {
                    // Check if player is standing inside the river boundary (|x| <= 3.8m)
                    bool isInsideRiver = Mathf.Abs(transform.position.x) <= 3.8f;
                    if (isInsideRiver)
                    {
                        // Passes the current season to synthesize specific river acoustics (winter ice vs. seasonal splashes)
                        foley.TriggerSeasonalWaterStep(soundstage.currentSeason);
                    }
                    else
                    {
                        foley.TriggerSeasonalStep(soundstage.currentSeason);
                    }
                }
                stepCycle = 0.0f;
            }
        }
    }

    // Dynamic, deep acoustic reverberation chamber
    void HandleCaveAcoustics()
    {
        float distToCave = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(caveCenter.x, 0f, caveCenter.z)
        );
        IsInCave = (distToCave <= caveRadius);

        if (reverb != null)
        {
            if (IsInCave)
            {
                // Prominent cavern stone echo reflections
                reverb.reverbPreset = AudioReverbPreset.Cave;
                reverb.dryLevel = 0f;
                reverb.room = 0f;
                reverb.roomHF = -300f;
                reverb.decayTime = 4.0f; // 4-second stone decay tail
                reverb.reflectionsLevel = 400f;
                reverb.reverbLevel = 1000f;
            }
            else
            {
                reverb.reverbPreset = AudioReverbPreset.Off;
            }
        }
    }

    void HandleSeasonalGazeAcoustics()
    {
        if (playerCamera == null || soundstage == null) return;

        float pitchY = playerCamera.transform.forward.y;
        float pitch01 = Mathf.Clamp01((pitchY + 1.0f) * 0.5f);
        soundstage.windCutoff = Mathf.Lerp(100f, 2400f, pitch01);
    }

    void HandleObstacleStrike()
    {
        if (foley == null || playerCamera == null || soundstage == null || soundstage.isAudioMuted) return;

        bool isAttacking = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isAttacking = true;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) isAttacking = true;

        if (isAttacking)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 4.0f))
            {
                string hitName = hit.collider.gameObject.name.ToLower();

                if (hitName.Contains("trunk") || hitName.Contains("log"))
                {
                    foley.TriggerImpact(ImpactMaterial.Wood, 1.0f);
                }
                else if (hitName.Contains("boulder") || hitName.Contains("rock") || hitName.Contains("croc") || hitName.Contains("cave") || hitName.Contains("monster") || hitName.Contains("altar"))
                {
                    foley.TriggerImpact(ImpactMaterial.Rock, 1.3f);
                }
                else if (hitName.Contains("canopy") || hitName.Contains("bush"))
                {
                    foley.TriggerImpact(ImpactMaterial.Foliage, 0.7f);
                }
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (foley == null || soundstage == null || soundstage.isAudioMuted) return;
        if (hit.normal.y > 0.7f) return;

        if (currentInputMoveDir.magnitude > 0.1f && collisionSoundCooldown <= 0f)
        {
            float impactAngle = Vector3.Dot(currentInputMoveDir, -hit.normal);
            if (impactAngle > 0.25f)
            {
                string hitName = hit.collider.gameObject.name.ToLower();

                if (hitName.Contains("trunk") || hitName.Contains("log"))
                {
                    foley.TriggerImpact(ImpactMaterial.Wood, 0.65f);
                    collisionSoundCooldown = 0.35f;
                }
                else if (hitName.Contains("boulder") || hitName.Contains("rock") || hitName.Contains("croc") || hitName.Contains("cave") || hitName.Contains("monster"))
                {
                    foley.TriggerImpact(ImpactMaterial.Rock, 0.75f);
                    collisionSoundCooldown = 0.35f;
                }
                else if (hitName.Contains("canopy") || hitName.Contains("bush"))
                {
                    foley.TriggerImpact(ImpactMaterial.Foliage, 0.45f);
                    collisionSoundCooldown = 0.35f;
                }
            }
        }
    }

    void HandleHotkeys()
    {
        if (Keyboard.current == null || soundstage == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) soundstage.SetEmotion(EmotionMode.Happy);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) soundstage.SetEmotion(EmotionMode.Panic);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) soundstage.SetEmotion(EmotionMode.Anger);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) soundstage.SetEmotion(EmotionMode.Lust);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) soundstage.SetEmotion(EmotionMode.Disgust);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) soundstage.SetEmotion(EmotionMode.Neutral);

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            soundstage.isAudioMuted = !soundstage.isAudioMuted;
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            soundstage.isComparisonMode = !soundstage.isComparisonMode;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            soundstage.CycleNextSeason();
        }
    }
}