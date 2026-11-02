import SwiftUI

struct SplashView: View {


    private let backgroundColour: Color = Color(
        red:   56 / 255,
        green: 182 / 255,
        blue:  255 / 255
    )

    private let logoAssetName: String    = "Signmate Logo Splash"
    private let logoSize: CGFloat        = 700
    private let appName: String          = "SignMate"
    private let appNameFontSize: CGFloat = 40
    private let bottomPadding: CGFloat   = 340


    var body: some View {

        ZStack {

            backgroundColour
                .ignoresSafeArea()

                logoImage
                    .offset(y: 50)

            VStack {

                Spacer()
                appNameText
                    .padding(.bottom, -300)
            }
        }
    }

    private var logoImage: some View {


        return Image(logoAssetName)
            .resizable()
            .scaledToFit()
            .frame(width: logoSize, height: logoSize)
        
    }

    private var appNameText: some View {

        return Text(appName)
            .font(.system(size: appNameFontSize, weight: .heavy, design: .rounded))
            .foregroundColor(.white)
            .padding(.bottom, bottomPadding)
            
    }
}

#Preview {
    SplashView()
}

