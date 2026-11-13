// Capture the explicit review camera, preserving any other open scene's camera.
var camera=GameObject.Find("Combat Review Camera").GetComponent<Camera>();
var active=RenderTexture.active;var previous=camera.targetTexture;
var rt=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
try{
    camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
    texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
    System.IO.File.WriteAllBytes("Art/RPGCharacter/Combat_Review.png",texture.EncodeToPNG());
}finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
return "Art/RPGCharacter/Combat_Review.png";
