//
//  RootView.swift
//  SignMate
//
//  Created by Rhaina on 30/05/2026.
//

import SwiftUI

struct RootView: View {

    @EnvironmentObject var authViewModel: AuthViewModel

    @State private var showSplash = true

    var body: some View {
        Group {
            if showSplash {
                SplashView()
            } else if authViewModel.isLoading {
                loadingView
            } else if authViewModel.isLoggedIn {
                NavigationStack {
                    TextTranslateView()
                }
            } else {
                NavigationStack {
                    WelcomeView()
                }
            }
        }
        .onAppear {
            DispatchQueue.main.asyncAfter(deadline: .now() + 3) {
                withAnimation {
                    showSplash = false
                }
            }
        }
    }

    private var loadingView: some View {
        ZStack {
            Color.white
                .ignoresSafeArea()

            VStack(spacing: 16) {
                ProgressView()
                    .scaleEffect(1.3)

                Text("Loading SignMate")
                    .font(.system(size: 18, weight: .heavy, design: .rounded))
                    .foregroundColor(.gray)
            }
        }
    }
}

#Preview {
    RootView()
        .environmentObject(AuthViewModel())
}
