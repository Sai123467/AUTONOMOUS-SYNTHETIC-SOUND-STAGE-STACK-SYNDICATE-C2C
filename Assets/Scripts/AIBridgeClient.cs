using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class AIBridgeClient : MonoBehaviour
{
    public ProceduralSoundstage soundstage;
    public AudioReverbFilter reverb;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isRunning = false;
    private string pendingEmotion = "";
    private float targetWeather = 0.3f;

    [System.Serializable]
    private class AIState
    {
        public string emotion;
        public float weather;
        public float tension;
    }

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 8765);
            stream = client.GetStream();
            isRunning = true;
            receiveThread = new Thread(ReceiveDataLoop) { IsBackground = true };
            receiveThread.Start();
            Debug.Log("[AIBridgeClient] Connected to Python server.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AIBridgeClient] Could not connect to Python: " + ex.Message);
        }
    }

    void ReceiveDataLoop()
    {
        byte[] buffer = new byte[1024];
        while (isRunning && client != null && client.Connected)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string rawJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    AIState state = JsonUtility.FromJson<AIState>(rawJson);
                    if (state != null)
                    {
                        if (!string.IsNullOrEmpty(state.emotion)) pendingEmotion = state.emotion;
                        targetWeather = state.weather;
                    }
                }
            }
            catch { break; }
        }
    }

    void Update()
    {
        if (soundstage == null) return;

        if (!string.IsNullOrEmpty(pendingEmotion))
        {
            if (Enum.TryParse(pendingEmotion, true, out EmotionMode parsedEmotion))
            {
                soundstage.SetEmotion(parsedEmotion);
            }
            pendingEmotion = "";
        }

        soundstage.weatherIntensity = Mathf.Lerp(soundstage.weatherIntensity, targetWeather, Time.deltaTime * 2.0f);
    }

    void OnDestroy()
    {
        isRunning = false;
        if (stream != null) stream.Close();
        if (client != null) client.Close();
    }
}