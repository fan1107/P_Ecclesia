# Mobile Google Sign-In for Unity

A lightweight plugin that bridges the mobile Google Sign-In SDKs to Unity for Android and iOS. This package gives you a simple C# interface to Android's Credential Manager and the iOS GoogleSignIn SDK.

The usage of this plug-in is stricly for returning a googleId Token for authentication. It does not currently support requesting scopes.

Eligibility for the [Google Open Source Software Vulnerability Rewards
Program](https://bughunters.google.com/open-source-security) is determined by the [Google Open Source Software Vulnerability Reward Program Rules](https://bughunters.google.com/about/rules/open-source/google-open-source-software-vulnerability-reward-program-rules).

## Features
- **Modern & Secure:** Uses the latest mobile SDKs, not outdated WebViews.
- **Backend Agnostic:** Get a Google ID Token to authenticate with any service you want (Firebase, Okta, your own backend, etc.).
- **Simple C# Events:** Just subscribe to `OnSignInSuccess` and `OnSignInError` to get the result.
- **Unity 6 Ready:** Built and tested for Unity 6.5+.


---

## Prerequisites & Google Cloud Setup

Before integrating this plugin, you must configure your application identity definitions directly in the Google Cloud Platform (GCP) Console.

### A. GCP Console Configuration

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).  
2. Select your project (or create a new one).  
3. Go to **APIs & Services \> Credentials**.  
4. **Configure the OAuth Consent Screen**: Define your application's public name, support email, and standard authorization scopes (e.g., `openid`, `email`, `profile`).  
5. **Create a Web Client ID (Mandatory)**:  
   - Click **Create Credentials \> OAuth Client ID**.  
   - Select **Web application** as the application type.  
   - Label this credential (e.g., `Server Web Client ID`).  
   - **Copy the resulting Web Client ID** (looks like `123456-abc.apps.googleusercontent.com`). This represents the "Audience" (aud) claim of your ID Tokens and is required for both platforms.

### B. Android Platform Credentials

1. In the GCP Console, click **Create Credentials \> OAuth Client ID** again.  
2. Select **Android** as the application type.  
3. Enter your Unity application's unique **Package Name** (e.g., `com.yourcompany.mygame`).  
4. Paste your SHA-1 fingerprint. Retrieve this by executing `keytool` against your development or release keystore: `bash keytool -keystore path-to-keystore.keystore -list -v`  
5. Note: GCP associates this native Android Client ID with your Web Client ID automatically.

### C. iOS Platform Credentials

1. In the GCP Console, click **Create Credentials \> OAuth Client ID**.  
2. Select **iOS** as the application type.  
3. Enter your matching iOS **Bundle ID** (e.g., `com.yourcompany.mygame`).  
4. Copy the **Reversed Client ID** (looks like `com.googleusercontent.apps.12345...`) listed on the credential details page. This value is required to set up the iOS redirect URL scheme.  
   

---

## 2\. Integration Guide

Choose the path that matches your current development state. Packages for Unity can be found in [Releases](/Releases/). These instructions assume that you have downloaded [`siwg_unity_mobile_plugin.unitypackage`](/Releases/siwg_unity_mobile_plugin.unitypackage) locally.

### Path A: Starting a New Project

1. **Create Project:** Open Unity Hub and create a new project using the **URP (Universal Render Pipeline)** template. **Must use Unity 6 LTS.**  
2. **Switch Platforms:** Go to **File \> Build Profiles**. Select **Android** and click **Switch Platform**. Do the same for **iOS**.  
3. **Import Dependencies:** Ensure the [**External Dependency Manager for Unity (EDM4U)**](https://github.com/googlesamples/unity-jar-resolver#external-dependency-manager-for-unity:~:text=License-,External%20Dependency%20Manager%20for%20Unity,-Overview) is installed in your project to manage native libraries.  
4. **Import This Plugin:** Go to **Assets \> Import Package \> Custom Package** and select [`siwg_unity_mobile_plugin.unitypackage`](/Releases/siwg_unity_mobile_plugin.unitypackage) from the location that you downloaded it. Ensure all files are checked and click **Import**.  
5. **Proceed to Section 3 (Final Configuration).**

---

### Path B: Integrating into an Existing Project

1. **Backup:** Always back up your project before importing new native plugins.  
2. **Prerequisite Check:** Ensure you have [**EDM4U**](https://github.com/googlesamples/unity-jar-resolver#external-dependency-manager-for-unity:~:text=License-,External%20Dependency%20Manager%20for%20Unity,-Overview) installed. If you have an older Google Sign-In plugin, remove it entirely.  
3. **Import This Plugin:** Go to **Assets \> Import Package \> Custom Package** and select the [`siwg_unity_mobile_plugin.unitypackage`](/Releases/siwg_unity_mobile_plugin.unitypackage) from the location that you downloaded it. Ensure all files are checked and click **Import**.
4. **Template Warning:** This plugin includes `baseProjectTemplate.gradle` and `settingsTemplate.gradle`.  
   * If you **do not** already have these templates, they will be added automatically.  
   * If you **already have** custom templates, Unity will not overwrite them. You **must manually merge** the Kotlin 1.9.24 definitions into your existing templates (see Section 3).  
5. **Proceed to Section 3 (Final Configuration).**

---

## 3\. Final Configuration (Required for All)

### A. Plugin Settings

1. Go to **Window \> Google Sign-In Native \> Settings**.  
2. Paste your **Web Client ID** into the first field.  
3. Paste your **iOS Reversed Client ID** into the second field.  
4. Click **Save Settings**.

### B. Dependency Resolution

1. Go to **Assets \> External Dependency Manager \> Android Resolver \> Force Resolve**.  
2. Wait for the success message. This downloads the native `googleid` and `androidx` libraries.

---

## 4\. Usage in Scene

1. **Create Manager:** Create an empty GameObject in your initial scene named `GoogleSignInManager`.  
2. **Attach Script:** Attach the `GoogleSignInManager.cs` script to it.  
3. **Assign Settings:** Drag the `GoogleSignInNativeSettings` asset (from `Assets/GoogleSignInNative/Resources`) into the **Sign In Settings** slot on the script.  
4. **Subscribe to Events & Trigger Sign-In:** Create a controller script to subscribe to `GoogleSignInManager.OnSignInCompleted` and `GoogleSignInManager.OnSignInFailed`, then call `StartSignIn()` when the user clicks your sign-in button:

```c#
using UnityEngine;

public class MyAuthController : MonoBehaviour
{
    private void OnEnable()
    {
        GoogleSignInManager.OnSignInCompleted += HandleSignInSuccess;
        GoogleSignInManager.OnSignInFailed += HandleSignInFailure;
    }

    private void OnDisable()
    {
        GoogleSignInManager.OnSignInCompleted -= HandleSignInSuccess;
        GoogleSignInManager.OnSignInFailed -= HandleSignInFailure;
    }

    public void OnSignInButtonClick()
    {
        GoogleSignInManager.Instance.StartSignIn();
    }

    private void HandleSignInSuccess(string idToken)
    {
        Debug.Log($"[Auth] Success! ID Token received: {idToken.Substring(0, 15)}...");
        // Send this ID token to your backend server or custom authentication provider
    }

    private void HandleSignInFailure(string error)
    {
        Debug.LogError($"[Auth] Google Sign-in failed: {error}");
    }
}

```
---

## 5\. Building the App

### Android

* Ensure your **Package Name** in Player Settings matches your GCP OAuth Credentials exactly.  
* Ensure your **Minimum API Level** is 34 or higher.  
* Click **Build and Run**.

### iOS

* Unity will generate an Xcode project. Open the `.xcworkspace` file.  
* In the **Signing & Capabilities** tab, select your Development Team.  
* The plugin automatically adds the URL Scheme for you. Run on a physical device.


### FAQ

* In GoogleSignInNativeAndroid.androidilb > [gradleTemplate.properties](Plugins/Android/gradleTemplate.properties) you may need to append `android.builtInKotlin=true` if your project has `android.builtInKotlin=false`. You can check this by running `grep -i kotlin gradle.properties` under YOUR_UNITY_PROJECT/Library/Bee/Android/Prj/IL2CPP/Gradle. Kotlin was not enabled by default on versions of Unity older that 6.5. This is due to Unity 6.5 updating to Android Gradle Plugin (AGP) 9.x.x. 

* In GoogleSignInNativeAndroid.androidlib > [build.gradle](Plugins/Android/GoogleSignInNativeAndroid.androidlib/build.gradle) the following block of code is used to ensure that we do not have Kotlin version conflicts when building the .androidlib:

```gradle
kotlin {
    compilerOptions {
        freeCompilerArgs.add("-Xskip-metadata-version-check")
    }
}
```
 This is because the [com.google.android.libraries.identity.googleid Library](https://maven.google.com/web/index.html#com.google.android.libraries.identity.googleid:googleid:1.2.1) used was built with Kotlin 2.4.x while the default for Unity is 2.1.0. This block can be removed once Unity updates to a default Kotlin version of 2.4.x or greater.

## Support & Security Disclaimers
This project is not intended for active development. Issues and contributions are handled on a best effort basis. This open source solution is intended to enhance the Google customer experience. For product support, please see [support.google.com](https://support.google.com).
Eligibility for the [Google Open Source Software Vulnerability Rewards Program](https://bughunters.google.com/open-source-security) is determined by the [Google Open Source Software Vulnerability Reward Program Rules](https://bughunters.google.com/about/rules/open-source/google-open-source-software-vulnerability-reward-program-rules).
