using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.Rendering;

public static class SetupTripoAdventurer
{
    const string Dir="Assets/Characters/TripoAdventurer";
    const string Model=Dir+"/Adventurer_Rigged.fbx";
    public static object Import()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        importer.animationType=ModelImporterAnimationType.Human;
        importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=true;
        importer.animationCompression=ModelImporterAnimationCompression.Off;
        importer.skinWeights=ModelImporterSkinWeights.Standard;
        importer.importBlendShapes=false;
        importer.SaveAndReimport();
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var hd=importer.humanDescription;
        hd.human=HumanTrait.BoneName.Where(n=>asset.GetComponentsInChildren<Transform>(true).Any(t=>t.name==n.Replace(" ","")))
            .Select(n=>new HumanBone{humanName=n,boneName=n.Replace(" ",""),limit=new HumanLimit{useDefaultValues=true}}).ToArray();
        hd.upperArmTwist=.5f;hd.lowerArmTwist=.5f;hd.upperLegTwist=.5f;hd.lowerLegTwist=.5f;hd.armStretch=.05f;hd.legStretch=.05f;hd.feetSpacing=0;
        importer.humanDescription=hd;
        var clips=importer.defaultClipAnimations;
        foreach(var c in clips){c.name=c.name.Split('|').Last();c.loopTime=true;c.loopPose=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.lockRootRotation=true;}
        importer.clipAnimations=clips;
        importer.SaveAndReimport();
        var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().FirstOrDefault();
        return new { valid=avatar!=null&&avatar.isValid,human=avatar!=null&&avatar.isHuman,mapped=hd.human.Select(b=>b.humanName).ToArray(),clips=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Select(c=>new{c.name,c.length,c.isHumanMotion}).ToArray() };
    }
    static Material Mat(string name,Color color,float metallic=0)
    {
        string path=Dir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.3f);return m;
    }
    static void Label(string text,Vector3 pos)
    {
        var go=new GameObject(text);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(12,0,0);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=.045f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.85f,.91f,.94f);
    }
    public static object Scene()
    {
        // Preserve any open demo scene edits before switching to the inspection scene.
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
        {var s=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);if(s.isDirty && !string.IsNullOrEmpty(s.path))EditorSceneManager.SaveScene(s);}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var meshMaterial=Mat("Adventurer_Inspection_Grey",new Color(.52f,.57f,.57f));
        var floorMaterial=Mat("Inspection_Floor",new Color(.055f,.075f,.095f));
        var pedestalMaterial=Mat("Inspection_Pedestal",new Color(.11f,.15f,.18f));
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var clips=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        string[] names={"Idle","Walk","RigCheck"};
        for(int i=0;i<3;i++)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(model);go.name="Adventurer_"+names[i];go.transform.position=new Vector3((i-1)*2.0f,0,0);go.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterial=meshMaterial;if(r is SkinnedMeshRenderer sm){sm.updateWhenOffscreen=true;sm.localBounds=new Bounds(new Vector3(0,.9f,0),Vector3.one*4);}}
            var a=go.GetComponent<Animator>();a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            string cp=Dir+"/"+names[i]+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(cp);
            var smachine=controller.layers[0].stateMachine;
            foreach(var s in smachine.states)smachine.RemoveState(s.state);
            var state=smachine.AddState(names[i]);state.motion=clips.FirstOrDefault(c=>c.name==names[i]);smachine.defaultState=state;a.runtimeAnimatorController=controller;
            var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pedestal.name=names[i]+"_Stand";pedestal.transform.position=new Vector3((i-1)*2,-.07f,0);pedestal.transform.localScale=new Vector3(1.45f,.065f,1.45f);pedestal.GetComponent<Renderer>().sharedMaterial=pedestalMaterial;
            Label(names[i].ToUpper(),new Vector3((i-1)*2,-.2f,-.8f));
            if(i==0)PrefabUtility.SaveAsPrefabAsset(go,Dir+"/Adventurer_Rigged.prefab");
        }
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Inspection Ground";floor.transform.position=new Vector3(0,-.14f,0);floor.transform.localScale=Vector3.one*3;floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
        var cam=new GameObject("Inspection Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(1.6f,1.9f,-7);cam.transform.LookAt(new Vector3(0,.85f,0));cam.orthographic=true;cam.orthographicSize=1.9f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.035f,.05f,.075f);cam.nearClipPlane=.05f;cam.farClipPlane=50;
        var key=new GameObject("Key Light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=2.2f;key.transform.rotation=Quaternion.Euler(45,-25,0);key.shadows=LightShadows.Soft;
        var fill=new GameObject("Fill Light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.85f;fill.color=new Color(.65f,.8f,1);fill.transform.rotation=Quaternion.Euler(25,150,0);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.39f,.45f);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/AdventurerRigReview.unity");AssetDatabase.SaveAssets();
        Selection.activeGameObject=GameObject.Find("Adventurer_Idle");
        if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0,.9f,0),new Vector3(6,3,3)),false);
        return new{scene=scene.path,clips=clips.Select(c=>c.name).ToArray(),avatar=model.GetComponent<Animator>().avatar.isValid};
    }
}
