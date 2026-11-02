//
//  AvatarService.swift
//  SignMate
//
//  Created by Rhaina on 31/05/2026.
//

import Foundation

final class AvatarService {

    static let shared = AvatarService()

    private init() {}

    func prepareAnimationPayload(for result: TranslationResult) -> AvatarAnimationPayload {
        AvatarAnimationPayload(
            originalText: result.originalText,
            glossSequence: result.glossSequence,
            subtitleText: result.subtitleText,
            animationFiles: result.animationIDs,
            avatarReady: !result.animationIDs.isEmpty
        )
    }

    func prepareUnityPayloadJSON(for result: TranslationResult) throws -> String {
        let payload = prepareAnimationPayload(for: result)
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]

        let data = try encoder.encode(payload)

        guard let jsonString = String(data: data, encoding: .utf8) else {
            throw AvatarServiceError.encodingFailed
        }

        return jsonString
    }

    func sendPayloadToUnity(for result: TranslationResult) {
        do {
            let payloadJSON = try prepareUnityPayloadJSON(for: result)
            UnityBridge.shared.sendAvatarPayload(payloadJSON)
        } catch {
            print("Failed to send payload to Unity: \(error.localizedDescription)")
        }
    }
    
}

struct AvatarAnimationPayload: Codable {
    let originalText: String
    let glossSequence: [String]
    let subtitleText: String
    let animationFiles: [String]
    let avatarReady: Bool
}

enum AvatarServiceError: Error, LocalizedError {
    case encodingFailed

    var errorDescription: String? {
        switch self {
        case .encodingFailed:
            return "Could not encode the avatar animation payload."
        }
    }
}
