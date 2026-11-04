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
using System;
using TMPro;

public class GoogleSignInManager : MonoBehaviour
{
    public GoogleSignInNativeSettingsSO signInSettings;
    public TextMeshProUGUI statusText;

    public static event Action<string> OnSignInSuccess;
    public static event Action<string> OnSignInError;

    private static GoogleSignInManager _instance;
    public static GoogleSignInManager Instance => _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (signInSettings == null)
            signInSettings = Resources.Load<GoogleSignInNativeSettingsSO>("GoogleSignInNativeSettings");

        if (signInSettings == null || string.IsNullOrEmpty(signInSettings.webClientId) || signInSettings.webClientId.Contains("YOUR_"))
        {
            UpdateStatus("ERROR: Plugin not configured. Go to Window > Google Sign-In Native > Settings.", true);
            return;
        }
    }

    void Start()
    {
        Debug.Log("[GoogleSignInManager] Starting Google Sign-In Manager INIT...");
        InitializeNativeSignIn();
        UpdateStatus("Ready to Sign In", false);
    }
    
    void UpdateStatus(string message, bool isError)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.black;
        }
        if (isError) Debug.LogError("[GoogleSignInManager] " + message);
        else Debug.Log("[GoogleSignInManager] " + message);
    }

    void InitializeNativeSignIn()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGoogleSignIn.Initialize(signInSettings.webClientId);

#elif UNITY_IOS && !UNITY_EDITOR
        iOSGoogleSignIn.Initialize(signInSettings.iosClientId);

#endif
    }

    public void StartSignIn()
    {
        UpdateStatus("Starting Sign In...", false);
#if UNITY_EDITOR
        HandleSignInError("Native Google Sign-In not supported in Editor.");
#elif UNITY_ANDROID
        AndroidGoogleSignIn.SignIn();
#elif UNITY_IOS
        if (string.IsNullOrEmpty(signInSettings.iosClientId) || signInSettings.iosClientId.Contains("YOUR_"))
        {
            HandleSignInError("iOS Client ID not set in settings.");
            return;
        }
        iOSGoogleSignIn.SignIn();
#else
        HandleSignInError("Platform not supported.");
#endif
    }

    // Called from native code
    public void HandleSignInSuccess(string idToken)
    {
        UpdateStatus($"Native sign-in success. ID Token received.", false);
        OnSignInSuccess?.Invoke(idToken);
    }

    // Called from native code
    public void HandleSignInError(string errorMessage)
    {
        UpdateStatus($"Error: {errorMessage}", true);
        OnSignInError?.Invoke(errorMessage);
    }
}