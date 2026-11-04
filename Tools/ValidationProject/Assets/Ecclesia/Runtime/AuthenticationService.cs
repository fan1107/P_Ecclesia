using System;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace Ecclesia
{
    public sealed class AuthenticationService : MonoBehaviour
    {
        public event Action Changed;
        public bool Ready { get; private set; }
        public bool Busy { get; private set; }
        public bool SignedIn => auth != null && auth.CurrentUser != null;
        public string UserId => SignedIn ? auth.CurrentUser.UserId : null;
        public string Status { get; private set; } = "Firebase 설정 전 · 오프라인 둘러보기를 이용할 수 있습니다.";
        FirebaseAuth auth;
        GoogleSignInManager google;
        bool alive = true;
        async void Start()
        {
            if (!Resources.Load<TextAsset>("EcclesiaFirebaseConfigured")) { Changed?.Invoke(); return; }
#if !UNITY_ANDROID || UNITY_EDITOR
            Status="Google 로그인은 Android 실기기에서 확인해주세요."; Changed?.Invoke();
            return;
#else
            try
            {
                var settings=Resources.Load<GoogleSignInNativeSettingsSO>("GoogleSignInNativeSettings");
                if(!settings || string.IsNullOrWhiteSpace(settings.webClientId) || settings.webClientId.Contains("YOUR_"))
                    throw new InvalidOperationException("Google Web Client ID 설정이 필요합니다.");
                Busy=true;Status="로그인 준비 중…";Changed?.Invoke();
                var dependency=await FirebaseApp.CheckAndFixDependenciesAsync();
                if(!alive)return;
                if(dependency!=DependencyStatus.Available)throw new InvalidOperationException("Google Play 서비스와 Firebase 의존성을 확인해주세요.");
                auth=FirebaseAuth.DefaultInstance;
                GoogleSignInManager.OnSignInSuccess+=OnGoogleSuccess;
                GoogleSignInManager.OnSignInError+=OnGoogleError;
                google=new GameObject("GoogleSignInManager").AddComponent<GoogleSignInManager>();
                Ready=true;Busy=false;Status=SignedIn?"로그인되어 있습니다. 예배당에 들어갈 수 있습니다.":"Google 계정으로 로그인해주세요.";
                Changed?.Invoke();
            }
            catch(Exception){Busy=false;Status="로그인 초기화 실패 · Firebase 설정과 연결을 확인해주세요.";Changed?.Invoke();}
#endif
        }
        public void SignIn()
        {
            if(!Ready||Busy)return;
            Busy=true;Status="Google 계정 선택을 기다리고 있습니다…";Changed?.Invoke();
            try { google.StartSignIn(); } catch(Exception) { OnGoogleError(""); }
        }
        async void OnGoogleSuccess(string token)
        {
            if(!Busy || auth==null)return;
            try
            {
                using(var credential=GoogleAuthProvider.GetCredential(token,null))
                    await auth.SignInWithCredentialAsync(credential);
                if(!alive)return;
                Busy=false;Status="로그인 완료 · 예배당에 들어가세요.";Changed?.Invoke();
            }
            catch(Exception){if(alive)OnGoogleError("");}
        }
        void OnGoogleError(string ignored)
        {
            Busy=false;Status="로그인이 취소되었거나 실패했습니다. 다시 시도할 수 있습니다.";Changed?.Invoke();
        }
        public void SignOut()
        {
            if(Busy)return;auth?.SignOut();Status="로그아웃했습니다.";Changed?.Invoke();
        }
        void OnDestroy()
        {
            alive=false;GoogleSignInManager.OnSignInSuccess-=OnGoogleSuccess;GoogleSignInManager.OnSignInError-=OnGoogleError;
            if(google)Destroy(google.gameObject);
        }
    }
}
