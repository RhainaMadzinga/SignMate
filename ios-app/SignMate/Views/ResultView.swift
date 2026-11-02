//
//  ResultView.swift
//  SignMate
//
//  Created by Rhaina on 04/05/2026.
//

import SwiftUI
import AVKit

struct ResultView: View {

    // MARK: - Data received from translation service

    let result: TranslationResult

    // MARK: - State

    @State private var showNoVideoAlert = false
    @State private var showVideoPlayer = false
    @State private var showSavedAlert = false
    @State private var unityPayloadText = ""

    // MARK: - Colours

    private let primaryBlue = Color(red: 56 / 255, green: 182 / 255, blue: 255 / 255)
    private let darkBlue = Color(red: 20 / 255, green: 57 / 255, blue: 120 / 255)
    private let avatarPanelBlue = Color(red: 225 / 255, green: 244 / 255, blue: 255 / 255)
    private let subtitleBackground = Color.white.opacity(0.92)

    // MARK: - Subtitle Text

    private var subtitleText: String {
        if result.subtitleText.isEmpty {
            return "No translation available"
        } else {
            return result.subtitleText
        }
    }

    // MARK: - Body

    var body: some View {
        ZStack {
            Color.white
                .ignoresSafeArea()

            VStack(spacing: 22) {

                // MARK: - Header

                Text("SignMate")
                    .font(.system(size: 25, weight: .heavy, design: .rounded))
                    .foregroundColor(primaryBlue)
                    .padding(.top, 22)

                // MARK: - Avatar Output Area

                ZStack(alignment: .topTrailing) {

                    RoundedRectangle(cornerRadius: 32)
                        .fill(avatarPanelBlue)
                        .overlay(
                            RoundedRectangle(cornerRadius: 32)
                                .stroke(primaryBlue.opacity(0.25), lineWidth: 1)
                        )
                        .shadow(color: primaryBlue.opacity(0.18), radius: 18, x: 0, y: 8)

                    VStack(spacing: 0) {

                        // MARK: - Avatar Integration Area

                        VStack(spacing: 12) {
                            Image(systemName: "person.crop.square")
                                .font(.system(size: 70))
                                .foregroundColor(primaryBlue.opacity(0.35))

                            Text("Avatar output area")
                                .font(.system(size: 16, weight: .medium, design: .rounded))
                                .foregroundColor(darkBlue.opacity(0.45))

                            if result.animationIDs.isEmpty {
                                Text("No animation sequence available")
                                    .font(.system(size: 13, weight: .medium, design: .rounded))
                                    .foregroundColor(.red.opacity(0.7))
                            } else {
                                Text("Animation sequence ready")
                                    .font(.system(size: 13, weight: .medium, design: .rounded))
                                    .foregroundColor(darkBlue.opacity(0.35))

                                Text("\(result.animationIDs.count) signs prepared for avatar")
                                    .font(.system(size: 13, weight: .semibold, design: .rounded))
                                    .foregroundColor(primaryBlue)
                            }
                        }
                        .frame(maxWidth: .infinity, maxHeight: .infinity)

                        // MARK: - Subtitle Text

                        Text(subtitleText.uppercased())
                            .font(.system(size: 20, weight: .heavy, design: .rounded))
                            .foregroundColor(darkBlue)
                            .multilineTextAlignment(.center)
                            .lineLimit(2)
                            .minimumScaleFactor(0.75)
                            .padding(.horizontal, 22)
                            .frame(maxWidth: .infinity)
                            .frame(height: 78)
                            .background(
                                RoundedRectangle(cornerRadius: 24)
                                    .fill(subtitleBackground)
                                    .shadow(color: .black.opacity(0.08), radius: 10, x: 0, y: 4)
                            )
                            .padding(.horizontal, 28)
                            .padding(.bottom, 24)
                    }

                    // MARK: - Play Button

                    Button {
                        handlePlayButton()
                    } label: {
                        HStack(spacing: 6) {
                            Image(systemName: "play.fill")
                                .font(.system(size: 14))

                            Text("Play")
                                .font(.system(size: 20, weight: .medium, design: .rounded))
                        }
                        .foregroundColor(.white)
                        .padding(.horizontal, 18)
                        .padding(.vertical, 10)
                        .background(
                            Capsule()
                                .fill(primaryBlue)
                                .shadow(color: primaryBlue.opacity(0.3), radius: 8, x: 0, y: 4)
                        )
                    }
                    .padding(.top, 28)
                    .padding(.trailing, 26)
                }
                .frame(height: 560)
                .padding(.horizontal, 28)

                // MARK: - Bottom Buttons

                HStack(spacing: 24) {

                    // Save translation button
                    Button {
                        saveTranslation()
                    } label: {
                        Image(systemName: "square.and.arrow.down")
                            .font(.system(size: 30, weight: .medium))
                            .foregroundColor(.black)
                            .frame(width: 70, height: 70)
                            .background(
                                RoundedRectangle(cornerRadius: 20)
                                    .fill(Color.white)
                                    .shadow(color: .black.opacity(0.10), radius: 12, x: 0, y: 5)
                            )
                    }

                    // Home button
                    NavigationLink {
                        TextTranslateView()
                    } label: {
                        Image(systemName: "house.fill")
                            .font(.system(size: 30, weight: .medium))
                            .foregroundColor(.black)
                            .frame(width: 70, height: 70)
                            .background(
                                RoundedRectangle(cornerRadius: 20)
                                    .fill(Color.white)
                                    .shadow(color: .black.opacity(0.10), radius: 12, x: 0, y: 5)
                            )
                    }
                }
                .padding(.bottom, 15)

                Spacer()
            }
        }
        .navigationBarBackButtonHidden(true)
        .onAppear {
            prepareAvatarPayload()
        }

        // MARK: - Alerts

        .alert("Avatar animation ready", isPresented: $showNoVideoAlert) {
            Button("OK", role: .cancel) { }
        } message: {
            Text(
                """
                The BSL translation has been mapped to avatar animation files:

                \(result.animationIDs.joined(separator: ", "))
                """
            )
        }

        .alert("Translation saved", isPresented: $showSavedAlert) {
            Button("OK", role: .cancel) { }
        } message: {
            Text("This translation has been saved on this device.")
        }

        // MARK: - Video Player

        .sheet(isPresented: $showVideoPlayer) {
            if let avatarVideoURL = result.avatarVideoURL {
                VideoPlayer(player: AVPlayer(url: avatarVideoURL))
                    .ignoresSafeArea()
            }
        }
    }

    // MARK: - Avatar Preparation

    private func prepareAvatarPayload() {
        do {
            unityPayloadText = try AvatarService.shared.prepareUnityPayloadJSON(for: result)
            AvatarService.shared.sendPayloadToUnity(for: result)

            print("Unity-ready JSON payload:")
            print(unityPayloadText)
        } catch {
            print("Failed to prepare Unity payload: \(error.localizedDescription)")
        }
    }

    // MARK: - Button Actions

    private func handlePlayButton() {
        prepareAvatarPayload()

        if result.avatarVideoURL == nil {
            showNoVideoAlert = true
        } else {
            showVideoPlayer = true
        }
    }

    private func saveTranslation() {
        var savedTranslations = loadSavedTranslations()
        savedTranslations.append(result)

        if let encoded = try? JSONEncoder().encode(savedTranslations) {
            UserDefaults.standard.set(encoded, forKey: "savedTranslations")
            showSavedAlert = true
        }
    }

    private func loadSavedTranslations() -> [TranslationResult] {
        guard let data = UserDefaults.standard.data(forKey: "savedTranslations") else {
            return []
        }

        return (try? JSONDecoder().decode([TranslationResult].self, from: data)) ?? []
    }
}

#Preview {
    NavigationStack {
        ResultView(
            result: TranslationResult(
                originalText: "I want water please",
                glossSequence: ["i", "water", "want", "please"],
                subtitleText: "i water want please",
                animationIDs: [
                    "i_animation",
                    "water_animation",
                    "want_animation",
                    "please_animation"
                ],
                avatarVideoURL: nil
            )
        )
    }
}
