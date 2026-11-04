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

package com.google.mobilesiwgunity

import android.app.Activity
import android.util.Log
import androidx.credentials.CredentialManager
import androidx.credentials.CredentialManagerCallback
import androidx.credentials.CustomCredential
import androidx.credentials.GetCredentialRequest
import androidx.credentials.GetCredentialResponse
import androidx.credentials.exceptions.GetCredentialException
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import com.unity3d.player.UnityPlayer
import java.security.MessageDigest
import java.util.UUID
import java.util.concurrent.Executor
import java.util.concurrent.Executors

object GoogleSignInHelper {

    private const val TAG = "GoogleSignInHelper"
    private const val UNITY_OBJECT = "GoogleSignInManager"
    private const val BUNDLE_KEY_CALLER_ID = "com.google.android.libraries.identity.googleid.BUNDLE_KEY_CALLER_ID"
    private const val CALLER_ID = "siwg_unity_plugin"

    private var webClientId: String = ""
    private var lastNonce: String = ""

    // One thread for the whole plug-in. Creating an executor per call leaks a
    // non-daemon thread on every sign-in attempt.
    private val callbackExecutor: Executor = Executors.newSingleThreadExecutor()

    @JvmStatic
    fun initialize(clientId: String) {
        webClientId = clientId
    }

    /** The nonce sent with the last request, so C# can verify the token's `nonce` claim. */
    @JvmStatic
    fun getLastNonce(): String = lastNonce

    @JvmStatic
    fun attemptSignIn() {
        val activity = UnityPlayer.currentActivity
        if (activity == null) {
            send("HandleSignInError", "No current activity")
            return
        }
        activity.runOnUiThread { signIn(activity) }
    }

    private fun signIn(activity: Activity) {
        lastNonce = sha256(UUID.randomUUID().toString())

        val option = GetGoogleIdOption.Builder()
            .setServerClientId(webClientId)
            .setNonce(lastNonce)
            .setFilterByAuthorizedAccounts(false)
            .build()

        option.requestData.putString(BUNDLE_KEY_CALLER_ID, CALLER_ID)
        option.candidateQueryData.putString(BUNDLE_KEY_CALLER_ID, CALLER_ID)
        
        Log.d(TAG, "[GoogleSignInHelper]Caller ID in requestData set to: " + option.requestData.getString(BUNDLE_KEY_CALLER_ID))
        Log.d(TAG, "[GoogleSignInHelper]Caller ID in candidateQueryData set to: " + option.candidateQueryData.getString(BUNDLE_KEY_CALLER_ID))

        val request = GetCredentialRequest.Builder()
            .addCredentialOption(option)
            .build()

        CredentialManager.create(activity).getCredentialAsync(
            activity,
            request,
            null,                                  // CancellationSignal
            callbackExecutor,
            object : CredentialManagerCallback<GetCredentialResponse, GetCredentialException> {
                override fun onResult(result: GetCredentialResponse) {
                    val credential = result.credential
                    if (credential is CustomCredential &&
                        credential.type == GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL) {
                        val idToken = GoogleIdTokenCredential.createFrom(credential.data).idToken
                        Log.d(TAG, "[GoogleSignInHelper]Google ID token retrieved.")
                        send("HandleSignInSuccess", idToken)
                    } else {
                        send("HandleSignInError", "Unexpected credential type: ${credential.type}")
                    }
                }

                override fun onError(e: GetCredentialException) {
                    Log.e(TAG, "[GoogleSignInHelper]GetCredentialException: ${e.type}")
                    send("HandleSignInError", e.message ?: e.type)
                }
            }
        )
    }

    private fun send(method: String, payload: String) =
        UnityPlayer.UnitySendMessage(UNITY_OBJECT, method, payload)

    private fun sha256(raw: String): String =
        MessageDigest.getInstance("SHA-256")
            .digest(raw.toByteArray())
            .joinToString("") { "%02x".format(it) }
}