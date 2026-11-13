const string dir="Assets/Characters/FantasyAdventurer";
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(dir+"/FantasyAdventurer.prefab");
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var checks=new System.Collections.Generic.List<object>();
try{
    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);go.transform.rotation=Quaternion.Euler(0,180,0);
    var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
    var renderer=go.GetComponentInChildren<SkinnedMeshRenderer>();
    var baked=new Mesh();
    var cameraObject=new GameObject("Verification Camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,preview);
    var camera=cameraObject.AddComponent<Camera>();camera.scene=preview;camera.transform.position=new Vector3(2.7f,1.65f,-5);camera.transform.LookAt(new Vector3(0,.9f,0));camera.orthographic=true;camera.orthographicSize=1.05f;
    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.10f,.14f);
    var keyObject=new GameObject("Verification Key");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(keyObject,preview);
    var key=keyObject.AddComponent<Light>();key.type=LightType.Directional;key.intensity=2.4f;key.transform.rotation=Quaternion.Euler(35,-30,0);
    var fillObject=new GameObject("Verification Fill");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,preview);
    var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=1.1f;fill.transform.rotation=Quaternion.Euler(25,150,0);
    foreach(string name in new[]{"Idle","Walk"}){
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(dir+"/"+name+".anim");
        var graph=UnityEngine.Playables.PlayableGraph.Create("Verify "+name);
        try{
            graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
            var playable=UnityEngine.Animations.AnimationClipPlayable.Create(graph,clip);
            var output=UnityEngine.Animations.AnimationPlayableOutput.Create(graph,"Character",animator);UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(output,playable);
            graph.Play();float minY=float.MaxValue,maxY=float.MinValue;Vector3[] first=null;float seam=0;
            for(int i=0;i<=16;i++){
                UnityEngine.Playables.PlayableExtensions.SetTime(playable,clip.length*i/16.0);graph.Evaluate(0);
                renderer.BakeMesh(baked);var vertices=baked.vertices;
                if(i==0)first=vertices;
                foreach(var vertex in vertices){
                    var world=renderer.transform.TransformPoint(vertex);
                    if(float.IsNaN(world.x)||float.IsNaN(world.y)||float.IsNaN(world.z))throw new Exception("Invalid mesh vertex");
                    minY=Mathf.Min(minY,world.y);maxY=Mathf.Max(maxY,world.y);
                }
                if(i==16)for(int j=0;j<vertices.Length;j++)seam=Mathf.Max(seam,Vector3.Distance(first[j],vertices[j]));
            }
            checks.Add(new{clip=name,clip.isHumanMotion,clip.isLooping,vertices=baked.vertexCount,minY,maxY,loopSeamMetres=seam});
            UnityEngine.Playables.PlayableExtensions.SetTime(playable,clip.length*.25);graph.Evaluate(0);
            var rt=new RenderTexture(550,650,24);camera.targetTexture=rt;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(550,650,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,550,650),0,0);image.Apply();System.IO.File.WriteAllBytes("Art/Characters/FantasyAdventurer/Unity_"+name+".png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }finally{graph.Destroy();}
    }
    UnityEngine.Object.DestroyImmediate(baked);
    return new{avatarValid=animator.avatar.isValid,avatarHuman=animator.avatar.isHuman,checks};
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
