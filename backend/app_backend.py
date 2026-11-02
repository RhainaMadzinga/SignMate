"""
app_backend.py

Small API wrapper for the SignMate backend.

Purpose:
- Allows the SwiftUI app to send English text to the Python backend.
- Returns BSL glosses, dataset JSON files, missing signs, and avatar animation IDs.
- This acts as the bridge between the iOS app and the backend functionality.

Run with:
    python3 src/app_backend.py
"""

from flask import Flask, request, jsonify
from context_recognition import translate_text_to_bsl_gloss

app = Flask(__name__)


@app.route("/", methods=["GET"])
def home():
    return jsonify({
        "message": "SignMate backend is running",
        "status": "ok"
    })


@app.route("/translate", methods=["POST"])
def translate():
    data = request.get_json()

    if not data or "text" not in data:
        return jsonify({
            "error": "Missing text input",
            "example": {
                "text": "I want water please"
            }
        }), 400

    input_text = data["text"]

    result = translate_text_to_bsl_gloss(input_text)

    return jsonify(result)


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000, debug=False)