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
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.forceInternetPermission=true;
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
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
        [Serializable] class ClientInfo { public AndroidInfo android_client_info; }
        [Serializable] class AndroidInfo { public string package_name; }
        [Serializable] class OAuth { public int client_type; public string client_id; }
        [MenuItem("Ecclesia/2. Configure Firebase from google-services.json")]
        public static void ConfigureFirebase()
        {
            const string marker="Assets/Ecclesia/Resources/EcclesiaFirebaseConfigured.txt";
            if(File.Exists(marker))AssetDatabase.DeleteAsset(marker);
            if(!File.Exists("Assets/google-services.json")){Debug.Log("ECCLESIA: Firebase console configuration pending; offline mode is available.");return;}
            var json=JsonUtility.FromJson<Config>(File.ReadAllText("Assets/google-services.json"));
            var client=json.client?.FirstOrDefault(c=>c.client_info?.android_client_info?.package_name==PackageName);
            var web=client?.oauth_client?.FirstOrDefault(c=>c.client_type==3)?.client_id;
            if(string.IsNullOrWhiteSpace(web))throw new InvalidOperationException("google-services.json must contain com.ecclesia.prayer and a Web OAuth client (type 3). Enable Google in Firebase Authentication and download again.");
            var settings=Resources.Load<GoogleSignInNativeSettingsSO>("GoogleSignInNativeSettings");
            if(!settings)throw new InvalidOperationException("Google sign-in settings asset missing.");
            settings.webClientId=web;EditorUtility.SetDirty(settings);
            File.WriteAllText(marker,"Firebase configuration present. This is not proof of successful sign-in.");
            AssetDatabase.ImportAsset(marker);AssetDatabase.SaveAssets();
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
            Require(world.ResolveTouch(new Vector2(16.5f,14))==world.Door,"Visible door does not map to entrance");
            world.Build(true);Require(world.Seats.Count==20,"Expected 20 seats");
            foreach(var seat in world.Seats.Keys)
            {
                var path=world.Navigation.FindPath(world.Spawn,seat);Require(path!=null,"Seat unreachable: "+seat);
                Require(path.All(world.Navigation.IsOpen),"Path crosses obstacle");
                Require(world.Navigation.FindPath(seat,world.Door)!=null,"Seat cannot reach exit");
            }
            Require(world.Navigation.FindPath(world.Spawn,new Vector2Int(7,4))==null,"Pew back is walkable");
            Require(world.Seats.ContainsKey(world.ResolveTouch(new Vector2(8.5f,7.9f))),"Pew touch does not select a seat");
            Require(world.Navigation.FindPath(world.Spawn,new Vector2Int(-1,5))==null,"Outside map is walkable");
            UnityEngine.Object.DestroyImmediate(go);
            File.WriteAllText("Builds/Validation/navigation.txt","PASS: courtyard entrance, all 20 seats, all 20 exit routes, pew and boundary rejection.");
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
