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

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;

public static class iOSGoogleSignIn
{
    [DllImport("__Internal")]
    private static extern void _IOSGoogleSignIn_Initialize(string iosClientId);

    [DllImport("__Internal")]
    private static extern void _IOSGoogleSignIn_SignIn();

    public static void Initialize(string iosClientId)
    {
        try { _IOSGoogleSignIn_Initialize(iosClientId); }
        catch (System.Exception e) { Debug.LogError($"[iOSGoogleSignIn] Initialize Error: {e.Message}"); }
    }

    public static void SignIn()
    {
        try { _IOSGoogleSignIn_SignIn(); }
        catch (System.Exception e) { Debug.LogError($"[iOSGoogleSignIn] SignIn Error: {e.Message}"); }
    }
}
#endif
