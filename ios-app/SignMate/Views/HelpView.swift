import SwiftUI

struct HelpView: View {
   
    @Environment(\.dismiss) private var dismiss
    @State private var showFAQ1: Bool = false
    @State private var showFAQ2: Bool = false
    @State private var showFAQ3: Bool = false

    private let primaryBlue: Color    = Color(red: 56  / 255, green: 182 / 255, blue: 255 / 255)
    private let sectionGrey: Color    = Color(red: 198 / 255, green: 198 / 255, blue: 206 / 255)
    private let dividerGrey: Color    = Color(red: 201 / 255, green: 201 / 255, blue: 208 / 255)
    private let backgroundGrey: Color = Color(red: 248 / 255, green: 248 / 255, blue: 248 / 255)

    private let fontName: String = "ArialRoundedMTBold"

    private let titleFontSize: CGFloat            = 24
    private let questionFontSize: CGFloat         = 20
    private let answerFontSize: CGFloat           = 16

    private let titleTopPadding: CGFloat          = 60
    private let dividerTopPadding: CGFloat        = 20
    private let dividerHorizontalPadding: CGFloat = 30
    private let dividerHeight: CGFloat            = 4
    private let faqDividerHeight: CGFloat         = 2

    private let faqSpacing: CGFloat               = 30
    private let faqItemSpacing: CGFloat           = 10
    private let faqTopPadding: CGFloat            = 40
    private let horizontalPadding: CGFloat        = 30

    var body: some View {

        ZStack(alignment: .topLeading) {

            backgroundGrey
                .ignoresSafeArea()

            VStack(spacing: 0) {

                helpTitle

                sectionDivider

                faqSection

                Spacer()
            }
            backButton
                 }
                 .navigationBarBackButtonHidden(true)
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
        

    private var helpTitle: some View {

        Text("Help Centre")
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

    private var faqSection: some View {

        VStack(spacing: faqSpacing) {

            faqItem(
                question: "How do I translate a video into sign?",
                answer: "Go to the Translate tab and upload your video so the app can process sign.",
                isExpanded: $showFAQ1
            )

            faqItem(
                question: "Can I save my videos?",
                answer: "Yes, saved videos will appear in your History section of the settings page .",
                isExpanded: $showFAQ2
            )

            faqItem(
                question: "Is there a text translation section?",
                answer: "Yes, go to the text translation section and type the sign you would like to appear.",
                isExpanded: $showFAQ3
            )
        }
        .padding(.top, faqTopPadding)
        .padding(.horizontal, horizontalPadding)
    }

    private func faqItem(question: String, answer: String, isExpanded: Binding<Bool>) -> some View {

        let chevronIcon = isExpanded.wrappedValue ? "chevron.up" : "chevron.down"

        return VStack(alignment: .leading, spacing: faqItemSpacing) {

            HStack {

                Text(question)
                    .font(.custom(fontName, size: questionFontSize))
                    .foregroundColor(primaryBlue)

                Spacer()

                Image(systemName: chevronIcon)
                    .foregroundColor(primaryBlue)
            }
            .onTapGesture {
                withAnimation {
                    isExpanded.wrappedValue.toggle()
                }
            }

            if isExpanded.wrappedValue {

                Text(answer)
                    .font(.custom(fontName, size: answerFontSize))
                    .foregroundColor(.gray)
                    .transition(.opacity)
            }

            Rectangle()
                .fill(dividerGrey)
                .frame(height: faqDividerHeight)
        }
    }
}

#Preview {
    NavigationStack {
        HelpView()
    }
}
