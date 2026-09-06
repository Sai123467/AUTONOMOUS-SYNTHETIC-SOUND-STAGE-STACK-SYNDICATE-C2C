using UnityEngine;

public class SoundstageHUD : MonoBehaviour
{
    public ProceduralSoundstage soundstage;
    public Camera playerCamera;

    private float[] rawBuffer = new float[128];
    private Texture2D crosshairTexture;

    void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        // Create a 1x1 white texture for clean line rendering
        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, Color.white);
        crosshairTexture.Apply();
    }

    void OnGUI()
    {
        // 1. Draw Centered Dynamic Reticle / Crosshair
        DrawCrosshair();

        // 2. Main Telemetry Window
        GUI.Box(new Rect(15, 15, 340, 275), "<b>AUTONOMOUS SYNTHETIC SOUNDSTAGE</b>");

        if (soundstage.isAudioMuted)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(25, 45, 310, 22), "STATUS: <b>AUDIO ENGINE STOPPED / MUTED [M]</b>");
            GUI.color = Color.white;
        }
        else if (soundstage.isComparisonMode)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(25, 45, 310, 22), "MODE: <b>TRADITIONAL PRE-RECORDED LOOP (.WAV)</b>");
            GUI.color = Color.white;
            GUI.Label(new Rect(25, 68, 310, 20), "• Flat repetition | Static pitch | No gaze response");
        }
        else
        {
            GUI.color = Color.cyan;
            GUI.Label(new Rect(25, 45, 310, 22), "MODE: <b>REAL-TIME PROCEDURAL DSP (ZERO .WAV)</b>");
            GUI.color = Color.white;
            GUI.Label(new Rect(25, 68, 310, 20), $"Current Season: <b>{soundstage.currentSeason}</b> [Tab]");
            GUI.Label(new Rect(25, 90, 310, 20), $"Active Emotion: <b>{soundstage.currentEmotion}</b> [Keys 1-7]");
            GUI.Label(new Rect(25, 112, 310, 20), $"Live Gaze Air Cutoff: <b>{soundstage.windCutoff:F0} Hz</b>");
        }

        GUI.color = Color.yellow;
        GUI.Label(new Rect(25, 135, 310, 20), "<b>JUDGE EXPERIMENTATION CONTROLS:</b>");
        GUI.color = Color.white;
        GUI.Label(new Rect(25, 155, 310, 18), "[Left Click / E] Hit Aimed Tree Trunk, Log, or Rock");
        GUI.Label(new Rect(25, 175, 310, 18), "[1-6] Emotions  |  [7] Neutral Silence");
        GUI.Label(new Rect(25, 195, 310, 18), "[Tab] Cycle Seasons (Winter, Summer, Spring, Fall)");
        GUI.Label(new Rect(25, 215, 310, 18), "[T] Toggle A/B Test  |  [M] Mute Engine");

        // 3. Real-Time Hardware Oscilloscope
        DrawLiveOscilloscope(new Rect(Screen.width - 250, 15, 235, 110));
    }

    void DrawCrosshair()
    {
        if (playerCamera == null) return;

        float centerX = Screen.width / 2.0f;
        float centerY = Screen.height / 2.0f;

        float size = 12f;      // Crosshair arm length
        float thickness = 2f; // Crosshair stroke thickness
        float gap = 4f;       // Center open gap

        // Check if aiming at an interactable obstacle within striking distance (3.5m)
        Color crosshairColor = new Color(1f, 1f, 1f, 0.75f);
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3.5f))
        {
            string n = hit.collider.gameObject.name.ToLower();
            if (n.Contains("trunk") || n.Contains("log") || n.Contains("boulder") || n.Contains("rock") || n.Contains("bush") || n.Contains("canopy"))
            {
                crosshairColor = new Color(0.2f, 1.0f, 0.5f, 0.95f); // Green/Cyan highlight when in range
            }
        }

        Color originalColor = GUI.color;
        GUI.color = crosshairColor;

        // Top line
        GUI.DrawTexture(new Rect(centerX - thickness / 2.0f, centerY - gap - size, thickness, size), crosshairTexture);
        // Bottom line
        GUI.DrawTexture(new Rect(centerX - thickness / 2.0f, centerY + gap, thickness, size), crosshairTexture);
        // Left line
        GUI.DrawTexture(new Rect(centerX - gap - size, centerY - thickness / 2.0f, size, thickness), crosshairTexture);
        // Right line
        GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness / 2.0f, size, thickness), crosshairTexture);

        // Center dot
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 1f, 2f, 2f), crosshairTexture);

        GUI.color = originalColor;
    }

    void DrawLiveOscilloscope(Rect rect)
    {
        GUI.Box(rect, "<b>DSP HARDWARE OSCILLOSCOPE</b>");
        AudioListener.GetOutputData(rawBuffer, 0);

        for (int i = 0; i < rawBuffer.Length - 1; i++)
        {
            float x1 = rect.x + 10 + ((float)i / rawBuffer.Length) * 215;
            float x2 = rect.x + 10 + ((float)(i + 1) / rawBuffer.Length) * 215;
            float y1 = rect.y + 60 + (rawBuffer[i] * 40f);
            float y2 = rect.y + 60 + (rawBuffer[i + 1] * 40f);

            GUI.DrawTexture(new Rect(x1, y1, Mathf.Max(1f, x2 - x1), 2), crosshairTexture);
        }
    }
}