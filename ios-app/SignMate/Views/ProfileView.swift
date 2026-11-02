import SwiftUI

struct ProfileView: View {
    
    @Environment(\.dismiss) private var dismiss
    @State private var name: String = "test"
    @State private var email: String = "test@example.com"
    @State private var password: String = ""
    @State private var showSavedAlert: Bool = false

    private let primaryBlue: Color = Color(red: 56  / 255, green: 182 / 255, blue: 255 / 255)
    private let sectionGrey: Color = Color(red: 198 / 255, green: 198 / 255, blue: 206 / 255)
    private let fieldGrey: Color   = Color(red: 217 / 255, green: 217 / 255, blue: 217 / 255)

    private let fontName: String = "ArialRoundedMTBold"
    
    private let defaultName: String     = "test"
    private let defaultEmail: String    = "test@example.com"

    private let titleFontSize: CGFloat          = 24
    private let fieldLabelFontSize: CGFloat     = 20
    private let fieldValueFontSize: CGFloat     = 18
    private let deletionFontSize: CGFloat       = 22

    private let titleTopPadding: CGFloat        = 50
    private let dividerTopPadding: CGFloat      = 22
    private let dividerHorizontalPadding: CGFloat = 30
    private let dividerHeight: CGFloat          = 4

    private let firstFieldTopPadding: CGFloat   = 42
    private let fieldTopPadding: CGFloat        = 28
    private let horizontalPadding: CGFloat      = 34

    private let fieldHeight: CGFloat            = 50
    private let fieldCornerRadius: CGFloat      = 8
    private let fieldSpacing: CGFloat           = 14
    private let fieldHorizontalPadding: CGFloat = 14

    private let deletionHeight: CGFloat         = 52
    private let deletionCornerRadius: CGFloat   = 8
    private let deletionBottomPadding: CGFloat  = 130
    private let saveButtonHeight: CGFloat = 48
    
    var body: some View {

        ZStack(alignment: .topLeading) {
            
            Color.white
                .ignoresSafeArea()
            
            VStack(alignment: .center, spacing: 0) {
                
                profileTitle
                
                sectionDivider
                
                fieldsSection
                
                Spacer()
                
                accountDeletionButton
            }
            
            backButton
        }
        
        .navigationBarBackButtonHidden(true)
        .onAppear {
            loadProfileData()
        }
        
        .alert("Profile Updated", isPresented: $showSavedAlert) {
            Button("OK", role: .cancel) { }
        } message: {
            Text("Your profile changes have been saved.")
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
        

    private var profileTitle: some View {

        Text("Profile")
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

    private var fieldsSection: some View {
        
        VStack(alignment: .leading, spacing: 0) {
            
            editableField(title: "Name", text: $name)
                .padding(.top, firstFieldTopPadding)
            
            editableField(title: "Email", text: $email)
                .padding(.top, fieldTopPadding)
            
            secureEditableField(title: "Password", text: $password)
                .padding(.top, fieldTopPadding)
            
            saveChangesButton .padding(.top, fieldTopPadding)
        }
        .padding(.horizontal, horizontalPadding)
    }

        private var saveChangesButton: some View {
            
            
            Button {
                saveProfileData()
            }label: {
                Text("Save Changes")
                    .font(.custom(fontName, size: 18))
                    .foregroundColor(.white)
                    .frame(maxWidth: .infinity)
                    .frame(height: saveButtonHeight)
                    .background(primaryBlue)
                    .clipShape(RoundedRectangle(cornerRadius: fieldCornerRadius))
            }
        }
    
        private var accountDeletionButton: some View {
            
            Text("Account Deletion")
                .font(.custom(fontName, size: deletionFontSize))
                .foregroundColor(.red)
                .frame(maxWidth: .infinity)
                .frame(height: deletionHeight)
                .background(fieldGrey)
                .clipShape(RoundedRectangle(cornerRadius: deletionCornerRadius))
                .padding(.horizontal, horizontalPadding)
                .padding(.bottom, deletionBottomPadding)
            
        }
        
        private func editableField(title: String, text: Binding<String>) -> some View {
            
            VStack(alignment: .leading, spacing: fieldSpacing) {
                
                Text(title)
                    .font(.custom(fontName, size: fieldLabelFontSize))
                    .foregroundColor(primaryBlue)
                
                TextField("", text: text)
                    .font(.custom(fontName, size: fieldValueFontSize))
                    .foregroundColor(.white)
                    .padding(.horizontal, fieldHorizontalPadding)
                    .frame(height: fieldHeight)
                    .background(fieldGrey)
                    .clipShape(RoundedRectangle(cornerRadius: fieldCornerRadius))
            }
        }
        
        private func secureEditableField(title: String, text: Binding<String>) -> some View {
            
            VStack(alignment: .leading, spacing: fieldSpacing) {
                
                Text(title)
                    .font(.custom(fontName, size: fieldLabelFontSize))
                    .foregroundColor(primaryBlue)
                
                SecureField("", text: text)
                    .font(.custom(fontName, size: fieldValueFontSize))
                    .foregroundColor(.white)
                    .padding(.horizontal, fieldHorizontalPadding)
                    .frame(height: fieldHeight)
                    .background(fieldGrey)
                    .clipShape(RoundedRectangle(cornerRadius: fieldCornerRadius))
            }
        }
        
        private func loadProfileData() {
            
            name     = UserDefaults.standard.string(forKey: "profileName")     ?? defaultName
            email    = UserDefaults.standard.string(forKey: "profileEmail")    ?? defaultEmail
            password = ""
        
    }

                private func saveProfileData() {

                    UserDefaults.standard.set(name,     forKey: "profileName")
                    UserDefaults.standard.set(email,    forKey: "profileEmail")
                    // Passwords are not stored locally in UserDefaults.
                    showSavedAlert = true
                }
            }

#Preview {
    NavigationStack {
        ProfileView()
    }
}
