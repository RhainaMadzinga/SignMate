//
//  CreateAccountView.swift
//  SignMate
//
//  Created by Rhaina on 07/04/2026.
//  Edited by Rhaina on 15/05/2026
import SwiftUI

struct CreateAccountView: View {

    @EnvironmentObject var authViewModel: AuthViewModel
 

    @State private var email = ""
    @State private var password = ""
    @State private var confirmPassword = ""

    private let primaryBlue = Color(red: 56 / 255, green: 182 / 255, blue: 255 / 255)

    var body: some View {
        VStack(spacing: 24) {

            Spacer()

            Text("Create Account")
                .font(.system(size: 36, weight: .heavy, design: .rounded))
                .fontWeight(.bold)

            Text("Create your SignMate account")
                .foregroundColor(.gray)

            VStack(spacing: 16) {
                TextField("Email address", text: $email)
                    .textInputAutocapitalization(.never)
                    .keyboardType(.emailAddress)
                    .autocorrectionDisabled()
                    .padding()
                    .background(Color.gray.opacity(0.12))
                    .cornerRadius(14)

                SecureField("Password", text: $password)
                    .padding()
                    .background(Color.gray.opacity(0.12))
                    .cornerRadius(14)

                SecureField("Confirm password", text: $confirmPassword)
                    .padding()
                    .background(Color.gray.opacity(0.12))
                    .cornerRadius(14)
            }

            if let errorMessage = authViewModel.errorMessage{
                Text(errorMessage)
                    .foregroundColor(.red)
                    .font(.footnote)
                    .multilineTextAlignment(.center)
            }



            Button {
                if password != confirmPassword {
                    authViewModel.errorMessage = "Passwords do not match."
                } else {
                    Task {
                        await authViewModel.createAccount(email: email, password: password)
                    }
                }
            } label: {
                Text("Create Account")
            }
            .buttonStyle(PrimaryButtonStyle(primaryColor: primaryBlue))
            
            .background(primaryBlue)
            .foregroundColor(.white)
            .cornerRadius(16)

            Spacer()
        }
        .padding(24)
    }
}

#Preview {
    NavigationStack {
        CreateAccountView()
            .environmentObject(AuthViewModel())
    }
}
