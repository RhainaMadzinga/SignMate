"""
bsl_gloss_rules.py

Rule dictionaries for SignMate's classical NLP text-to-BSL gloss layer.

Keep this file separate from context_recognition.py so that the vocabulary,
English-to-gloss mappings, and variation rules can be edited without changing
the backend pipeline code.
"""

# Words that do not usually need their own sign in the simplified BSL gloss.
# Example: "I will go home" -> "now I home go"
STOP_WORDS = {
    "a", "an", "the", "to", "at", "in", "on", "is", "am", "are",
    "was", "were", "be", "been", "being", "do", "does", "did"
}

# Basic English word/phrase replacements into dataset-style gloss names.
# Add to this as your dataset grows.
ENGLISH_TO_GLOSS = {
    "i": "i",
    "me": "i",
    "my": "my",
    "you": "you",
    "we": "we",
    "us": "we",

    "hello": "hello",
    "hi": "hello",
    "please": "please",
    "thank": "thank_you",
    "thanks": "thank_you",
    "thankyou": "thank_you",
    "sorry": "sorry",
    "oops": "sorry_oops",
    "yes": "yes",
    "no": "no",

    "go": "go",
    "went": "past_go",
    "come": "come",
    "home": "home",
    "university": "university",
    "school": "school",
    "room": "room",

    "eat": "eat",
    "drink": "drink",
    "water": "water",
    "want": "want",
    "need": "need",
    "help": "help",

    "good": "good",
    "bad": "bad",
    "terrible": "terrible",
    "fantastic": "fantastic",
    "well": "well",

    "now": "now",
    "today": "today",
    "tomorrow": "tomorrow",
    "yesterday": "yesterday",
    "morning": "morning",
    "afternoon": "afternoon",
    "night": "night",
    "before": "before",
    "later": "later",
    "soon": "soon",
    "past": "past",
    "future": "future",
    "last": "last_week",
    "next": "next_week",

    "right": "right",
    "left": "left_direction",
    "finish": "finish",
    "complete": "finish_complete",
    "stop": "finish_stop",
    "more": "more",

    "what": "what",
    "where": "where",
    "why": "why",
    "how": "how",
}

# Multi-word phrases should be handled before single-word token mapping.
PHRASE_TO_GLOSS = {
    "thank you": ["thank_you"],
    "last week": ["last_week"],
    "next week": ["next_week"],
    "right side": ["right_side"],
    "right direction": ["right_direction"],
    "right answer": ["right_correct"],
    "correct answer": ["right_correct"],
    "well done": ["well_good"],
    "feel well": ["well_health"],
    "feel bad": ["bad_health"],
}

# Tense handling for simple English input.
# BSL often marks time at the beginning of the sentence.
FUTURE_WORDS = {"will", "going_to", "shall"}
PAST_WORDS = {"went", "was", "were", "did", "had"}
PRESENT_WORDS = {"now", "today"}

# Simplified ordering:
# TIME + SUBJECT + OBJECT/PLACE + VERB + QUESTION
TIME_GLOSSES = {
    "now", "today", "tomorrow", "yesterday", "morning", "afternoon",
    "night", "before", "later", "soon", "past", "future", "last_week", "next_week"
}

SUBJECT_GLOSSES = {"i", "you", "we", "he", "she", "they", "me", "my"}

VERB_GLOSSES = {
    "go", "come", "eat", "drink", "want", "need", "help",
    "finish_complete", "finish_stop", "finish"
}

QUESTION_GLOSSES = {"what", "where", "why", "how"}

# Ambiguous glosses and the variation that should be chosen from context.
# The keys are broad glosses that may not have a direct dataset JSON.
VARIATION_RULES = {
    "right": {
        "right_correct": {"answer", "correct", "yes", "true", "okay"},
        "right_direction": {"go", "turn", "left_direction", "direction", "where"},
        "right_side": {"hand", "arm", "leg", "body", "side"}
    },
    "finish": {
        "finish_complete": {"work", "school", "task", "food", "project", "lesson", "homework"},
        "finish_stop": {"stop", "enough", "end", "no_more", "now"}
    },
    "well": {
        "well_health": {"feel", "health", "sick", "better", "ill"},
        "well_good": {"done", "good", "work", "sign", "perform"}
    },
    "bad": {
        "bad_health": {"feel", "sick", "ill", "pain", "stomach", "head"},
        "bad_quality": {"work", "answer", "wrong", "result", "behaviour", "sign"}
    },
    "help": {
        "help_request": {"need", "please", "i", "me", "can", "want"},
        "help_offer": {"you", "give", "support", "assist"}
    }
}

DEFAULT_VARIATIONS = {
    "right": "right_correct",
    "finish": "finish_complete",
    "well": "well_health",
    "bad": "bad_quality",
    "help": "help_request"
}
