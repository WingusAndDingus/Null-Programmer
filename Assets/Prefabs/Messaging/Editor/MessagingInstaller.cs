#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class MessagingInstaller
{
    private const string Root="Assets/Messaging";
    [MenuItem("Tools/Null Programmer/Install Messaging In Current Desktop")]
    public static void Install()
    {
        if(Application.isPlaying) { Debug.LogError("Exit Play mode first."); return; }
        
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var inbox=Object.FindObjectsByType<IntegratedMailInbox>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        var ide=Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.name=="IDE Icon");
        if(inbox==null || ide==null || inbox.WindowObject==null || inbox.Drag==null) throw new System.InvalidOperationException("Open a desktop scene containing the existing mail window and IDE Icon.");
        if(TMP_Settings.defaultFontAsset==null) throw new System.InvalidOperationException("Import TMP Essential Resources first.");
        var importer=AssetImporter.GetAtPath(Root+"/Art/MessagesIcon.png") as TextureImporter;
        if(importer==null) throw new System.InvalidOperationException("MessagesIcon.png is missing from Assets/Messaging/Art.");
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/MessagesIcon.png");
        if(!AssetDatabase.IsValidFolder(Root+"/Generated")) AssetDatabase.CreateFolder(Root,"Generated");
        // Remove only the previous messaging installation in this scene.
        foreach(var old in Object.FindObjectsByType<MessagingDesktopApp>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(old.view!=null) Undo.DestroyObjectImmediate(old.view.gameObject);
            if(old.badgeCanvas!=null) Undo.DestroyObjectImmediate(old.badgeCanvas.gameObject);
            Undo.DestroyObjectImmediate(old.gameObject);
        }
        var original=inbox.WindowObject;
        string prefabPath=Root+"/Generated/MessagingWindow.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject window;
        MessagingWindowView view;
        {
        window=Object.Instantiate(original,original.transform.parent); window.name="Messaging Window"; window.SetActive(true);
        Undo.RegisterCreatedObjectUndo(window,"Install Messaging");
        var source=new SerializedObject(inbox);
        var originalBody=source.FindProperty("windowBody").objectReferenceValue as SpriteRenderer;
        var originalBar=source.FindProperty("titleBar").objectReferenceValue as SpriteRenderer;
        var body=Part(window,original,originalBody.transform).GetComponent<SpriteRenderer>();
        var bar=Part(window,original,originalBar.transform).GetComponent<SpriteRenderer>();
        foreach(var canvas in window.GetComponentsInChildren<Canvas>(true)) Object.DestroyImmediate(canvas.gameObject);
        foreach(var identity in window.GetComponentsInChildren<WindowIdentity>(true))
        { var so=new SerializedObject(identity); so.FindProperty("windowId").stringValue="MessagingWindow"; so.ApplyModifiedPropertiesWithoutUndo(); }
        foreach(var label in bar.GetComponentsInChildren<TMP_Text>(true))
            if(!label.transform.parent.name.Contains("Button")) label.text="Messages";
        view=window.AddComponent<MessagingWindowView>(); view.body=body; view.titleBar=bar;
        view.drag=window.GetComponentInChildren<DraggableWindow>(true);
        MatchEmailTypography(view,inbox);
        BuildUI(view);
        view.FitContents();
        // Authored scene references (desktop markers, camera, close handler) are restored below.
        prefab=PrefabUtility.SaveAsPrefabAsset(window,prefabPath);
        Object.DestroyImmediate(window);
        }
        var parent=original.transform.parent; var position=original.transform.localPosition; var scale=original.transform.localScale;
        window=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        window.transform.localPosition=position; window.transform.localScale=scale;
        Undo.RegisterCreatedObjectUndo(window,"Install Messaging Window");
        view=window.GetComponent<MessagingWindowView>();
        var identityComponent=window.GetComponentInChildren<WindowIdentity>(true);
        if(identityComponent==null) identityComponent=window.AddComponent<WindowIdentity>();
        var identitySO=new SerializedObject(identityComponent);
        identitySO.FindProperty("windowId").stringValue="MessagingWindow";
        identitySO.ApplyModifiedPropertiesWithoutUndo();
        var dragSO=new SerializedObject(view.drag);
        dragSO.FindProperty("windowIdentity").objectReferenceValue=identityComponent;
        dragSO.FindProperty("desktopBottomLeft").objectReferenceValue=inbox.Drag.DesktopBottomLeft;
        dragSO.FindProperty("desktopTopRight").objectReferenceValue=inbox.Drag.DesktopTopRight;
        dragSO.ApplyModifiedPropertiesWithoutUndo();
        var icon=Object.Instantiate(ide.gameObject,ide.transform.parent); icon.name="Messages Icon";
        Undo.RegisterCreatedObjectUndo(icon,"Install Messages Icon");
        var image=icon.GetComponent<SpriteRenderer>();
        var mailIcon=inbox.GetComponent<SpriteRenderer>();
        if(mailIcon==null) mailIcon=inbox.GetComponentInChildren<SpriteRenderer>(true);
        Vector3 size=mailIcon!=null ? mailIcon.bounds.size : ide.bounds.size;
        // Match visible artwork rather than the transparent PNG rectangle.
        importer.isReadable=true; importer.SaveAndReimport();
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/MessagesIcon.png");
        var pixels=sprite.texture.GetPixels32(); int w=sprite.texture.width,h=sprite.texture.height;
        int xmin=w,ymin=h,xmax=0,ymax=0;
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(pixels[y*w+x].a>16)
        { xmin=Mathf.Min(xmin,x); xmax=Mathf.Max(xmax,x); ymin=Mathf.Min(ymin,y); ymax=Mathf.Max(ymax,y); }
        float visibleX=Mathf.Max(1,xmax-xmin+1)/(float)w, visibleY=Mathf.Max(1,ymax-ymin+1)/(float)h;
        image.sprite=sprite; image.drawMode=SpriteDrawMode.Simple;
        Vector3 parentScale=icon.transform.parent.lossyScale;
        icon.transform.localScale=new Vector3(size.x/(sprite.bounds.size.x*visibleX)/parentScale.x,size.y/(sprite.bounds.size.y*visibleY)/parentScale.y,1);
        icon.transform.position=ide.transform.position+new Vector3((ide.bounds.size.x+size.x)*.5f+size.x*.3f,0,0);
        var collider=icon.GetComponent<BoxCollider2D>(); if(collider!=null) { collider.size=new Vector2(sprite.bounds.size.x*visibleX,sprite.bounds.size.y*visibleY); collider.offset=new Vector2(((xmin+xmax+1)*.5f-w*.5f)/sprite.pixelsPerUnit,((ymin+ymax+1)*.5f-h*.5f)/sprite.pixelsPerUnit); }
        var app=icon.AddComponent<MessagingDesktopApp>(); app.view=view;
        app.conversations=new[] { MakeConversation("FamilyCheckIn","family_checkin","Family",1), MakeConversation("WeekendFriend","friend_weekend","Friend",7) };
        var trigger=icon.GetComponent<EventTrigger>(); if(trigger==null) trigger=icon.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        var entry=new EventTrigger.Entry { eventID=EventTriggerType.PointerClick };
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(entry.callback,app.Open); trigger.triggers.Add(entry);
        foreach(var control in window.GetComponentsInChildren<EventTrigger>(true))
        {
            if(control.name.Contains("Close"))
            {
                control.triggers.Clear();
                var closeEntry=new EventTrigger.Entry { eventID=EventTriggerType.PointerClick };
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(closeEntry.callback,app.Close);
                control.triggers.Add(closeEntry); EditorUtility.SetDirty(control); continue;
            }
            var so=new SerializedObject(control); var entries=so.FindProperty("m_Delegates");
            for(int e=0;e<entries.arraySize;e++)
            {
                var calls=entries.GetArrayElementAtIndex(e).FindPropertyRelative("callback.m_PersistentCalls.m_Calls");
                for(int i=0;i<calls.arraySize;i++)
                {
                    var call=calls.GetArrayElementAtIndex(i);
                    if(call.FindPropertyRelative("m_MethodName").stringValue!="CloseWindow") continue;
                    call.FindPropertyRelative("m_Target").objectReferenceValue=app;
                    call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue=typeof(MessagingDesktopApp).AssemblyQualifiedName;
                    call.FindPropertyRelative("m_MethodName").stringValue="Close";
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        // No desktop notification badge.
        view.FitContents(); window.SetActive(false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(view.drag);
        EditorUtility.SetDirty(app);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Selection.activeGameObject=icon;
        Debug.Log("Messaging installed in current scene. Save the scene, then Play. Original email and IDE scripts are unchanged.");
    }
    private static void MatchEmailTypography(MessagingWindowView view,IntegratedMailInbox inbox)
    {
        var source=new SerializedObject(inbox);
        var tmp=source.FindProperty("bodyLabelTMP").objectReferenceValue as TMP_Text;
        var legacy=source.FindProperty("bodyLabel").objectReferenceValue as Text;
        view.messageFont=tmp!=null && tmp.font!=null ? tmp.font : TMP_Settings.defaultFontAsset;
        // Both canvases use a 900-unit reference width. Preserve email's world text size.
        float width=inbox.WindowBody.bounds.size.x;
        float padding=Mathf.Min(inbox.WindowBody.bounds.size.x,inbox.WindowBody.bounds.size.y)*.025f;
        float targetUnits=(width-2*padding)/900f;
        float sourceUnits=source.FindProperty("worldUnitsPerPixel").floatValue;
        if(sourceUnits<=0) sourceUnits=targetUnits;
        var textTransform=tmp!=null ? tmp.transform : legacy!=null ? legacy.transform : null;
        float relativeScale=1f;
        if(textTransform!=null && inbox.ContentCanvas!=null)
            relativeScale=Mathf.Abs(textTransform.lossyScale.y/inbox.ContentCanvas.transform.lossyScale.y);
        view.messageFontSize=(tmp!=null ? tmp.fontSize : legacy!=null ? legacy.fontSize : 36)*sourceUnits/targetUnits*relativeScale;
    }
    private static Transform Part(GameObject clone,GameObject original,Transform part)
    {
        string path=AnimationUtility.CalculateTransformPath(part,original.transform);
        return string.IsNullOrEmpty(path) ? clone.transform : clone.transform.Find(path);
    }
    private static MessageConversation MakeConversation(string name,string id,string contact,int day)
    {
        string path=Root+"/Generated/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<MessageConversation>(path); if(existing!=null) return existing;
        var c=ScriptableObject.CreateInstance<MessageConversation>(); c.id=id; c.contactName=contact; c.availableFromDay=day;
        c.openingMessage=day==1 ? "Hey, you've been quiet lately. How's the new project going?" : "You've been working all week. Want to catch up this weekend?";
        c.replies=new[] {
            new MessageConversation.Reply { text=day==1 ? "It's been stressful. Can we catch up this weekend?" : "Yes. I'd like to catch up.", response="Of course. You don't have to handle everything alone.",connectednessDelta=10 },
            new MessageConversation.Reply { text="Busy right now. I'll message later.",response="Okay. Let me know when you're free." },
            new MessageConversation.Reply { text="Please stop distracting me.",response="I was just checking in. I'll give you some space.",connectednessDelta=-5 }
        };
        AssetDatabase.CreateAsset(c,path); return c;
    }
    private static void BuildUI(MessagingWindowView view)
    {
        var go=new GameObject("Messages Content",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        go.transform.SetParent(view.transform,false); view.canvas=go.GetComponent<Canvas>(); view.canvas.renderMode=RenderMode.WorldSpace;
        var back=Rect("Background",go.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        back.gameObject.AddComponent<Image>().color=new Color(.055f,.075f,.105f);
        var heading=Rect("Header",go.transform,new Vector2(0,1),Vector2.one,new Vector2(18,-65),new Vector2(-18,-8));
        view.heading=Label("Title",heading,"Messages",Mathf.RoundToInt(view.messageFontSize));
        view.heading.font=view.messageFont; view.heading.fontSharedMaterial=view.messageFont.material;
        var left=Rect("Contacts",go.transform,Vector2.zero,new Vector2(.25f,1),new Vector2(12,12),new Vector2(-12,-80));
        Layout(left); view.contacts=left;
        var right=Rect("Conversation",go.transform,new Vector2(.27f,0),Vector2.one,new Vector2(0,12),new Vector2(-12,-80));
        var choices=Rect("Replies",right,Vector2.zero,new Vector2(1,.45f),Vector2.zero,Vector2.zero); 
        var replyScroll=choices.gameObject.AddComponent<ScrollRect>(); replyScroll.horizontal=false;
        var replyViewport=Rect("Reply Viewport",choices,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        replyViewport.gameObject.AddComponent<Image>().color=new Color(.055f,.075f,.105f);
        replyViewport.gameObject.AddComponent<RectMask2D>();
        var replyContent=Rect("Reply Choices",replyViewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);
        replyContent.pivot=new Vector2(.5f,1); Layout(replyContent);
        replyContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        replyScroll.viewport=replyViewport; replyScroll.content=replyContent; view.replies=replyContent;
        var scrollRoot=Rect("History Scroll",right,new Vector2(0,.48f),Vector2.one,Vector2.zero,Vector2.zero);
        var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false;
        var viewport=Rect("Viewport",scrollRoot,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color=new Color(.08f,.11f,.15f); viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect("History",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);
        content.pivot=new Vector2(.5f,1); Layout(content);
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport=viewport; scroll.content=content; view.scroll=scroll; view.history=content;
    }
    private static void Layout(RectTransform rect)
    {
        var layout=rect.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=12;
        layout.padding=new RectOffset(10,10,10,10); layout.childControlWidth=true; layout.childControlHeight=true;
        layout.childForceExpandWidth=true; layout.childForceExpandHeight=false;
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var rect=go.GetComponent<RectTransform>();
        rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=low; rect.offsetMax=high; return rect;
    }
    private static TMP_Text Label(string name,Transform parent,string text,int size)
    {
        var rect=Rect(name,parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font=TMP_Settings.defaultFontAsset; label.fontSharedMaterial=label.font.material;
        label.text=text; label.fontSize=size; label.color=Color.white; label.raycastTarget=false; return label;
    }
}
#endif
