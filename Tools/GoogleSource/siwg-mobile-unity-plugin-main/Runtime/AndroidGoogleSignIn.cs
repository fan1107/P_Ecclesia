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

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

public static class AndroidGoogleSignIn
{
    private static AndroidJavaClass helperClass;
    private static bool isInitialized = false;

    public static void Initialize(string webClientId)
    {
        try
        {
            helperClass = new AndroidJavaClass("com.google.mobilesiwgunity.GoogleSignInHelper");
            if (helperClass != null)
            {
                helperClass.CallStatic("initialize", webClientId);
                isInitialized = true;
                Debug.Log("[AndroidGoogleSignIn] Initialization successful.");
            }
            else
            {
                Debug.LogError("[AndroidGoogleSignIn] Failed to find GoogleSignInHelper class.");
            }
        }
        catch (System.Exception e) { Debug.LogError($"[AndroidGoogleSignIn] Initialize Error: {e}"); }
    }

    public static void SignIn()
    {
         if (!isInitialized || helperClass == null)
         {
             Debug.LogError("[AndroidGoogleSignIn] Not Initialized. Call Initialize first.");
             // Optionally send an error back to the manager
             var manager = GameObject.Find("GoogleSignInManager");
             manager?.SendMessage("OnSignInError", "AndroidGoogleSignIn not initialized.");
             return;
         }

         try
         {
            // attemptSignIn now handles runOnUiThread internally
            helperClass.CallStatic("attemptSignIn");
         }
         catch (System.Exception e) { Debug.LogError($"[AndroidGoogleSignIn] SignIn Error: {e}"); }
    }
}
#endif
