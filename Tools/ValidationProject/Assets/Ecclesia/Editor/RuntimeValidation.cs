using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Ecclesia.EditorTools
{
    [InitializeOnLoad]
    public static class RuntimeValidation
    {
        static double started;
        static int stage;
        static RuntimeValidation(){EditorApplication.update+=Tick;started=EditorApplication.timeSinceStartup;}
        public static void Run()
        {
            ProjectSetup.Validate();EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool("Ecclesia.RuntimeValidation",true);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool("Ecclesia.RuntimeValidation",false)||!EditorApplication.isPlaying)return;
            try
            {
                if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Runtime flow timeout at stage "+stage);
                var app=UnityEngine.Object.FindFirstObjectByType<EcclesiaApp>();
                var visitor=UnityEngine.Object.FindFirstObjectByType<Visitor>(FindObjectsInactive.Include);
                var world=UnityEngine.Object.FindFirstObjectByType<ChapelWorld>();
                if(!app||!visitor||!world)return;
                if(stage==0 && EditorApplication.timeSinceStartup-started>3)
                {
                    Capture("01-login");app.EnterOffline();stage=1;
                }
                else if(stage==1)
                {
                    Capture("02-courtyard");if(!app.RequestMove(world.Door))throw new Exception("Door move rejected");stage=2;
                }
                else if(stage==2 && world.Indoors)
                {
                    if(!app.RequestMove(new Vector2Int(7,7)))throw new Exception("Seat move rejected");stage=3;
                }
                else if(stage==3 && !visitor.Moving)
                {
                    if(visitor.Cell!=new Vector2Int(7,7))throw new Exception("Seat arrival mismatch");
                    app.BeginPrayer();if(!visitor.Praying)throw new Exception("Prayer did not start");stage=4;
                }
                else if(stage==4)
                {
                    Capture("03-prayer");
                    app.RequestMove(world.Door);if(visitor.Praying)throw new Exception("Prayer did not stop on movement");stage=5;
                }
                else if(stage==5 && !world.Indoors)
                {
                    Directory.CreateDirectory("Builds/Validation");File.WriteAllText("Builds/Validation/runtime.txt","PASS: welcome, offline entry, courtyard path, doorway transition, seat arrival, prayer light, movement ends prayer, exit transition.");
                    SessionState.SetBool("Ecclesia.RuntimeValidation",false);Debug.Log("ECCLESIA_RUNTIME_PASS");EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetBool("Ecclesia.RuntimeValidation",false);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory("Builds/Validation");
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;canvas.sortingOrder=1000;
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Builds/Validation/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(rt);
        }
    }
}
