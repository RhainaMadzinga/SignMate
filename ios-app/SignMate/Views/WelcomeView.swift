//
//  WelcomeView.swift
//  SignMate
//
//  Created by Rhaina on 05/04/2026.
//
import SwiftUI

struct WelcomeView: View {
    
    @EnvironmentObject var authViewModel: AuthViewModel
    
    private let primaryColor = Color(red: 56/255, green: 182/255, blue: 255/255) // #38BFFF
    private let backgroundColor = Color(.systemGray6)
    
    var body: some View {
        NavigationStack {
            ZStack {
                backgroundColor
                    .ignoresSafeArea()
                
                VStack {
                    Spacer()
                    
                    VStack(spacing: 12) {
                        Text("SignMate")
                            .font(.system(size: 42, weight: .heavy, design: .rounded))
                            .foregroundColor(primaryColor)
                    }
                    
                    Spacer()
                    
                    VStack(spacing: 16) {
                        NavigationLink(destination: CreateAccountView()    .environmentObject(authViewModel)) {
                            Text("CREATE ACCOUNT")
                                .font(.system(size: 16, weight: .heavy, design: .rounded))
                                .foregroundColor(.white)
                                .frame(maxWidth: .infinity)
                                .frame(height: 56)
                                .background(primaryColor)
                                .cornerRadius(14)
                        }
                        
                        
                        NavigationLink(destination: LoginView()    .environmentObject(authViewModel)) {
                            Text("I ALREADY HAVE AN ACCOUNT")
                                .font(.system(size: 16, weight: .heavy, design: .rounded))
                                .foregroundColor(primaryColor)
                                .frame(maxWidth: .infinity)
                                .frame(height: 56)
                                .background(Color.white)
                                .overlay(
                                    RoundedRectangle(cornerRadius: 14)
                                        .stroke(primaryColor, lineWidth: 1.5)
                                )
                                .cornerRadius(14)
                        }
                    }
                    .padding(.horizontal, 24)
                    .padding(.bottom, 48)
                }
            }
            .navigationBarBackButtonHidden(true)
        }
    }
}

#Preview {
    NavigationStack {
        WelcomeView()
            .environmentObject(AuthViewModel())
    }
}
