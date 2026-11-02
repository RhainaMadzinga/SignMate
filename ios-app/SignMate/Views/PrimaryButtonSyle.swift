//
//  PrimaryButtonSyle.swift
//  SignMate
//
//  Created by Rhaina on 16/05/2026.
//

import SwiftUI

struct PrimaryButtonStyle: ButtonStyle {
    
    let primaryColor: Color
    
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 18, weight: .heavy, design: .rounded))
            .foregroundColor(.white)
            .frame(maxWidth: .infinity)
            .frame(height: 56)
            .background(primaryColor)
            .cornerRadius(14)
            .opacity(configuration.isPressed ? 0.8 : 1.0)
    }
}
