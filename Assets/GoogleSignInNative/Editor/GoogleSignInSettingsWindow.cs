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

using UnityEditor;
using UnityEngine;
using System.IO;

public class GoogleSignInSettingsWindow : EditorWindow
{
    [MenuItem("Window/Google Sign-In Native/Settings")]
    public static void ShowWindow()
    {
        GetWindow<GoogleSignInSettingsWindow>("GSI Native Settings");
    }

    private GoogleSignInNativeSettingsSO settings;
    private const string settingsPath = "Assets/GoogleSignInNative/Resources";
    private const string settingsAssetName = "GoogleSignInNativeSettings.asset";

    private void OnEnable() => LoadSettings();

    void LoadSettings()
    {
        string fullPath = Path.Combine(settingsPath, settingsAssetName);
        settings = AssetDatabase.LoadAssetAtPath<GoogleSignInNativeSettingsSO>(fullPath);

        if (settings == null)
        {
            if (!Directory.Exists(settingsPath)) Directory.CreateDirectory(settingsPath);
            settings = ScriptableObject.CreateInstance<GoogleSignInNativeSettingsSO>();
            AssetDatabase.CreateAsset(settings, fullPath);
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        if (settings == null) { EditorGUILayout.HelpBox("Settings asset not found.", MessageType.Error); return; }

        EditorGUILayout.LabelField("Google Sign-In Native Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Configure the Google OAuth Client IDs for the plugin.", MessageType.Info);
        EditorGUILayout.Space();

        settings.webClientId = EditorGUILayout.TextField(new GUIContent("Web Client ID set for Android", "Client ID of type 'Web application' from Google Cloud Console"), settings.webClientId);
        EditorGUILayout.Space();
        settings.iosClientId = EditorGUILayout.TextField(new GUIContent("iOS Client ID", "Client ID from GoogleService-Info.plist (GIDClientID)"), settings.iosClientId);
        settings.iosReversedClientId = EditorGUILayout.TextField(new GUIContent("iOS Reversed Client ID", "The REVERSED_CLIENT_ID from your GoogleService-Info.plist for the iOS URL Scheme"), settings.iosReversedClientId);

        EditorGUILayout.Space();
        if (GUILayout.Button("Save Settings"))
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("Google Sign-In Native settings saved.");
        }
    }
}
