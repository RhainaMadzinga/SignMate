//
//  UnityBridge.swift
//  SignMate
//
//  Created by Rhaina
//

import Foundation

final class UnityBridge {

    static let shared = UnityBridge()

    private init() {}

    func sendAvatarPayload(_ payloadJSON: String) {
        /*
         Unity integration point.

         Once UnityFramework is embedded into the Xcode project, this method
         will send the payload to a Unity GameObject using UnitySendMessage.

         Target Unity GameObject:
         SignMateUnityReceiver

         Target Unity method:
         ReceiveAvatarPayload
         */

        print("Sending avatar payload to Unity bridge:")
        print(payloadJSON)

        // This notification allows the app to confirm the bridge was triggered.
        NotificationCenter.default.post(
            name: .unityAvatarPayloadReady,
            object: payloadJSON
        )
    }
}

extension Notification.Name {
    static let unityAvatarPayloadReady = Notification.Name("unityAvatarPayloadReady")
}
