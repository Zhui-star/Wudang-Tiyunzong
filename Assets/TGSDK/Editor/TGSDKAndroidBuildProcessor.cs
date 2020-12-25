using UnityEditor;
using UnityEditor.Android;
using UnityEngine;
using System.IO;

public class TGSDKAndroidBuildProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder
    {
    	// 同种插件的优先级
        get { return 999; }
    }
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log("Bulid path : " + path);
#if UNITY_2019_3_OR_NEWER
        string gradlePropertiesFile = path + "/../gradle.properties";
#else
        string gradlePropertiesFile = path + "/gradle.properties";
#endif
        if (File.Exists(gradlePropertiesFile))
        {
            File.Delete(gradlePropertiesFile);
        }
        StreamWriter writer = File.CreateText(gradlePropertiesFile);
        writer.WriteLine("org.gradle.jvmargs=-Xmx4096M");
        writer.WriteLine("android.useAndroidX=true");
        writer.WriteLine("android.enableJetifier=true");
        writer.Flush();
        writer.Close();
    }
}
