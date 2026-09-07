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
        solidTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
        solidTexture.Apply();
    }

    void OnGUI()
    {
        DrawCrosshair();

        // 1. Controls & Engine State Window (Top Left)
        DrawControlsPanel(new Rect(15, 15, 395, 295));

        // 2. Telemetry, Inspector & Resynthesis Readout (Bottom Left)
        DrawComparisonTelemetry(new Rect(15, Screen.height - 245, 510, 230));

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
            GUI.Label(new Rect(rect.x + 12, rect.y + 26, 370, 20), "STATUS: <b>AUDIO ENGINE MUTED [M]</b>");
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = soundstage.isComparisonMode ? Color.yellow : Color.cyan;
            string modeName = soundstage.isComparisonMode ? "STATIC PRE-RECORDED (.WAV) MODE" : "ZERO-WAV PROCEDURAL DSP ENGINE";
            GUI.Label(new Rect(rect.x + 12, rect.y + 26, 370, 20), $"ENGINE: <b>{modeName}</b>");
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 12, rect.y + 46, 370, 18), $"Current Season: <b>{soundstage.currentSeason}</b> [Tab]");
            GUI.Label(new Rect(rect.x + 12, rect.y + 64, 370, 18), $"Active Emotion: <b>{soundstage.currentEmotion}</b> [Keys 1-6]");
            GUI.Label(new Rect(rect.x + 12, rect.y + 82, 370, 18), $"Dynamic Ground Patch: <b>Patch #{((int)(Time.time * 0.5f) % 4) + 1} (Evolving every 2s)</b>");

            // Environmental Acoustic Chamber Detection
            if (SoundstageRig.IsInCave)
            {
                GUI.color = new Color(0.4f, 1.0f, 0.4f);
                GUI.Label(new Rect(rect.x + 12, rect.y + 100, 370, 18), "Acoustic Environment: <b>CAVERN (4.0s Echo Active)</b>");
                GUI.color = Color.white;
            }
            else
            {
                GUI.Label(new Rect(rect.x + 12, rect.y + 100, 370, 18), $"Acoustic Environment: <b>Open Forest (Gaze: {soundstage.windCutoff:F0}Hz)</b>");
            }
        }

        // Controls Section
        GUI.color = Color.yellow;
        GUI.Label(new Rect(rect.x + 12, rect.y + 126, 370, 18), "<b>CONTROLS:</b>");
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 12, rect.y + 146, 370, 18), "[T] Toggle A/B Test (Compare vs Traditional .WAV)");
        GUI.Label(new Rect(rect.x + 12, rect.y + 164, 370, 18), "[Left Click / E] Strike Obstacles, Monster or River Rocks");
        GUI.Label(new Rect(rect.x + 12, rect.y + 182, 370, 18), "[1-5] Synthesize Emotions  |  [6] Neutral Silence");
        GUI.Label(new Rect(rect.x + 12, rect.y + 200, 370, 18), "[Tab] Cycle Seasons (Winter, Summer, Spring, Autumn)");
        GUI.Label(new Rect(rect.x + 12, rect.y + 218, 370, 18), "[M] Mute Soundstage");
        GUI.Label(new Rect(rect.x + 12, rect.y + 238, 370, 18), "• Walk to X:30, Z:25 to enter Cavern & Monster Den");
        GUI.Label(new Rect(rect.x + 12, rect.y + 256, 370, 18), "• Walk into River for dynamic water vs ice footsteps");
    }

    void DrawComparisonTelemetry(Rect rect)
    {
        GUI.DrawTexture(rect, solidTexture);
        GUI.Box(rect, "<b>LIVE AUDIO ARCHITECTURE TELEMETRY</b>");

        bool isWav = soundstage.isComparisonMode;

        // Metric 1: Source Origin
        GUI.Label(new Rect(rect.x + 12, rect.y + 26, 160, 18), "Acoustic Origin:");
        GUI.color = isWav ? Color.yellow : Color.cyan;
        GUI.Label(new Rect(rect.x + 175, rect.y + 26, 325, 18), isWav ? "Pre-Recorded Static .WAV File" : "Real-Time DSP Mathematical Synthesis");
        GUI.color = Color.white;

        // Metric 2: Asset Memory Storage
        GUI.Label(new Rect(rect.x + 12, rect.y + 46, 160, 18), "Asset Disk & RAM Usage:");
        GUI.color = isWav ? new Color(1f, 0.4f, 0.4f) : new Color(0.3f, 1f, 0.4f);
        GUI.Label(new Rect(rect.x + 175, rect.y + 46, 325, 18), isWav ? "~18.4 MB (Uncompressed Samples)" : "<b>0.00 KB</b> (Zero Audio Files Stored)");
        GUI.color = Color.white;

        // Metric 3: Waveform Uniqueness (Entropy)
        GUI.Label(new Rect(rect.x + 12, rect.y + 66, 160, 18), "Sample Uniqueness:");
        GUI.color = isWav ? new Color(1f, 0.5f, 0.5f) : new Color(0.3f, 1f, 0.4f);
        GUI.Label(new Rect(rect.x + 175, rect.y + 66, 325, 18), isWav ? "0% (Repeats exact same clip)" : "100% (Unique wave calculated per step)");
        GUI.color = Color.white;

        // Metric 4: Active Synthesis Equation & Physics
        GUI.Label(new Rect(rect.x + 12, rect.y + 88, 160, 18), "Active DSP Formula:");
        GUI.color = Color.white;
        string formula = isWav ? "N/A (Static playback buffer)" : ProceduralFoley.LastSynthesisFormula;
        GUI.Label(new Rect(rect.x + 175, rect.y + 88, 325, 18), $"<i>{formula}</i>");

        // Metric 5: Live Physical Parameters
        GUI.Label(new Rect(rect.x + 12, rect.y + 108, 160, 18), "Fundamental Frequency:");
        GUI.Label(new Rect(rect.x + 175, rect.y + 108, 325, 18), isWav ? "Locked at 180 Hz" : $"{ProceduralFoley.LastFundamentalFreq:F1} Hz (Dynamic weight shift)");

        GUI.Label(new Rect(rect.x + 12, rect.y + 128, 160, 18), "Physical Decay Window:");
        GUI.Label(new Rect(rect.x + 175, rect.y + 128, 325, 18), isWav ? "Fixed clip duration" : $"{ProceduralFoley.LastDecayTimeMs:F0} ms (Surface resistance)");

        // Metric 6: Diegetic Harmonization Readout
        GUI.Label(new Rect(rect.x + 12, rect.y + 148, 160, 18), "Harmonic Resynthesis:");
        GUI.color = new Color(0.6f, 0.9f, 1f);
        GUI.Label(new Rect(rect.x + 175, rect.y + 148, 325, 18), isWav ? "Disabled (Decoupled audio)" : "Active (Foley transients modulate BGM)");
        GUI.color = Color.white;

        // Total Synthesized Waveforms counter
        GUI.color = Color.yellow;
        GUI.Label(new Rect(rect.x + 12, rect.y + 178, 485, 18),
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
        if (Physics.Raycast(ray, out RaycastHit hit, 4.0f))
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