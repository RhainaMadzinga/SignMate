//
//  AuthViewModel.swift
//  SignMate
//
//  Created by Rhaina on 15/05/2026.
//

import Foundation
import Combine
import FirebaseAuth

@MainActor
final class AuthViewModel: ObservableObject {

    @Published var user: User?
    @Published var isLoading = true
    @Published var errorMessage: String? = nil

    private var authStateHandle: AuthStateDidChangeListenerHandle?

    var isLoggedIn: Bool {
        user != nil
    }

    init() {
        listenToAuthState()
    }

    deinit {
        if let authStateHandle = authStateHandle {
            Auth.auth().removeStateDidChangeListener(authStateHandle)
        }
    }

    private func listenToAuthState() {
        authStateHandle = Auth.auth().addStateDidChangeListener { [weak self] _, user in
            Task { @MainActor in
                self?.user = user
                self?.isLoading = false
            }
        }
    }

    func createAccount(email: String, password: String) async {
        errorMessage = nil

        let cleanedEmail = email.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !cleanedEmail.isEmpty else {
            errorMessage = "Please enter your email address."
            return
        }

        guard !password.isEmpty else {
            errorMessage = "Please enter your password."
            return
        }

        guard password.count >= 6 else {
            errorMessage = "Your password must be at least 6 characters."
            return
        }

        do {
            let result = try await Auth.auth().createUser(withEmail: cleanedEmail, password: password)
            self.user = result.user
        } catch {
            errorMessage = readableAuthError(error)
        }
    }

    func signIn(email: String, password: String) async {
        errorMessage = nil

        let cleanedEmail = email.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !cleanedEmail.isEmpty else {
            errorMessage = "Please enter your email address."
            return
        }

        guard !password.isEmpty else {
            errorMessage = "Please enter your password."
            return
        }

        do {
            let result = try await Auth.auth().signIn(withEmail: cleanedEmail, password: password)
            self.user = result.user
        } catch {
            errorMessage = readableAuthError(error)
        }
    }

    func resetPassword(email: String) async {
        errorMessage = nil

        let cleanedEmail = email.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !cleanedEmail.isEmpty else {
            errorMessage = "Please enter your email address."
            return
        }

        do {
            try await Auth.auth().sendPasswordReset(withEmail: cleanedEmail)
            errorMessage = "Password reset email sent."
        } catch {
            errorMessage = readableAuthError(error)
        }
    }

    func signOut() {
        errorMessage = nil

        do {
            try Auth.auth().signOut()
            self.user = nil
        } catch {
            errorMessage = "Could not log out. Please try again."
        }
    }

    private func readableAuthError(_ error: Error) -> String {
        let nsError = error as NSError

        guard let code = AuthErrorCode(rawValue: nsError.code) else {
            return error.localizedDescription
        }

        switch code {
        case .invalidEmail:
            return "Please enter a valid email address."

        case .emailAlreadyInUse:
            return "This email address is already in use."

        case .weakPassword:
            return "Your password is too weak. Please use at least 6 characters."

        case .wrongPassword:
            return "Incorrect password. Please try again."

        case .userNotFound:
            return "No account was found with this email address."

        case .networkError:
            return "Network error. Please check your connection."

        default:
            return error.localizedDescription
        }
    }
}
