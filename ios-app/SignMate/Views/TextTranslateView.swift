import SwiftUI

struct TextTranslateView: View {
    
    @State private var sentence = ""
    @State private var navigateToAvatar: Bool = false
    @State private var navigateToSettings: Bool = false
    @State private var navigateToProcessing = false
    @State private var showEmptyTextAlert = false
    
    private let primaryBlue: Color = Color(red: 56  / 255, green: 182 / 255, blue: 255 / 255)
    private let sectionGrey: Color = Color(red: 198 / 255, green: 198 / 255, blue: 206 / 255)
    private let lightGrey: Color   = Color(red: 233 / 255, green: 233 / 255, blue: 238 / 255)
    private let darkBlue: Color        = Color(red: 20  / 255, green: 57  / 255, blue: 120 / 255)
    private let avatarPanelBlue: Color = Color(red: 225 / 255, green: 244 / 255, blue: 255 / 255)
    
    private var cleanedSentence: String {
        sentence.trimmingCharacters(in: .whitespacesAndNewlines)
    }
    
    private let fontName: String = "ArialRoundedMTBold"
    
    private let titleFontSize: CGFloat          = 34
    private let labelFontSize: CGFloat          = 20
    private let editorFontSize: CGFloat         = 18
    private let enterTextFontSize: CGFloat      = 16
    
    private let titleTopPadding: CGFloat        = 44
    private let titleTrailingPadding: CGFloat   = 28
    
    private let inputSectionTopPadding: CGFloat = 60
    private let inputSectionSpacing: CGFloat    = 12
    private let inputHorizontalPadding: CGFloat = 22
    
    private let editorHeight: CGFloat           = 140
    private let editorPadding: CGFloat          = 10
    private let editorCornerRadius: CGFloat     = 12
    
    private let cardHeight: CGFloat             = 360
    private let cardCornerRadius: CGFloat       = 32
    private let cardHorizontalPadding: CGFloat  = 28
    private let cardTopPadding: CGFloat         = 55
    
    private let buttonIconSize: CGFloat         = 14
    private let buttonHorizontalPadding: CGFloat = 18
    private let buttonVerticalPadding: CGFloat  = 10
    private let buttonBottomPadding: CGFloat    = 24
    
    private let settingsIconSize: CGFloat       = 22
    private let supportedSigns: [String: String] = [
        "after": "after",
        "afternoon": "afternoon",
        "again": "again",
        "bad": "bad",
        "be": "be",
        "before": "before",
        "bright": "bright",
        "cant": "cant",
        "check": "check",
        "close": "close",
        "cold": "cold",
        "come": "come",
        "comfortable": "comfortable",
        "dark": "dark",
        "dont want": "dont_want",
        "drink": "drink",
        "eat": "eat",
        "error": "error",
        "fantastic": "fantastic",
        "finish": "finish",
        "finish complete": "finish_complete",
        "finish stop": "finish_stop",
        "future": "future",
        "go": "go",
        "good": "good",
        "have": "have",
        "he": "he",
        "hello": "hello",
        "help": "help",
        "her": "her",
        "high": "high",
        "his": "his",
        "home": "home",
        "hot": "hot",
        "how": "how",
        "i": "i",
        "last week": "last_week",
        "later": "later",
        "left depart": "left_depart",
        "left direction": "left_direction",
        "left side": "left_side",
        "low": "low",
        "me": "me",
        "more": "more",
        "morning": "morning",
        "my": "my",
        "name": "name",
        "need": "need",
        "next week": "next_week",
        "night": "night",
        "no": "no",
        "now": "now",
        "off": "off",
        "on": "on",
        "open": "open",
        "past": "past",
        "please": "please",
        "present": "present",
        "right correct": "right_correct",
        "right direction": "right_direction",
        "right side": "right_side",
        "room": "room",
        "school": "school",
        "send": "send",
        "soon": "soon",
        "sorry": "sorry",
        "sorry oops": "sorry_oops",
        "stop": "stop",
        "terrible": "terrible",
        "thank you": "thank_you",
        "today": "today",
        "tomorrow": "tomorrow",
        "university": "university",
        "want": "want",
        "water": "water",
        "we": "we",
        "well": "well",
        "went": "went",
        "what": "what",
        "where": "where",
        "why": "why",
        "yes": "yes",
        "yesterday": "yesterday",
        "you": "you"

    ]
    
    var body: some View {
        
        ZStack(alignment: .topLeading) {
            
            Color.white
                .ignoresSafeArea()
            
            VStack(spacing: 0) {
                
                headerSection
                
                inputCard
                
                Spacer()
            }
        }
        .navigationBarBackButtonHidden(true)
        .navigationDestination(isPresented: $navigateToProcessing) {
            ProcessingView(inputText: cleanedSentence)
        }
        .navigationDestination(isPresented: $navigateToSettings) {
            SettingsView()
        }
        .alert("Enter text", isPresented: $showEmptyTextAlert){
            Button("OK", role: .cancel) { }
        } message: {
            Text("Please enter a sentence to translate")
        }
        
    }
    
    
    private var pageTitle: some View {
        
        Text("Text Translation")
            .font(.custom(fontName, size: titleFontSize))
            .foregroundColor(sectionGrey)
            .padding(.top, titleTopPadding)
    }
    
    private var headerSection: some View {
        ZStack {
            
            Text("SignMate")
                .font(.system(size: titleFontSize, weight: .heavy, design: .rounded))
                .foregroundColor(primaryBlue)
            
            HStack {
                
                Spacer()
                
                settingsButton
                    .padding(.trailing, titleTrailingPadding)
            }
        }
        .padding(.top, titleTopPadding)
    }
    
    private var inputCard: some View {
        ZStack {
            
            RoundedRectangle(cornerRadius: cardCornerRadius)
                .fill(avatarPanelBlue)
                .overlay(
                    RoundedRectangle(cornerRadius: cardCornerRadius)
                        .stroke(primaryBlue.opacity(0.25), lineWidth: 1)
                )
                .shadow(color: primaryBlue.opacity(0.18), radius: 18, x: 0, y: 8)
            
            VStack(spacing: 0) {
                
                inputSection
                
                translateButton
                    .padding(.bottom, buttonBottomPadding)
            }
        }
        .frame(height: cardHeight)
        .padding(.horizontal, cardHorizontalPadding)
        .padding(.top, cardTopPadding)
    }
    
    private var inputSection: some View {
        
        VStack(alignment: .leading, spacing: inputSectionSpacing) {
            
            Text("Enter text")
                .font(.system(size: enterTextFontSize, weight: .medium))
                .foregroundColor(darkBlue.opacity(0.45))
            
            TextEditor(text: $sentence)
                .font(.custom(fontName, size: editorFontSize))
                .frame(height: editorHeight)
                .padding(editorPadding)
                .background(Color.white.opacity(0.8))
                .clipShape(RoundedRectangle(cornerRadius: editorCornerRadius))
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .padding(.horizontal, inputHorizontalPadding)
        .padding(.top, inputSectionTopPadding)
    }
    
    
    private var translateButton: some View {

            Button {
                if cleanedSentence.isEmpty {
                    showEmptyTextAlert = true
                } else {
                    navigateToProcessing = true
                }
            } label: {
                HStack(spacing: 6) {
                    
                    Image(systemName: "arrow.right.circle.fill")
                        .font(.system(size: buttonIconSize))
                    
                    Text("Translate")
                        .font(.custom(fontName, size: labelFontSize))
                }
                .foregroundColor(.white)
                .padding(.horizontal, buttonHorizontalPadding)
                .padding(.vertical, buttonVerticalPadding)
                .background(
                    Capsule()
                        .fill(primaryBlue)
                        .shadow(color: primaryBlue.opacity(0.3), radius: 8, x: 0, y: 4)
                )
            }
        }
    
    
    private var settingsButton: some View {
        
        Button {
            navigateToSettings = true
        } label: {
            Image(systemName: "gearshape.fill")
                .font(.system(size: settingsIconSize, weight: .semibold))
            
                .foregroundColor(primaryBlue)
        }
    }
    private func processInputText() -> [String] {
        
        let words = sentence
            .lowercased()
            .trimmingCharacters(in: .whitespacesAndNewlines)
        
        if let fullMatch = supportedSigns[words] {
            return [fullMatch]
        }
        
        return words
            .split(separator: " ")
            .map { String($0) }
    }
}

#Preview {
    NavigationStack {
        TextTranslateView()
    }
}

