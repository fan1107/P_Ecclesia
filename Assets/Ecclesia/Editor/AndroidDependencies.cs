using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace Ecclesia.EditorTools
{
    // Reproducible dependencies for this Android-only prototype. Reads the SDK's own manifests.
    // Works even before an interactive EDM4U Force Resolve has been run.
    public sealed class AndroidDependencies : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder=>1000;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Unity 6000.3.28 uses AGP 9 but disables its built-in Kotlin by default.
            // The official sign-in library contains Kotlin sources and uses that DSL.
            var properties=Path.Combine(Directory.GetParent(path).FullName,"gradle.properties");
            var propertyText=File.ReadAllText(properties).Replace("android.builtInKotlin=false","android.builtInKotlin=true");
            File.WriteAllText(properties,propertyText);
            var specs=new[]{"Assets/Firebase/Editor/AppDependencies.xml","Assets/Firebase/Editor/AuthDependencies.xml","Assets/Firebase/Editor/AnalyticsDependencies.xml","Assets/GoogleSignInNative/Editor/GoogleSignInDependencies.xml"}
                .SelectMany(file=>XDocument.Load(file).Descendants("androidPackage").Select(e=>(string)e.Attribute("spec"))).Distinct();
            var gradle=Path.Combine(path,"build.gradle");
            const string marker="// Ecclesia pinned Android dependencies";
            var gradleText = File.ReadAllText(gradle);
            var missing = specs.Where(spec => !gradleText.Contains("'" + spec + "'")).ToArray();
            if (missing.Length > 0)
                File.AppendAllText(gradle,"\n"+marker+"\ndependencies {\n"+string.Join("\n",missing.Select(s=>"    implementation '"+s+"'"))+"\n}\n");
            var source=Path.GetFullPath("Assets/Firebase/m2repository");
            var repository=Path.Combine(Directory.GetParent(path).FullName,"ecclesiaMaven");
            foreach(var file in Directory.GetFiles(source,"*",SearchOption.AllDirectories).Where(f=>f.EndsWith(".pom")||f.EndsWith(".srcaar")))
            {
                var relative=file.Substring(source.Length+1);if(relative.EndsWith(".srcaar"))relative=Path.ChangeExtension(relative,".aar");
                var target=Path.Combine(repository,relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (file.EndsWith(".pom"))
                    File.WriteAllText(target, File.ReadAllText(file).Replace("<packaging>srcaar</packaging>", "<packaging>aar</packaging>"));
                else
                    File.Copy(file,target,true);
            }
            var settings=Path.Combine(Directory.GetParent(path).FullName,"settings.gradle");
            var content=File.ReadAllText(settings);
            if(!content.Contains(marker))File.AppendAllText(settings,"\n"+marker+"\ndependencyResolutionManagement { repositories { maven { url = uri('"+repository.Replace('\\','/')+"') } } }\n");
            Debug.Log("ECCLESIA: Firebase and Google Android dependencies added to generated Gradle project.");
        }
    }
}
