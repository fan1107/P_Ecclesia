/* 
Copyright 2026 Google LLC

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UnityEngine;

[CreateAssetMenu(fileName = "GoogleSignInNativeSettings", menuName = "GoogleSignInNative/Settings", order = 1)]
public class GoogleSignInNativeSettingsSO : ScriptableObject
{
    [Header("Google Cloud OAuth 2.0 Client ID")]
    [Tooltip("The Client ID of type 'Web application' from your Google Cloud Console, used for both Android and iOS.")]
    public string webClientId = "YOUR_WEB_CLIENT_ID.apps.googleusercontent.com";

    [Header("iOS Configuration")]
    [Tooltip("The Client ID of type 'iOS application' from Google Cloud Console / GoogleService-Info.plist (GIDClientID).")]
    public string iosClientId = "YOUR_IOS_CLIENT_ID.apps.googleusercontent.com";

    [Header("iOS URL Scheme Configuration")]
    [Tooltip("The REVERSED_CLIENT_ID from your GoogleService-Info.plist. This is essential for the iOS redirect.")]
    public string iosReversedClientId = "com.googleusercontent.apps.YOUR_REVERSED_ID";
}
