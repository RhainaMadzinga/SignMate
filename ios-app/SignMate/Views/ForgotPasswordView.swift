//
//  ForgotPasswordView.swift
//  SignMate
//
//  Created by Rhaina on 15/05/2026.
//


import SwiftUI

struct ForgotPasswordView: View {

    @EnvironmentObject var authViewModel: AuthViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var email = ""
    @State private var showResetSentAlert = false

    private let primaryBlue = Color(red: 56 / 255, green: 182 / 255, blue: 255 / 255)

    var body: some View {
        ZStack {
            Color.white
                .ignoresSafeArea()

            VStack(spacing: 24) {

                // MARK: - Header

                VStack(spacing: 10) {
                    Text("Forgot Password?")
                        .font(.system(size: 32, weight: .heavy, design: .rounded))
                        .foregroundColor(primaryBlue)

                    Text("Enter your email address and we’ll send you a link to reset your password.")
                        .font(.system(size: 15, weight: .medium, design: .rounded))
                        .foregroundColor(.gray)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal, 28)
                }
                .padding(.top, 60)

                // MARK: - Email Field

                VStack(alignment: .leading, spacing: 8) {
                    Text("Email address")
                        .font(.system(size: 14, weight: .bold, design: .rounded))
                        .foregroundColor(.gray)

                    TextField("Enter your email", text: $email)
                        .keyboardType(.emailAddress)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .padding()
                        .frame(height: 56)
                        .background(Color.gray.opacity(0.08))
                        .cornerRadius(14)
                }
                .padding(.horizontal, 28)
                .padding(.top, 20)

                // MARK: - Message

                if let message = authViewModel.errorMessage {
                    Text(message)
                        .font(.system(size: 14, weight: .medium, design: .rounded))
                        .foregroundColor(message.lowercased().contains("sent") ? .green : .red)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal, 28)
                }

                // MARK: - Reset Button

                Button {
                    Task {
                        await authViewModel.resetPassword(email: email)

                        if authViewModel.errorMessage?.lowercased().contains("sent") == true {
                            showResetSentAlert = true
                        }
                    }
                } label: {
                    Text("Send Reset Link")
                        .font(.system(size: 16, weight: .heavy, design: .rounded))
                        .foregroundColor(.white)
                        .frame(maxWidth: .infinity)
                        .frame(height: 56)
                        .background(primaryBlue)
                        .cornerRadius(14)
                }
                .padding(.horizontal, 28)
                .padding(.top, 8)

                // MARK: - Back to Login

                Button {
                    dismiss()
                } label: {
                    Text("Back to Login")
                        .font(.system(size: 15, weight: .heavy, design: .rounded))
                        .foregroundColor(primaryBlue)
                }
                .padding(.top, 8)

                Spacer()
            }
        }
        .navigationBarBackButtonHidden(false)
        .alert("Reset email sent", isPresented: $showResetSentAlert) {
            Button("OK") {
                dismiss()
            }
        } message: {
            Text("Please check your email inbox for the password reset link.")
        }
    }
}

#Preview {
    NavigationStack {
        ForgotPasswordView()
            .environmentObject(AuthViewModel())
    }
}
