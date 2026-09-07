using UnityEngine;

public class SoundstageHUD : MonoBehaviour
{
    public ProceduralSoundstage soundstage;
    public Camera playerCamera;

    private float[] rawBuffer = new float[128];
    private Texture2D crosshairTexture;
    private Texture2D solidTexture;

    void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, Color.white);
        crosshairTexture.Apply();

        solidTexture = new Texture2D(1, 1);
        solidTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.70f));
        solidTexture.Apply();
    }

    void OnGUI()
    {
        DrawCrosshair();

        // 1. Controls & Engine State Window (Top Left)
        DrawControlsPanel(new Rect(15, 15, 390, 295));

        // 2. Beginner-Friendly Telemetry & Live Data (Bottom Left)
        DrawComparisonTelemetry(new Rect(15, Screen.height - 235, 490, 220));

        // 3. Real-Time Hardware Oscilloscope (Top Right)
        DrawLiveOscilloscope(new Rect(Screen.width - 265, 15, 250, 120));
    }

    void DrawControlsPanel(Rect rect)
    {
        GUI.DrawTexture(rect, solidTexture);
        GUI.Box(rect, "<b>AUTONOMOUS SYNTHETIC SOUNDSTAGE</b>");

        if (soundstage.isAudioMuted)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(rect.x + 12, rect.y + 26, 360, 20), "STATUS: <b>AUDIO ENGINE MUTED [M]</b>");
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = soundstage.isComparisonMode ? Color.yellow : Color.cyan;
            string modeName = soundstage.isComparisonMode ? "STATIC PRE-RECORDED (.WAV) MODE" : "ZERO-WAV PROCEDURAL DSP ENGINE";
            GUI.Label(new Rect(rect.x + 12, rect.y + 26, 360, 20), $"ENGINE: <b>{modeName}</b>");
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 12, rect.y + 46, 360, 18), $"Current Season: <b>{soundstage.currentSeason}</b> [Tab]");
            GUI.Label(new Rect(rect.x + 12, rect.y + 64, 360, 18), $"Active Emotion: <b>{soundstage.currentEmotion}</b> [Keys 1-6]");
            GUI.Label(new Rect(rect.x + 12, rect.y + 82, 360, 18), $"Dynamic Ground Patch: <b>Patch #{((int)(Time.time * 0.5f) % 4) + 1} (Evolving every 2s)</b>");

            if (SoundstageRig.IsInCave)
            {
                GUI.color = new Color(0.4f, 1.0f, 0.4f);
                GUI.Label(new Rect(rect.x + 12, rect.y + 100, 360, 18), "Acoustic Environment: <b>CAVERN (3.2s Echo Active)</b>");
                GUI.color = Color.white;
            }
            else
            {
                GUI.Label(new Rect(rect.x + 12, rect.y + 100, 360, 18), $"Acoustic Environment: <b>Open Forest (Gaze: {soundstage.windCutoff:F0}Hz)</b>");
            }
        }

        GUI.color = Color.yellow;
        GUI.Label(new Rect(rect.x + 12, rect.y + 126, 360, 18), "<b>CONTROLS:</b>");
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 12, rect.y + 146, 360, 18), "[T] Toggle A/B Test (Compare vs Traditional .WAV)");
        GUI.Label(new Rect(rect.x + 12, rect.y + 164, 360, 18), "[Left Click / E] Strike Obstacles, Monster or River Rocks");
        GUI.Label(new Rect(rect.x + 12, rect.y + 182, 360, 18), "[1-5] Synthesize Emotions  |  [6] Neutral Silence");
        GUI.Label(new Rect(rect.x + 12, rect.y + 200, 360, 18), "[Tab] Cycle Seasons (Winter, Summer, Spring, Autumn)");
        GUI.Label(new Rect(rect.x + 12, rect.y + 218, 360, 18), "[M] Mute Soundstage");
        GUI.Label(new Rect(rect.x + 12, rect.y + 238, 360, 18), "• Walk to X:30, Z:25 to enter the Cavern & Monster Den");
        GUI.Label(new Rect(rect.x + 12, rect.y + 256, 360, 18), "• Walk into River for dynamic water vs ice footsteps");
    }

    void DrawComparisonTelemetry(Rect rect)
    {
        GUI.DrawTexture(rect, solidTexture);
        GUI.Box(rect, "<b>LIVE AUDIO ARCHITECTURE TELEMETRY</b>");

        bool isWav = soundstage.isComparisonMode;

        GUI.Label(new Rect(rect.x + 12, rect.y + 28, 160, 20), "Acoustic Origin:");
        GUI.color = isWav ? Color.yellow : Color.cyan;
        GUI.Label(new Rect(rect.x + 175, rect.y + 28, 305, 20), isWav ? "Pre-Recorded Static .WAV File" : "Real-Time DSP Mathematical Synthesis");
        GUI.color = Color.white;

        GUI.Label(new Rect(rect.x + 12, rect.y + 52, 160, 20), "Asset Disk & RAM Usage:");
        GUI.color = isWav ? new Color(1f, 0.4f, 0.4f) : new Color(0.3f, 1f, 0.4f);
        GUI.Label(new Rect(rect.x + 175, rect.y + 52, 305, 20), isWav ? "~18.4 MB (Uncompressed Samples)" : "<b>0.00 KB</b> (Zero Audio Files Stored)");
        GUI.color = Color.white;

        GUI.Label(new Rect(rect.x + 12, rect.y + 76, 160, 20), "Sample Uniqueness:");
        GUI.color = isWav ? new Color(1f, 0.5f, 0.5f) : new Color(0.3f, 1f, 0.4f);
        GUI.Label(new Rect(rect.x + 175, rect.y + 76, 305, 20), isWav ? "0% (Repeats exact same clip)" : "100% (Unique wave calculated per step)");
        GUI.color = Color.white;

        GUI.Label(new Rect(rect.x + 12, rect.y + 104, 160, 20), "Active DSP Formula:");
        GUI.color = Color.white;
        string formula = isWav ? "N/A (Static playback buffer)" : ProceduralFoley.LastSynthesisFormula;
        GUI.Label(new Rect(rect.x + 175, rect.y + 104, 305, 20), $"<i>{formula}</i>");

        GUI.Label(new Rect(rect.x + 12, rect.y + 128, 160, 20), "Fundamental Frequency:");
        GUI.Label(new Rect(rect.x + 175, rect.y + 128, 305, 20), isWav ? "Locked at 180 Hz" : $"{ProceduralFoley.LastFundamentalFreq:F1} Hz (Dynamic weight shift)");

        GUI.Label(new Rect(rect.x + 12, rect.y + 152, 160, 20), "Physical Decay Window:");
        GUI.Label(new Rect(rect.x + 175, rect.y + 152, 305, 20), isWav ? "Fixed clip duration" : $"{ProceduralFoley.LastDecayTimeMs:F0} ms (Surface resistance)");

        GUI.color = Color.yellow;
        GUI.Label(new Rect(rect.x + 12, rect.y + 184, 470, 20),
            isWav ? "MODE STATUS: Demonstrating canned, pre-recorded audio limitations."
                  : $"GENERATIVE ENGINE ACTIVE: <b>{ProceduralFoley.TotalSynthesizedWaves}</b> unique audio waves generated this session.");
        GUI.color = Color.white;
    }

    void DrawCrosshair()
    {
        if (playerCamera == null) return;

        float centerX = Screen.width / 2.0f;
        float centerY = Screen.height / 2.0f;
        float size = 12f;
        float thickness = 2f;
        float gap = 4f;

        Color crosshairColor = new Color(1f, 1f, 1f, 0.75f);
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3.5f))
        {
            string n = hit.collider.gameObject.name.ToLower();
            if (n.Contains("trunk") || n.Contains("log") || n.Contains("boulder") || n.Contains("rock") || n.Contains("croc") || n.Contains("monster") || n.Contains("cave") || n.Contains("bush") || n.Contains("canopy") || n.Contains("altar"))
            {
                crosshairColor = new Color(0.2f, 1.0f, 0.5f, 0.95f);
            }
        }

        Color originalColor = GUI.color;
        GUI.color = crosshairColor;
        GUI.DrawTexture(new Rect(centerX - thickness / 2.0f, centerY - gap - size, thickness, size), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX - thickness / 2.0f, centerY + gap, thickness, size), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX - gap - size, centerY - thickness / 2.0f, size, thickness), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX + gap, centerY - thickness / 2.0f, size, thickness), crosshairTexture);
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 1f, 2f, 2f), crosshairTexture);
        GUI.color = originalColor;
    }

    void DrawLiveOscilloscope(Rect rect)
    {
        GUI.DrawTexture(rect, solidTexture);
        GUI.Box(rect, "<b>HARDWARE OSCILLOSCOPE</b>");
        AudioListener.GetOutputData(rawBuffer, 0);

        for (int i = 0; i < rawBuffer.Length - 1; i++)
        {
            float x1 = rect.x + 10 + ((float)i / rawBuffer.Length) * 230;
            float x2 = rect.x + 10 + ((float)(i + 1) / rawBuffer.Length) * 230;
            float y1 = rect.y + 65 + (rawBuffer[i] * 42f);
            float y2 = rect.y + 65 + (rawBuffer[i + 1] * 42f);

            GUI.DrawTexture(new Rect(x1, y1, Mathf.Max(1f, x2 - x1), 2), crosshairTexture);
        }
    }
}