if(!EditorApplication.isPlaying)throw new System.Exception("Play mode required");
var demo=UnityEngine.Object.FindFirstObjectByType<RpgCombatSample.CombatSample>();
demo.SetAutomatic(false);
var camera=demo.reviewCamera;
var output=System.IO.Path.GetFullPath("Art/RPGCharacter/PreviewFrames");System.IO.Directory.CreateDirectory(output);
var rt=RenderTexture.GetTemporary(960,540,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
var texture=new Texture2D(960,540,TextureFormat.RGB24,false);
int count=0,lastFrame=-1,oldCapture=Time.captureFramerate,oldTarget=Application.targetFrameRate;bool begun=false;
int startStrikes=demo.CompletedStrikes;
int startHits=demo.ConfirmedHits;
UnityEditor.EditorApplication.CallbackFunction tick=null;
System.Action cleanup=()=>{EditorApplication.update-=tick;Time.captureFramerate=oldCapture;Application.targetFrameRate=oldTarget;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);};
tick=()=>{
 if(!EditorApplication.isPlaying){cleanup();return;}
 if(!begun){if(demo.CurrentAttack>=0)return;demo.SetAutomatic(true);Time.captureFramerate=15;Application.targetFrameRate=15;begun=true;}
 if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
 var previous=camera.targetTexture;var active=RenderTexture.active;
 try{
  camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
  texture.ReadPixels(new Rect(0,0,960,540),0,0);texture.Apply();
  System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"Combat_"+count.ToString("000")+".png"),texture.EncodeToPNG());
  count++;
 }catch(System.Exception ex){Debug.LogException(ex);cleanup();return;}
 finally{camera.targetTexture=previous;RenderTexture.active=active;}
 if(count>=60){System.IO.File.WriteAllText("Art/RPGCharacter/recording_validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {frames=count,fps=15,seconds=4,completedStrikes=demo.CompletedStrikes-startStrikes,hits=demo.ConfirmedHits-startHits,frameCount=Time.frameCount}));cleanup();}
};
EditorApplication.update+=tick;EditorApplication.isPaused=false;
return new {output,frames=60};
