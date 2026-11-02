import SwiftUI

struct NotificationsView: View {

    @Environment(\.dismiss) private var dismiss
    @State private var appNotificationsEnabled: Bool   = true
    @State private var emailNotificationsEnabled: Bool = true

    private let primaryBlue: Color    = Color(red: 56  / 255, green: 182 / 255, blue: 255 / 255)
    private let sectionGrey: Color    = Color(red: 198 / 255, green: 198 / 255, blue: 206 / 255)
    private let backgroundGrey: Color = Color(red: 248 / 255, green: 248 / 255, blue: 248 / 255)
    
    private let fontName: String = "ArialRoundedMTBold"

    private let titleFontSize: CGFloat          = 24
    private let rowFontSize: CGFloat            = 22
    private let toggleScale: CGFloat            = 1.1

    private let titleTopPadding: CGFloat        = 60
    private let dividerTopPadding: CGFloat      = 22
    private let dividerHorizontalPadding: CGFloat = 30
    private let dividerHeight: CGFloat          = 4

    private let rowSpacing: CGFloat             = 38
    private let rowsTopPadding: CGFloat         = 42
    private let horizontalPadding: CGFloat      = 34

    var body: some View {

        ZStack(alignment: .topLeading) {
            
            backgroundGrey
                .ignoresSafeArea()
            
            VStack(spacing: 0) {
                notificationsTitle
                
                sectionDivider
                
                notificationRows
                
                Spacer()
            }
           
            backButton
            
        }
        
        .navigationBarBackButtonHidden(true)
        .onAppear {
           
            appNotificationsEnabled = UserDefaults.standard.object(forKey: "appNotificationsEnabled") as? Bool ?? true
            
            emailNotificationsEnabled = UserDefaults.standard.object(forKey: "emailNotificationsEnabled") as? Bool ?? true
        }
        
        .onChange(of: appNotificationsEnabled) {
            UserDefaults.standard.set(appNotificationsEnabled, forKey: "appNotificationsEnabled")
        }
        
        .onChange(of: emailNotificationsEnabled) {
            UserDefaults.standard.set(emailNotificationsEnabled, forKey: "emailNotificationsEnabled")
        }
    }
            
    private var backButton: some View {
        
        Button {
                        dismiss()
                    } label: {
                        Image(systemName: "chevron.left")
                            .font(.system(size: 20, weight: .semibold))
                            .foregroundColor(sectionGrey)
                    }
                    .padding(.leading, 34)
                    .padding(.top, titleTopPadding + 4)
        }

private var notificationsTitle: some View {
    
    Text("Notifications")
        .font(.custom(fontName, size: titleFontSize))
        .foregroundColor(sectionGrey)
        .padding(.top, titleTopPadding)
}

    private var sectionDivider: some View {

        Rectangle()
            .fill(sectionGrey)
            .frame(height: dividerHeight)
            .padding(.top, dividerTopPadding)
            .padding(.horizontal, dividerHorizontalPadding)
    }

    private var notificationRows: some View {

        VStack(spacing: rowSpacing) {

            notificationRow(title: "App Notifications", isOn: $appNotificationsEnabled)

            notificationRow(title: "Email Notifications", isOn: $emailNotificationsEnabled)
        }
        .padding(.top, rowsTopPadding)
        .padding(.horizontal, horizontalPadding)
    }

    private func notificationRow(title: String, isOn: Binding<Bool>) -> some View {

        HStack {

            Text(title)
                .font(.custom(fontName, size: rowFontSize))
                .foregroundColor(primaryBlue)

            Spacer()

            Toggle("", isOn: isOn)
                .labelsHidden()
                .tint(primaryBlue)
                .scaleEffect(toggleScale)
        }
    }
}

#Preview {
    NavigationStack {
        NotificationsView()
    }
}
