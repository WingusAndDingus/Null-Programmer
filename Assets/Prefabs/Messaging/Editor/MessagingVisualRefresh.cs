#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Updates the messaging prefab's presentation without reinstalling its monitor host.</summary>
public static class MessagingVisualRefresh
{
    private const string Root = "Assets/Prefabs/Messaging";
    private const string PrefabPath = Root + "/Generated/MessagingWindow.prefab";

    [MenuItem("Tools/Null Programmer/Refresh Messaging Visuals")]
    public static void Refresh()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Exit Play mode before refreshing the messaging visuals.");
            return;
        }

        ConfigurePortrait(Root + "/Art/FamilyAvatar.png");
        ConfigurePortrait(Root + "/Art/FriendAvatar.png");
        AssetDatabase.Refresh();

        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var view = prefab.GetComponent<MessagingWindowView>();
            if (view == null || view.body == null || view.titleBar == null || view.drag == null)
                throw new InvalidOperationException("MessagingWindow.prefab is missing its window view, body, title bar, or drag component.");

            view.RebuildVisuals();
            EditorUtility.SetDirty(view);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssignPortrait("family_checkin", Root + "/Art/FamilyAvatar.png");
        AssignPortrait("friend_weekend", Root + "/Art/FriendAvatar.png");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Messaging visuals refreshed. Existing right-monitor prefab instances will use the updated layout; you do not need to reinstall the monitor host.");
    }

    private static void ConfigurePortrait(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Portrait image is missing: " + path);
        bool changed = importer.textureType != TextureImporterType.Sprite ||
                       importer.spriteImportMode != SpriteImportMode.Single ||
                       importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                       importer.textureCompression != TextureImporterCompression.Uncompressed ||
                       !importer.alphaIsTransparency;
        if (!changed) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void AssignPortrait(string conversationId, string spritePath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) throw new InvalidOperationException("Portrait did not import as a Sprite: " + spritePath);
        var conversation = AssetDatabase.FindAssets("t:MessageConversation")
            .Select(guid => AssetDatabase.LoadAssetAtPath<MessageConversation>(AssetDatabase.GUIDToAssetPath(guid)))
            .FirstOrDefault(item => item != null && item.id == conversationId);
        if (conversation == null) return;
        conversation.avatar = sprite;
        EditorUtility.SetDirty(conversation);
    }
}
#endif
