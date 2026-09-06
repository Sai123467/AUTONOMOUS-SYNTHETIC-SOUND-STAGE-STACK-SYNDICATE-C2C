import socket
import json
import threading
from vaderSentiment.vaderSentiment import SentimentIntensityAnalyzer

HOST = '127.0.0.1'
PORT = 8765

analyzer = SentimentIntensityAnalyzer()

def parse_emotion_to_code(text):
    lower = text.lower()

    # 1. Direct Keyword Matching
    if any(w in lower for w in ["neutral", "calm", "clear", "stop", "reset", "normal"]):
        return "Neutral"
    if any(w in lower for w in ["rage", "angry", "hate", "kill", "furious", "destroy", "mad"]):
        return "Anger"
    if any(w in lower for w in ["lust", "love", "desire", "passion", "sensual", "warmth", "romantic"]):
        return "Lust"
    if any(w in lower for w in ["gross", "disgust", "sick", "nauseous", "vomit", "slimy", "awful"]):
        return "Disgust"
    if any(w in lower for w in ["run", "monster", "scared", "fear", "panic", "help", "terror"]):
        return "Panic"
    if any(w in lower for w in ["storm", "thunder", "rain", "hurricane", "wind", "cyclone"]):
        return "Storm"
    if any(w in lower for w in ["happy", "joy", "cheerful", "peace", "glad", "wonderful", "excited"]):
        return "Happy"

    # 2. NLP Sentiment Fallback (VADER)
    scores = analyzer.polarity_scores(text)
    compound = scores['compound']

    if compound >= 0.35:
        return "Happy"
    elif compound <= -0.4:
        return "Panic"
    else:
        return "Neutral"

def handle_client(conn, addr):
    print(f"[AI Bridge] Connected to Unity at {addr}")
    try:
        while True:
            print("\nEnter any text (e.g. 'I am angry', 'I feel desire', 'That is gross', 'reset'):")
            user_input = input("Input > ").strip()
            if not user_input:
                continue

            detected_emotion = parse_emotion_to_code(user_input)
            print(f">> Detected Emotion: {detected_emotion}")

            payload = {
                "emotion": detected_emotion,
                "weather": 0.8 if detected_emotion == "Storm" else 0.3
            }
            conn.sendall((json.dumps(payload) + "\n").encode('utf-8'))
    except (ConnectionResetError, BrokenPipeError):
        print("[AI Bridge] Unity disconnected.")
    finally:
        conn.close()

def main():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((HOST, PORT))
    server.listen(1)
    print(f"[AI Bridge] Running on {HOST}:{PORT}. Ready for judge inputs.")

    while True:
        conn, addr = server.accept()
        threading.Thread(target=handle_client, args=(conn, addr), daemon=True).start()

if __name__ == "__main__":
    main()