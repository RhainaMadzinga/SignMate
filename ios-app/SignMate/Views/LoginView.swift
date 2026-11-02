//
//  LoginView.swift
//  SignMate
//
//  Created by Rhaina on 06/04/2026.
//  Edited by Rhaina on 15/05/2026.
import SwiftUI

struct LoginView: View {

    @EnvironmentObject var authViewModel: AuthViewModel

    @State private var email = ""
    @State private var password = ""

    private let primaryBlue = Color(red: 56 / 255, green: 182 / 255, blue: 255 / 255)

    var body: some View {
        VStack(spacing: 24) {

            Spacer()

            Text("Welcome Back")
                .font(.system(size: 36, weight: .heavy, design: .rounded))
                .fontWeight(.bold)

            Text("Log in to continue using SignMate")
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
            }

            if let errorMessage = authViewModel.errorMessage {
                Text(errorMessage)
                    .font(.system(size: 14, weight: .medium, design: .rounded))
                    .foregroundColor(.red)
                    .multilineTextAlignment(.center)
                    .padding(.horizontal)
            }

            Button {
                Task {
                    await authViewModel.signIn(email: email, password: password)
                }
            } label: {
                if authViewModel.isLoading {
                    ProgressView()
                        .tint(.white)
                } else {
                    Text("LOG IN")
                }
            }
            .buttonStyle(PrimaryButtonStyle(primaryColor: primaryBlue))
            
            .background(primaryBlue)
            .foregroundColor(.white)
            .cornerRadius(16)

            NavigationLink {
                ForgotPasswordView()
                    .environmentObject(authViewModel)
            } label: {
                Text("Forgot password?")
                    .foregroundColor(primaryBlue)
                    .fontWeight(.semibold)
            }

            NavigationLink {
                CreateAccountView()
                    .environmentObject(authViewModel)
            } label: {
                Text("Don't have an account?")
                    .foregroundColor(primaryBlue)
                    .fontWeight(.semibold)
            }

            Spacer()
        }
        .padding(24)
        .navigationBarBackButtonHidden(true)
    }
}

#Preview {
    NavigationStack {
        LoginView()
            .environmentObject(AuthViewModel())
    }
}
