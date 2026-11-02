//
//  TranslationService.swift
//  SignMate
//
//  Connects the SignMate iOS app to the local Python Flask backend.
//

import Foundation

// MARK: - Backend Request

struct BackendTranslationRequest: Codable {
    let text: String
}

// MARK: - Backend Response Models

struct BackendDatasetItem: Codable {
    let animation_id: String?
    let available: Bool
    let dataset_file: String?
    let message: String?
    let sign: String
}

struct BackendTranslationResponse: Codable {
    let avatar_animations: [String]
    let avatar_ready: Bool
    let broad_glosses: [String]
    let bsl_glosses: [String]
    let dataset_sequence: [BackendDatasetItem]
    let gloss_sentence: String
    let input_text: String
    let missing_json: [String]
    let tokens: [String]
}

// MARK: - Translation Service

final class TranslationService {

    static let shared = TranslationService()

    private init() {}

    private let backendURL = "http://127.0.0.1:5000/translate"

    func translate(_ input: String) async throws -> TranslationResult {
        let cleanedInput = input.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !cleanedInput.isEmpty else {
            throw TranslationError.emptyInput
        }

        guard let url = URL(string: backendURL) else {
            throw TranslationError.backendConnectionFailed
        }

        let requestBody = BackendTranslationRequest(text: cleanedInput)

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(requestBody)

        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse else {
            throw TranslationError.invalidBackendResponse
        }

        guard httpResponse.statusCode == 200 else {
            throw TranslationError.backendConnectionFailed
        }

        let backendResult = try JSONDecoder().decode(BackendTranslationResponse.self, from: data)

        if !backendResult.missing_json.isEmpty {
            throw TranslationError.unsupportedSigns(backendResult.missing_json)
        }

        return TranslationResult(
            originalText: backendResult.input_text,
            glossSequence: backendResult.bsl_glosses,
            subtitleText: backendResult.gloss_sentence,
            animationIDs: backendResult.avatar_animations,
            avatarVideoURL: nil
        )
    }
}

// MARK: - Translation Errors

enum TranslationError: Error, LocalizedError {
    case emptyInput
    case unsupportedSigns([String])
    case backendConnectionFailed
    case invalidBackendResponse

    var errorDescription: String? {
        switch self {
        case .emptyInput:
            return "Please enter a sentence to translate."

        case .unsupportedSigns(let signs):
            return "These signs are not available yet: \(signs.joined(separator: ", "))"

        case .backendConnectionFailed:
            return "Could not connect to the SignMate backend. Make sure the Python backend is running."

        case .invalidBackendResponse:
            return "The SignMate backend returned an invalid response."
        }
    }
}
