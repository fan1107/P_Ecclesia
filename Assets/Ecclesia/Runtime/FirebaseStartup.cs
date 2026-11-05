#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using UnityEngine;

namespace Ecclesia
{
    internal static class FirebaseStartup
    {
        private static Task<FirebaseApp> initialization;

        // Analytics and sign-in both request the default app on Unity's main thread.
        internal static Task<FirebaseApp> GetAppAsync() => initialization ??= InitializeAppAsync();

        private static async Task<FirebaseApp> InitializeAppAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException($"Firebase dependency status: {status}");

            return FirebaseApp.DefaultInstance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void StartAnalytics()
        {
            if (!Resources.Load<TextAsset>("EcclesiaFirebaseConfigured"))
                return;

            try
            {
                await GetAppAsync();
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Debug.Log("ECCLESIA: Firebase Analytics initialized.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"ECCLESIA: Firebase Analytics initialization failed. {exception.Message}");
            }
        }
    }
}
#endif
