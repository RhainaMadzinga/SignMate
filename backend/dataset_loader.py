import json
from pathlib import Path


BASE_DIR = Path(__file__).resolve().parent.parent

MAPPING_FILE = BASE_DIR / "data" / "signmate_dataset" / "sign_mapping.json"


def load_sign_mapping():
    """
    Loads the sign mapping file.

    The mapping file links each BSL gloss word to:
    - its landmark JSON file
    - its avatar animation ID
    """
    if not MAPPING_FILE.exists():
        raise FileNotFoundError(f"Mapping file not found: {MAPPING_FILE}")

    with open(MAPPING_FILE, "r", encoding="utf-8") as file:
        return json.load(file)


def get_dataset_file_for_sign(sign):
    """
    Finds the dataset JSON file and animation ID for one sign.
    """
    mapping = load_sign_mapping()

    normalised_sign = sign.lower().strip().replace(" ", "_")

    if normalised_sign not in mapping:
        return {
            "available": False,
            "sign": normalised_sign,
            "dataset_file": None,
            "animation_id": None,
            "message": f"No dataset file found for sign: {normalised_sign}"
        }

    dataset_file = BASE_DIR / mapping[normalised_sign]["dataset_file"]

    return {
        "available": dataset_file.exists(),
        "sign": normalised_sign,
        "dataset_file": str(dataset_file),
        "animation_id": mapping[normalised_sign]["animation_id"],
        "message": "Dataset file found" if dataset_file.exists() else "Dataset file path is in mapping but file does not exist"
    }


def build_animation_sequence(signs):
    """
    Takes a list of BSL gloss signs and returns the dataset files
    and animation IDs needed by the avatar.
    """
    sequence = []

    for sign in signs:
        sequence.append(get_dataset_file_for_sign(sign))

    return sequence


def print_sequence_result(sequence):
    """
    Prints the result clearly for testing.
    """
    print("\nSignMate Dataset Loader Test")
    print("----------------------------")

    for item in sequence:
        print(f"Sign: {item['sign']}")
        print(f"Available: {item['available']}")
        print(f"Dataset file: {item['dataset_file']}")
        print(f"Animation ID: {item['animation_id']}")
        print(f"Message: {item['message']}")
        print()


if __name__ == "__main__":
    test_signs = [
        "hello",
        "i",
        "want",
        "water",
        "please",
        "thank_you",
        "yes",
        "no"
    ]

    result = build_animation_sequence(test_signs)
    print_sequence_result(result)