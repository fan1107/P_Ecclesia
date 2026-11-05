using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecclesia.EditorTools
{
    public static class ProjectSetup
    {
        public const string ScenePath="Assets/Ecclesia/Scenes/Ecclesia.unity";
        public const string PackageName="com.ecclesia.prayer";
        [MenuItem("Ecclesia/1. Prepare Android and Scene")]
        public static void Prepare()
        {
            PlayerSettings.companyName="Ecclesia";PlayerSettings.productName="에끌레시아";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,PackageName);
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.forceInternetPermission=true;
            PlayerSettings.defaultScreenWidth=720;PlayerSettings.defaultScreenHeight=1280;
            PlayerSettings.runInBackground=false;
            if(!File.Exists(ScenePath))
            {
                // Additive scene creation preserves the user's open scene and any unsaved edits.
                var old=SceneManager.GetActiveScene();
                bool batchEmpty=Application.isBatchMode&&string.IsNullOrEmpty(old.path);
                if(!batchEmpty&&string.IsNullOrEmpty(old.path)&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,batchEmpty?NewSceneMode.Single:NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                new GameObject("Ecclesia").AddComponent<EcclesiaApp>();
                EditorSceneManager.SaveScene(scene,ScenePath);
                if(!batchEmpty){SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            ConfigureFirebase();AssetDatabase.SaveAssets();
            Debug.Log("ECCLESIA: Android settings and scene prepared.");
        }
        [Serializable] class Config { public Client[] client; }
        [Serializable] class Client { public ClientInfo client_info; public OAuth[] oauth_client; }
        [Serializable] class ClientInfo { public AndroidInfo android_client_info; public string mobilesdk_app_id; }
        [Serializable] class AndroidInfo { public string package_name; }
        [Serializable] class OAuth { public int client_type; public string client_id; }
        [MenuItem("Ecclesia/2. Configure Firebase from google-services.json")]
        public static void ConfigureFirebase()
        {
            const string marker="Assets/Ecclesia/Resources/EcclesiaFirebaseConfigured.txt";
            if (!File.Exists("Assets/google-services.json"))
            {
                if (File.Exists(marker)) AssetDatabase.DeleteAsset(marker);
                Debug.Log("ECCLESIA: Firebase console configuration pending; offline mode is available.");
                return;
            }
            var web = ReadWebClientId(File.ReadAllText("Assets/google-services.json"));
            var settings=Resources.Load<GoogleSignInNativeSettingsSO>("GoogleSignInNativeSettings");
            if (settings && settings.webClientId != web)
            {
                settings.webClientId = web;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            if (!File.Exists(marker))
            {
                File.WriteAllText(marker, "Firebase Android configuration present.");
                AssetDatabase.ImportAsset(marker);
            }
            if (!settings || string.IsNullOrWhiteSpace(web))
                Debug.Log("ECCLESIA: Firebase configured for Analytics. Google sign-in requires a Web OAuth client and sign-in settings.");
        }
        private static string ReadWebClientId(string configText)
        {
            var config = JsonUtility.FromJson<Config>(configText);
            var client = config?.client?.FirstOrDefault(c => c?.client_info?.android_client_info?.package_name == PackageName);
            if (string.IsNullOrWhiteSpace(client?.client_info?.mobilesdk_app_id))
                throw new BuildFailedException($"google-services.json must contain the Firebase Android app ID for {PackageName}.");

            return client.oauth_client?.FirstOrDefault(c => c?.client_type == 3)?.client_id ?? "";
        }
        [MenuItem("Ecclesia/3. Open Prototype")]
        public static void Open(){Prepare();if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath);}
        [MenuItem("Ecclesia/4. Build Android APK")]
        public static void BuildAndroid()
        {
            Prepare();Directory.CreateDirectory("Builds/Android");
            EditorUserBuildSettings.buildAppBundle=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Android/Ecclesia-prototype.apk",target=BuildTarget.Android,options=BuildOptions.Development});
            File.WriteAllText("Builds/Android/result.txt",report.summary.result+" errors="+report.summary.totalErrors);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed; see editor log.");
        }
        public static void Validate()
        {
            Prepare();Directory.CreateDirectory("Builds/Validation");
            var go=new GameObject("Navigation validation");var world=go.AddComponent<ChapelWorld>();
            world.Build(false);Require(world.Navigation.FindPath(world.Spawn,world.Door)!=null,"Courtyard door unreachable");
            Require(world.ResolveTouch(new Vector2(9.5f,20))==world.Door,"Visible door does not map to entrance");
            world.Build(true);Require(world.Seats.Count==20,"Expected 20 seats");
            foreach(var seat in world.Seats.Keys)
            {
                var path=world.Navigation.FindPath(world.Spawn,seat);Require(path!=null,"Seat unreachable: "+seat);
                Require(path.All(world.Navigation.IsOpen),"Path crosses obstacle");
                Require(world.Navigation.FindPath(seat,world.Door)!=null,"Seat cannot reach exit");
            }
            Require(world.Navigation.FindPath(world.Spawn,new Vector2Int(4,8))!=null,"Pew area must be freely walkable");
            Require(world.ResolveTouch(new Vector2(5.5f,7.9f))==new Vector2Int(5,7),"Touch must not snap to a seat");
            Require(world.IsNearPew(new Vector2(5.5f,8.5f)),"Between-pew prayer unavailable");
            Require(!world.IsNearPew(new Vector2(9.5f,4.5f)),"Entrance must not enable prayer");
            Require(world.Navigation.FindPath(world.Spawn,new Vector2Int(-1,5))==null,"Outside map is walkable");
            UnityEngine.Object.DestroyImmediate(go);
            File.WriteAllText("Builds/Validation/navigation.txt","PASS: courtyard entrance, all 20 seats, all 20 exit routes, free pew movement, proximity prayer and boundary rejection.");
            Debug.Log("ECCLESIA_VALIDATION_PASS");
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    }
    public sealed class FirebaseBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report){if(report.summary.platform==BuildTarget.Android)ProjectSetup.ConfigureFirebase();}
    }
}
