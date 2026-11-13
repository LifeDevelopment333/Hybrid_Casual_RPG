const string path="Assets/Characters/tripo_convert_c68bf175-48e4-46b7-ad62-d50e3fed1a30.fbx";
var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
var importer=(ModelImporter)AssetImporter.GetAtPath(path);
return new {
    animationType=importer.animationType.ToString(), scale=importer.globalScale, fileScale=importer.fileScale,
    transforms=model.GetComponentsInChildren<Transform>(true).Select(t=>new {t.name,position=t.localPosition.ToString(),scale=t.localScale.ToString()}).ToArray(),
    renderers=model.GetComponentsInChildren<Renderer>(true).Select(r=>new {r.name,bounds=r.bounds.ToString(),materials=r.sharedMaterials.Select(m=>m?m.name:"null").ToArray(),bones=r is SkinnedMeshRenderer s?s.bones.Length:0}).ToArray(),
    assets=AssetDatabase.LoadAllAssetsAtPath(path).Where(a=>a is AnimationClip||a is Avatar||a is Material).Select(a=>new {a.name,type=a.GetType().Name}).ToArray(),
    textures=AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Characters/tripo_convert_c68bf175-48e4-46b7-ad62-d50e3fed1a30.fbm"}).Select(g=>AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g))).Select(t=>new {t.name,t.width,t.height}).ToArray()
};
