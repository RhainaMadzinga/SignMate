"""
context_recognition.py

SignMate classical NLP backend.

Purpose:
1. Take English text from the Text Translate View.
2. Convert it into simplified BSL gloss order.
3. Choose the correct sign variation for ambiguous words.
4. Connect each final gloss to the matching dataset JSON file.
5. Return an avatar-ready sequence.

Example:
    "I will go home"
becomes:
    ["now", "i", "home", "go"]

This file should sit in src/ beside bsl_gloss_rules.py.
"""

from __future__ import annotations

import json
import re
from pathlib import Path
from typing import Dict, List, Optional, Any

from bsl_gloss_rules import (
    STOP_WORDS,
    ENGLISH_TO_GLOSS,
    PHRASE_TO_GLOSS,
    FUTURE_WORDS,
    PAST_WORDS,
    TIME_GLOSSES,
    SUBJECT_GLOSSES,
    VERB_GLOSSES,
    QUESTION_GLOSSES,
    VARIATION_RULES,
    DEFAULT_VARIATIONS,
)

from dataset_loader import build_animation_sequence



DEFAULT_DATASET_JSON_DIR = Path("data/signmate_dataset/processed/landmarks")
# needs to be the path for the unity .json files to connect to, not the processed dataset folder


def normalise_label(label: str) -> str:
    """
    Converts labels into the same style as dataset filenames.

    Example:
        "Thank You" -> "thank_you"
    """
    return label.strip().lower().replace(" ", "_").replace("-", "_")


def tokenize_text(text: str) -> List[str]:
    """
    Classical NLP preprocessing:
    - lowercases text
    - removes punctuation
    - splits into tokens

    Example:
        "I will go home" -> ["i", "will", "go", "home"]
    """
    cleaned = text.lower()
    cleaned = cleaned.replace("going to", "going_to")
    cleaned = re.sub(r"[^a-zA-Z0-9_'\s]", " ", cleaned)
    tokens = [token.strip() for token in cleaned.split() if token.strip()]
    return tokens


def apply_phrase_rules(tokens: List[str]) -> List[str]:
    """
    Replaces known English phrases with one dataset gloss.

    Example:
        ["thank", "you"] -> ["thank_you"]
    """
    output: List[str] = []
    index = 0

    while index < len(tokens):
        matched = False

        # Check 3-word phrases, then 2-word phrases.
        for phrase_length in (3, 2):
            phrase_tokens = tokens[index:index + phrase_length]
            phrase = " ".join(phrase_tokens)

            if phrase in PHRASE_TO_GLOSS:
                output.extend(PHRASE_TO_GLOSS[phrase])
                index += phrase_length
                matched = True
                break

        if not matched:
            output.append(tokens[index])
            index += 1

    return output


def english_tokens_to_glosses(tokens: List[str]) -> List[str]:
    """
    Converts English tokens into broad gloss labels.
    This does not yet fix BSL ordering or ambiguous variations.
    """
    glosses: List[str] = []

    for token in tokens:
        if token in STOP_WORDS:
            continue

        # Future tense marker:
        # "will" does not need a direct sign in this simplified project.
        # It tells the system to add "now" or "future" depending on your design.
        if token in FUTURE_WORDS:
            if "now" not in glosses:
                glosses.insert(0, "now")
            continue

        # Simple past handling.
        if token in PAST_WORDS and "past" not in glosses:
            glosses.insert(0, "past")
            continue

        gloss = ENGLISH_TO_GLOSS.get(token, normalise_label(token))
        glosses.append(gloss)

    return glosses


def reorder_to_bsl_gloss(glosses: List[str]) -> List[str]:
    """
    Reorders a simple English-style sequence into a simplified BSL gloss order.

    Rule used:
        TIME + SUBJECT + OBJECT/PLACE + VERB + POLITE + QUESTION

    Example:
        ["i", "now", "go", "home"] -> ["now", "i", "home", "go"]
        ["i", "want", "water", "please"] -> ["i", "water", "want", "please"]
    """
    time = []
    subject = []
    verb = []
    polite = []
    question = []
    other = []

    polite_glosses = {"please", "thank_you", "sorry", "sorry_oops"}

    for gloss in glosses:
        if gloss in TIME_GLOSSES:
            time.append(gloss)
        elif gloss in SUBJECT_GLOSSES:
            subject.append(gloss)
        elif gloss in polite_glosses:
            polite.append(gloss)
        elif gloss in VERB_GLOSSES:
            verb.append(gloss)
        elif gloss in QUESTION_GLOSSES:
            question.append(gloss)
        else:
            other.append(gloss)

    ordered = time + subject + other + verb + polite + question

    seen = set()
    deduped = []

    for gloss in ordered:
        if gloss not in seen:
            deduped.append(gloss)
            seen.add(gloss)

    return deduped


def choose_sign_variations(glosses: List[str], window_size: int = 2) -> List[str]:
    """
    Chooses the correct variation for ambiguous glosses.

    Example:
        ["go", "right"] -> ["go", "right_direction"]
        ["right", "answer"] -> ["right_correct", "answer"]
    """
    final_glosses: List[str] = []

    for index, gloss in enumerate(glosses):
        if gloss not in VARIATION_RULES:
            final_glosses.append(gloss)
            continue

        start = max(0, index - window_size)
        end = min(len(glosses), index + window_size + 1)
        nearby_glosses = set(glosses[start:index] + glosses[index + 1:end])

        selected_variation = DEFAULT_VARIATIONS[gloss]

        for variation, context_words in VARIATION_RULES[gloss].items():
            if nearby_glosses.intersection(context_words):
                selected_variation = variation
                break

        final_glosses.append(selected_variation)

    return final_glosses



def translate_text_to_bsl_gloss(text: str) -> Dict[str, Any]:
    """
    Full backend pipeline for Text Translate View.

    Input:
        English text from the app.

    Output:
        Dictionary containing:
        - original input text
        - tokens
        - broad glosses
        - final BSL glosses
        - matching dataset JSON files
        - missing JSON files
        - avatar animation IDs
    """
    tokens = tokenize_text(text)
    phrase_tokens = apply_phrase_rules(tokens)
    broad_glosses = english_tokens_to_glosses(phrase_tokens)
    ordered_glosses = reorder_to_bsl_gloss(broad_glosses)
    final_glosses = choose_sign_variations(ordered_glosses)

    dataset_sequence = build_animation_sequence(final_glosses)

    missing_json = [
        item["sign"]
        for item in dataset_sequence
        if not item["available"]
    ]

    return {
        "input_text": text,
        "tokens": tokens,
        "broad_glosses": broad_glosses,
        "bsl_glosses": final_glosses,
        "gloss_sentence": " ".join(final_glosses),
        "dataset_sequence": dataset_sequence,
        "missing_json": missing_json,
        "avatar_animations": [
            item["animation_id"]
            for item in dataset_sequence
            if item["available"]
        ],
        "avatar_ready": len(missing_json) == 0
    }


def process_sign_sequence(detected_signs: List[str]) -> Dict[str, Any]:
    """
    Backwards-compatible function for older sign-detection testing.

    Takes detected sign labels, applies context-aware variation rules,
    then connects them to the dataset and avatar animation sequence.
    """
    normalised = [normalise_label(sign) for sign in detected_signs]
    final_glosses = choose_sign_variations(normalised)

    dataset_sequence = build_animation_sequence(final_glosses)

    missing_json = [
        item["sign"]
        for item in dataset_sequence
        if not item["available"]
    ]

    return {
        "detected_signs": detected_signs,
        "context_aware_signs": final_glosses,
        "dataset_sequence": dataset_sequence,
        "missing_json": missing_json,
        "avatar_animations": [
            item["animation_id"]
            for item in dataset_sequence
            if item["available"]
        ],
        "avatar_ready": len(missing_json) == 0
    }

if __name__ == "__main__":
    examples = [
        "I will go home",
        "I need help please",
        "I feel bad",
        "Go right",
        "That is the right answer",
        "Well done",
        "I will go to university tomorrow",
    ]

    for sentence in examples:
        result = translate_text_to_bsl_gloss(sentence)
        print("English:       ", result["input_text"])
        print("BSL gloss:     ", result["gloss_sentence"])
        print("Missing JSON:  ", result["missing_json"])
        print("Dataset files: ", result["dataset_sequence"])
        print()
