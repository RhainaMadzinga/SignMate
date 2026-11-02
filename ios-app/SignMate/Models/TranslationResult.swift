//
//  TranslationResult.swift
//  SignMate
//
//  Created by Rhaina on 22/05/2026.
//

import Foundation

struct TranslationResult: Identifiable, Codable {
    let id: UUID
    let originalText: String
    let glossSequence: [String]
    let subtitleText: String
    let animationIDs: [String]
    let avatarVideoURL: URL?
    let createdAt: Date

    init(
        id: UUID = UUID(),
        originalText: String,
        glossSequence: [String],
        subtitleText: String,
        animationIDs: [String],
        avatarVideoURL: URL? = nil,
        createdAt: Date = Date()
    ) {
        self.id = id
        self.originalText = originalText
        self.glossSequence = glossSequence
        self.subtitleText = subtitleText
        self.animationIDs = animationIDs
        self.avatarVideoURL = avatarVideoURL
        self.createdAt = createdAt
    }
}
