import SwiftUI

struct ContentView: View {
    
    @StateObject private var authViewModel = AuthViewModel()
    @State private var showSplash = true
    
    var body: some View {
        NavigationStack {
            Group {
                if showSplash {
                    SplashView()
                } else {
                    if authViewModel.isLoggedIn {
                        TextTranslateView()
                            .environmentObject(authViewModel)
                    } else {
                        WelcomeView()
                            .environmentObject(authViewModel)
                    }
                }
            }
        }
        .onAppear {
            DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                withAnimation {
                    showSplash = false
                }
            }
        }
    }
}

#Preview {
    ContentView()
}
