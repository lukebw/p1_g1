using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CheeseTownPhone;
public static class TownAudioSetup
{
    static readonly string[] files = { "“Item Pickup.mp3", "Shop Open.mp3", "UI Button Click.mp3", "upgrads sound(shopping) (1).mp3", "Purchase Failed.mp3", "main music (1).mp3", "fade out music (1).mp3", "giant axe strike hitting solid wood.mp3", "moving on grass (1).mp3", "freesound_community-pick-92276.mp3" };
    static readonly string[] labels = { "Tree cheese drop", "Shop open", "UI click", "Upgrade successful", "Purchase failed", "Main music", "Ending music", "Axe on tree", "Grass movement", "Ground cheese pickup" };
    [MenuItem("Cheese Town/Audio/Configure audio components")]
    public static void Configure()
    {
        AssetDatabase.Refresh();
        foreach (string file in files)
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Audio/"+file);
            var sample=importer.defaultSampleSettings; sample.loadType=AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings=sample; importer.SaveAndReimport();
        }
        var report=new System.Text.StringBuilder();
        foreach (string path in new[] { "Assets/Scenes/Wilderness.unity", "Assets/CheeseTownPhone/CheeseTownPhone/Scenes/CheeseTownPhone.unity" })
        {
            var scene=EditorSceneManager.OpenScene(path);
            var existing=UnityEngine.Object.FindAnyObjectByType<TownAudio>();
            if(existing!=null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var root=new GameObject("Game Audio (editable components)");
            var audio=root.AddComponent<TownAudio>();
            var sources=new AudioSource[files.Length];
            for(int i=0;i<files.Length;i++)
            {
                var child=new GameObject(labels[i]); child.transform.SetParent(root.transform);
                var source=child.AddComponent<AudioSource>();
                source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/"+files[i]);
                if(source.clip==null) throw new Exception("Missing audio: "+files[i]);
                source.spatialBlend=0; source.playOnAwake=false; source.loop=i==5||i==6||i==8;
                source.priority=i==5||i==6?64:128;
                source.volume=BalancedVolume(source.clip,i==5||i==6?.055f:i==8?.035f:.12f);
                var balance=child.AddComponent<AudioBalance>(); balance.targetRms=i==5||i==6?.055f:i==8?.035f:.12f;
                sources[i]=source;
                report.AppendLine(path+" | "+labels[i]+" | volume="+source.volume.ToString("F3")+" | loop="+source.loop);
            }
            audio.treeDrop=sources[0]; audio.shop=sources[1]; audio.uiClick=sources[2]; audio.upgrade=sources[3]; audio.failed=sources[4];
            audio.mainMusic=sources[5]; audio.endingMusic=sources[6]; audio.axe=sources[7]; audio.footsteps=sources[8];
            audio.groundPickup=sources[9];
            ConfigureTreeHits();
            foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None)) AddButton(button);
            EditorSceneManager.SaveScene(scene);
        }
        foreach(string path in new[] { "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/TabletView.prefab", "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/UpgradeRow.prefab" })
        {
            var root=PrefabUtility.LoadPrefabContents(path); var view=root.GetComponent<TabletView>();
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                var sound=AddButton(button); sound.shopButton=view!=null&&(button==view.shopButton||button==view.shopLauncher);
            }
            PrefabUtility.SaveAsPrefabAsset(root,path); PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../Audio-configuration.txt")),report.ToString());
        Debug.Log("TOWN_AUDIO_SETUP_PASS");
    }
    // Editor-only wiring: bind to the weapon hitbox referenced by the cursor.
    // A legacy ChopHitbox on the cursor itself never receives the weapon's contacts.
    public static void ConfigureTreeHits()
    {
        foreach (var cursor in UnityEngine.Object.FindObjectsByType<CursorController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var hitbox = cursor.chopHitbox;
            if (hitbox == null)
                throw new InvalidOperationException("Assign CursorController.chopHitbox before configuring tree-hit audio: " + cursor.name);
            var legacy = cursor.GetComponent<TreeHitAudio>();
            if (legacy != null && legacy.gameObject != hitbox.gameObject)
                UnityEngine.Object.DestroyImmediate(legacy);
            if (hitbox.GetComponent<TreeHitAudio>() == null)
                hitbox.gameObject.AddComponent<TreeHitAudio>();
            hitbox.treeHitAudio = hitbox.GetComponent<TreeHitAudio>();
            EditorUtility.SetDirty(hitbox);
        }
    }
    static UIButtonAudio AddButton(Button button)
    {
        var sound=button.GetComponent<UIButtonAudio>(); if(sound==null) sound=button.gameObject.AddComponent<UIButtonAudio>();
        sound.shopButton=button.name=="Shop button"; return sound;
    }
    static float BalancedVolume(AudioClip clip,float target)
    {
        clip.LoadAudioData(); float[] samples=new float[clip.samples*clip.channels];
        if(!clip.GetData(samples,0)) throw new Exception("Cannot measure "+clip.name);
        double sum=0; float peak=0;
        foreach(float sample in samples){sum+=(double)sample*sample;peak=Mathf.Max(peak,Mathf.Abs(sample));}
        double rms=Math.Sqrt(sum/Math.Max(1,samples.Length));
        return Mathf.Clamp((float)Math.Min(target/Math.Max(.0001,rms),.7/Math.Max(.0001,peak)),.01f,1f);
    }
}
