//
//  ProcessingView.swift
//  SignMate
//
//  Created by Rhaina on 17/05/2026.
//

import SwiftUI

struct ProcessingView: View {

    let inputText: String
    
    @Environment(\.dismiss) private var dismiss

    @State private var activeSegment = 0
    @State private var navigateToResult = false
    @State private var result: TranslationResult?
    @State private var errorMessage: String?
    @State private var showErrorAlert = false

    private let primaryBlue = Color(red: 56 / 255, green: 182 / 255, blue: 255 / 255)
    private let darkBlue = Color(red: 20 / 255, green: 57 / 255, blue: 120 / 255)
    private let segmentCount = 22

    var body: some View {
        ZStack {
            Color.white
                .ignoresSafeArea()

            VStack(spacing: 28) {

                Text("SignMate")
                    .font(.system(size: 34, weight: .heavy, design: .rounded))
                    .foregroundColor(primaryBlue)
                    .padding(.top, 36)

                Spacer()

                ZStack {
                    ForEach(0..<segmentCount, id: \.self) { index in
                        Capsule()
                            .fill(index == activeSegment ? primaryBlue : primaryBlue.opacity(0.16))
                            .frame(width: 8, height: 30)
                            .offset(y: -92)
                            .rotationEffect(.degrees(Double(index) / Double(segmentCount) * 360))
                    }

                    Circle()
                        .fill(Color.white)
                        .frame(width: 125, height: 125)
                        .shadow(color: primaryBlue.opacity(0.15), radius: 16, x: 0, y: 8)

                    Image(systemName: "hands.sparkles.fill")
                        .font(.system(size: 42))
                        .foregroundColor(primaryBlue)
                }
                .frame(width: 220, height: 220)

                VStack(spacing: 10) {
                    Text("Processing")
                        .font(.system(size: 26, weight: .heavy, design: .rounded))
                        .foregroundColor(darkBlue)

                    Text("Preparing your avatar translation")
                        .font(.system(size: 16, weight: .medium, design: .rounded))
                        .foregroundColor(.gray)
                        .multilineTextAlignment(.center)

                    Text("“\(inputText)”")
                        .font(.system(size: 15, weight: .semibold, design: .rounded))
                        .foregroundColor(darkBlue.opacity(0.75))
                        .multilineTextAlignment(.center)
                        .padding(.horizontal, 32)
                        .padding(.top, 8)
                }

                Spacer()
            }
        }
        .navigationBarBackButtonHidden(true)
        .onAppear {
            startLoadingAnimation()
        }
        .task {
            await processTranslation()
        }
        .navigationDestination(isPresented: $navigateToResult) {
            if let result = result {
                ResultView(result: result)
            }
        }
        .alert("Translation failed", isPresented: $showErrorAlert) {
            Button("OK", role: .cancel) {
                dismiss()
            }
        } message: {
            Text(errorMessage ?? "Something went wrong while translating your sentence.")
        }
    }

    private func startLoadingAnimation() {
        Timer.scheduledTimer(withTimeInterval: 0.12, repeats: true) { timer in
            activeSegment = (activeSegment + 1) % segmentCount

            if navigateToResult || showErrorAlert {
                timer.invalidate()
            }
        }
    }

    private func processTranslation() async {
        do {
            try await Task.sleep(nanoseconds: 1_200_000_000)

            let translatedResult = try await TranslationService.shared.translate(inputText)

            await MainActor.run {
                self.result = translatedResult
                self.navigateToResult = true
            }
        } catch {
            await MainActor.run {
                self.errorMessage = error.localizedDescription
                self.showErrorAlert = true
            }
        }
    }
}

#Preview {
    NavigationStack {
        ProcessingView(inputText: "I will go home")
    }
}
