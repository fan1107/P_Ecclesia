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

#if UNITY_IOS && UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using UnityEngine;
using System.Linq;

public class iOSBuildConfig
{
    [PostProcessBuild(100)] // Run somewhat late
    public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            string plistPath = Path.Combine(path, "Info.plist");
            Debug.Log($"[iOSBuildConfig] Modifying Info.plist at: {plistPath}");

            if (!File.Exists(plistPath))
            {
                Debug.LogError($"[iOSBuildConfig] Info.plist not found at path: {plistPath}");
                return;
            }

            PlistDocument plist = new PlistDocument();
            try
            {
                plist.ReadFromFile(plistPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[iOSBuildConfig] Error reading Info.plist: {e.Message}");
                return;
            }

            PlistElementDict rootDict = plist.root;

            var settings = Resources.Load<GoogleSignInNativeSettingsSO>("GoogleSignInNativeSettings");
            if (settings == null || string.IsNullOrEmpty(settings.iosReversedClientId) || settings.iosReversedClientId.Contains("YOUR_"))
            {
                Debug.LogError("[iOSBuildConfig] iOS Reversed Client ID not set in Google Sign-In Native Settings!");
                return;
            }
            string reversedClientId = settings.iosReversedClientId;
            Debug.Log($"[iOSBuildConfig] Using Reversed Client ID: {reversedClientId}");

            // Get or create CFBundleURLTypes array
            PlistElementArray urlTypesArray;
            if (rootDict.values.ContainsKey("CFBundleURLTypes"))
            {
                urlTypesArray = rootDict["CFBundleURLTypes"].AsArray();
                Debug.Log("[iOSBuildConfig] Found existing CFBundleURLTypes.");
            }
            else
            {
                urlTypesArray = rootDict.CreateArray("CFBundleURLTypes");
                Debug.Log("[iOSBuildConfig] Created new CFBundleURLTypes array.");
            }

            // Check if our URL type already exists
            PlistElementDict existingDict = urlTypesArray.values
                .Select(item => item.AsDict())
                .FirstOrDefault(dict => dict.values.ContainsKey("CFBundleURLName") && dict["CFBundleURLName"].AsString() == reversedClientId);

            if (existingDict == null)
            {
                PlistElementDict googleSignInUrlType = urlTypesArray.AddDict();
                googleSignInUrlType.SetString("CFBundleTypeRole", "Editor");
                googleSignInUrlType.SetString("CFBundleURLName", reversedClientId);
                PlistElementArray googleSignInSchemes = googleSignInUrlType.CreateArray("CFBundleURLSchemes");
                googleSignInSchemes.AddString(reversedClientId);
                Debug.Log($"[iOSBuildConfig] Added new URL Scheme: {reversedClientId}");
            }
            else
            {
                Debug.Log($"[iOSBuildConfig] Found existing URL type for {reversedClientId}. Checking schemes...");
                PlistElementArray schemesArray = existingDict.CreateArray("CFBundleURLSchemes"); // Gets or creates
                if (!schemesArray.values.Any(el => el.AsString() == reversedClientId))
                {
                    schemesArray.AddString(reversedClientId);
                    Debug.Log($"[iOSBuildConfig] Added scheme {reversedClientId} to existing URL type.");
                }
                else
                {
                    Debug.Log($"[iOSBuildConfig] Scheme {reversedClientId} already present.");
                }
            }

            try
            {
                File.WriteAllText(plistPath, plist.WriteToString());
                Debug.Log("[iOSBuildConfig] Successfully wrote changes to Info.plist.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[iOSBuildConfig] Error writing Info.plist: {e.Message}");
            }
        }
    }
}
#endif
