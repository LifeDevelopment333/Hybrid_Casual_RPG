using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class OrganizePlayerAnimations
{
    const string Root = "Assets/RPG/ART/Player/Animations";
    static string Category(string name)
    {
        if (name.Contains("death")) return "Death";
        if (name.Contains("idle")) return "Idle";
        if (name.Contains("walk")) return "Walk";
        if (name.Contains("run")) return "Run";
        if (name.Contains("turn")) return "Turn";
        if (name.Contains("strafe")) return "Strafe";
        if (name.Contains("blocking")) return "Block";
        if (name.Contains("impact")) return "Hit";
        if (name.Contains("crouching")) return "Crouch";
        if (name.Contains("jump attack")) return "JumpAttack";
        if (name.Contains("jump")) return "Jump";
        if (name.Contains("slide attack")) return "SlideAttack";
        if (name.Contains("high spin attack")) return "SpinAttack";
        if (name.Contains("slash")) return "Slash";
        if (name.Contains("kick")) return "Kick";
        if (name.Contains("attack")) return "Attack";
        if (name.Contains("cast")) return "Cast";
        if (name.Contains("power up")) return "PowerUp";
        if (name.StartsWith("draw a")) return "DrawSword";
        throw new Exception("Unclassified animation: " + name);
    }
    public static object Main()
    {
        var paths = AssetDatabase.FindAssets("t:AnimationClip", new[] { Root })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == Root).OrderBy(p => p).ToArray();
        var moves = paths.Select(p => new { source = p, category = Category(Path.GetFileNameWithoutExtension(p)), guid = AssetDatabase.AssetPathToGUID(p) }).ToArray();
        // Validate all destinations before moving any asset.
        foreach (var move in moves)
            if (File.Exists(Root + "/" + move.category + "/" + Path.GetFileName(move.source)))
                throw new Exception("Destination already exists for " + move.source);
        var controllers = AssetDatabase.FindAssets("t:RuntimeAnimatorController", new[] { "Assets/RPG/ART/Player" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(p => AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p)).Where(c => c).ToArray();
        var references = controllers.ToDictionary(c => c, c => c.animationClips.Select(AssetDatabase.GetAssetPath).Select(AssetDatabase.AssetPathToGUID).OrderBy(g => g).ToArray());
        var completed = new List<KeyValuePair<string, string>>();
        try
        {
            foreach (string category in moves.Select(m => m.category).Distinct())
                if (!AssetDatabase.IsValidFolder(Root + "/" + category)) AssetDatabase.CreateFolder(Root, category);
            foreach (var move in moves)
            {
                string destination = Root + "/" + move.category + "/" + Path.GetFileName(move.source);
                string error = AssetDatabase.MoveAsset(move.source, destination);
                if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                completed.Add(new KeyValuePair<string, string>(move.source, destination));
                if (AssetDatabase.AssetPathToGUID(destination) != move.guid) throw new Exception("GUID changed: " + destination);
            }
            foreach (var controller in controllers)
            {
                var after = controller.animationClips.Select(AssetDatabase.GetAssetPath).Select(AssetDatabase.AssetPathToGUID).OrderBy(g => g);
                if (!references[controller].SequenceEqual(after)) throw new Exception("Controller references changed: " + controller.name);
            }
        }
        catch
        {
            foreach (var move in completed.AsEnumerable().Reverse()) AssetDatabase.MoveAsset(move.Value, move.Key);
            throw;
        }
        AssetDatabase.SaveAssets();
        return new { moved = moves.Length, guidAndControllerReferencesPreserved = true, controllers = controllers.Select(c => new { c.name, clips = c.animationClips.Length }).ToArray(), folders = moves.GroupBy(m => m.category).Select(g => new { folder = g.Key, clips = g.Count() }).ToArray() };
    }
}
