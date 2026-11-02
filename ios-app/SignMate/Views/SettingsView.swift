import SwiftUI

struct SettingsView: View {
    
    @EnvironmentObject var authViewModel: AuthViewModel
    @Environment(\.dismiss) private var dismiss
    @State private var showSavedTranslations: Bool = false
    @State private var showSignOutAlert: Bool = false
    @State private var savedTranslations: [TranslationResult] = []
    

    private let primaryBlue: Color  = Color(red: 56  / 255, green: 182 / 255, blue: 255 / 255)
    private let lightGrey: Color    = Color(red: 233 / 255, green: 233 / 255, blue: 238 / 255)
    private let sectionGrey: Color  = Color(red: 198 / 255, green: 198 / 255, blue: 206 / 255)
    private let dividerGrey: Color  = Color(red: 201 / 255, green: 201 / 255, blue: 208 / 255)

    private let fontName: String = "ArialRoundedMTBold"

    private let headerHeight: CGFloat        = 190
    private let headerFontSize: CGFloat      = 34
    private let headerTopPadding: CGFloat    = -30

    private let sectionFontSize: CGFloat     = 22
    private let rowFontSize: CGFloat         = 24
    private let chevronSize: CGFloat         = 24
    private let itemFontSize: CGFloat        = 18

    private let signOutWidth: CGFloat        = 168
    private let signOutHeight: CGFloat       = 58
    private let signOutCornerRadius: CGFloat = 12
    private let signOutFontSize: CGFloat     = 22

    private let horizontalPadding: CGFloat    = 18
    private let sectionTopPadding: CGFloat    = 14
    private let rowTopPadding: CGFloat        = 28
    private let dividerTopPadding: CGFloat    = 18
    private let dropdownTopPadding: CGFloat   = 12
    private let signOutTopPadding: CGFloat    = 52
    private let signOutBottomPadding: CGFloat = 40

    private let animationDuration: Double = 0.2

    var body: some View {

        ZStack(alignment: .topLeading) {

            Color.white
                .ignoresSafeArea()

            VStack(spacing: 0) {

                headerSection

                ScrollView(showsIndicators: false) {

                    VStack(alignment: .leading, spacing: 0) {

                        sectionTitle("Account")
                            .padding(.top, sectionTopPadding)

                        profileNavigationLink
                            .padding(.top, rowTopPadding)

                        notificationsNavigationLink
                            .padding(.top, rowTopPadding)
                        
                        sectionDivider()
                            .padding(.top, dividerTopPadding)

                        sectionTitle("History")
                            .padding(.top, 24)

                        dropdownRow(title: "Saved Translations", isExpanded: $showSavedTranslations)
                            .padding(.top, rowTopPadding)

                        if showSavedTranslations {
                            savedTranslationsDropdown
                                .padding(.top, dropdownTopPadding)
                        }
                        sectionDivider()
                            .padding(.top, dividerTopPadding)

                        sectionTitle("Support")
                            .padding(.top, 24)

                        helpCentreNavigationLink
                            .padding(.top, rowTopPadding)

                        signOutButton
                    }
                    .padding(.horizontal, horizontalPadding)
                }
            }
            backButton
        }
        .navigationBarBackButtonHidden(true)
        .onAppear {
            loadSavedTranslations()
        }
        .alert("Sign Out", isPresented: $showSignOutAlert) {
            Button("Cancel", role: .cancel) { }

            Button("Sign Out", role: .destructive) {
                authViewModel.signOut()
            }
        } message: {
            Text("Are you sure you want to sign out?")
        }
    }

    private var headerSection: some View {

        ZStack {

            primaryBlue
                .frame(height: headerHeight)
                .ignoresSafeArea(edges: .top)

            Text("Settings")
                .font(.custom(fontName, size: headerFontSize))
                .foregroundColor(.white)
                .padding(.top, headerTopPadding)
        }
    }

    private var signOutButton: some View {

        Button {
            showSignOutAlert = true
        } label: {
            Text("Sign Out")
                .font(.custom(fontName, size: signOutFontSize))
                .foregroundColor(primaryBlue)
                .frame(width: signOutWidth, height: signOutHeight)
                .background(lightGrey)
                .clipShape(RoundedRectangle(cornerRadius: signOutCornerRadius))
                .frame(maxWidth: .infinity)
        }
        .padding(.top, signOutTopPadding)
        .padding(.bottom, signOutBottomPadding)
        
    }
    private var profileNavigationLink: some View {

        NavigationLink {
            ProfileView()
        } label: {
            settingsRow(title: "Profile", icon: "chevron.right")
        }
        .buttonStyle(.plain)
    }

    private var notificationsNavigationLink: some View {

        NavigationLink {
            NotificationsView()
        } label: {
            settingsRow(title: "Notifications", icon: "chevron.right")
        }
        .buttonStyle(.plain)
    }

    private var helpCentreNavigationLink: some View {

        NavigationLink {
            HelpView()
        } label: {
            settingsRow(title: "Help Centre", icon: "chevron.right")
        }
        .buttonStyle(.plain)
    }
    
    private var backButton: some View {

        Button {
            dismiss()
        } label: {
            Image(systemName: "chevron.left")
                .font(.system(size: 22, weight: .semibold))
                .foregroundColor(.white)
        }
        .padding(.leading, 28)
        .padding(.top, 52)
    }
    
    private func sectionTitle(_ title: String) -> some View {

        Text(title)
            .font(.custom(fontName, size: sectionFontSize))
            .foregroundColor(sectionGrey)
    }

    private func settingsRow(title: String, icon: String) -> some View {

        HStack {

            Text(title)
                .font(.custom(fontName, size: rowFontSize))
                .foregroundColor(primaryBlue)

            Spacer()

            Image(systemName: icon)
                .font(.system(size: chevronSize, weight: .semibold))
                .foregroundColor(primaryBlue)
        }
    }

    private func dropdownRow(title: String, isExpanded: Binding<Bool>) -> some View {

        let chevronIcon     = isExpanded.wrappedValue ? "chevron.up" : "chevron.down"
        let toggleAnimation = Animation.easeInOut(duration: animationDuration)

        return Button {
            withAnimation(toggleAnimation) {
                isExpanded.wrappedValue.toggle()
            }
        } label: {
            HStack {

                Text(title)
                    .font(.custom(fontName, size: rowFontSize))
                    .foregroundColor(primaryBlue)

                Spacer()

                Image(systemName: chevronIcon)
                    .font(.system(size: chevronSize, weight: .semibold))
                    .foregroundColor(primaryBlue)
            }
        }
        .buttonStyle(.plain)
    }

    private var savedTranslationsDropdown: some View {

        VStack(alignment: .leading, spacing: 10) {

            if savedTranslations.isEmpty {

                Text("No saved translations yet")
                    .font(.custom(fontName, size: itemFontSize))
                    .foregroundColor(.gray)
                    .padding(.leading, 12)

                Text("Translations saved from the result page will appear here.")
                    .font(.custom(fontName, size: 13))
                    .foregroundColor(sectionGrey)
                    .padding(.top, 4)
                    .padding(.leading, 12)

            } else {

                Text("Translation history")
                    .font(.custom(fontName, size: 14))
                    .foregroundColor(sectionGrey)
                    .padding(.leading, 12)

                ForEach(savedTranslations) { translation in

                    NavigationLink {
                        ResultView(result: translation)
                    } label: {
                        HStack {

                            Image(systemName: "hands.sparkles.fill")
                                .foregroundColor(primaryBlue)

                            VStack(alignment: .leading, spacing: 4) {
                                Text(formatSubtitle(translation.subtitleText))
                                    .font(.custom(fontName, size: itemFontSize))
                                    .foregroundColor(.gray)
                                    .lineLimit(1)

                                Text(formattedDate(translation.createdAt))
                                    .font(.system(size: 12, weight: .medium, design: .rounded))
                                    .foregroundColor(sectionGrey)
                            }

                            Spacer()

                            Image(systemName: "chevron.right")
                                .font(.system(size: 13, weight: .semibold))
                                .foregroundColor(sectionGrey)
                        }
                        .padding(.leading, 12)
                    }
                    .buttonStyle(.plain)
                }
            }
        }
        .padding(.vertical, 12)
        .padding(.horizontal, 10)
        .background(lightGrey.opacity(0.6))
        .clipShape(RoundedRectangle(cornerRadius: 10))
        .padding(.leading, 8)
    }

    private func loadSavedTranslations() {
        guard let data = UserDefaults.standard.data(forKey: "savedTranslations") else {
            savedTranslations = []
            return
        }

        savedTranslations = (try? JSONDecoder().decode([TranslationResult].self, from: data)) ?? []
    }

    private func formatSubtitle(_ text: String) -> String {
        text
            .replacingOccurrences(of: "_", with: " ")
            .uppercased()
    }

    private func formattedDate(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: date)
    }
    
    private func sectionDivider() -> some View {

        Rectangle()
            .fill(dividerGrey)
            .frame(height: 1)
            .clipShape(RoundedRectangle(cornerRadius: 2))
    }
}

#Preview {
    NavigationStack {
        SettingsView()
            .environmentObject(AuthViewModel())
    }
}

