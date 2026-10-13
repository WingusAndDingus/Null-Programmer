#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace NullProgrammer.Day3
{
    public static class Day3LearningInstaller
    {
        private const string Root="Assets/Day3Learning";
        private static string Generated=Root+"/Generated";
        [MenuItem("Tools/Null Programmer/Install Day 3 Learning From Current Desktop")]
        public static void Install()
        {
            if(Application.isPlaying) { Debug.LogError("Exit Play mode first."); return; }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(All<Day3DesktopFlow>().Any()) { Repair(); return; }
            var source=SceneManager.GetActiveScene(); string originalPath=source.path;
            if(string.IsNullOrEmpty(originalPath)) { Debug.LogError("Open and save your working desktop scene first."); return; }
            string runFolder=null;
            try
            {
                if(source.path.StartsWith("Assets/Day3Integrated/")) throw new InvalidOperationException("Open your working baseline desktop instead of the old Day3Integrated scene.");
                var mail=All<IntegratedMailInbox>().FirstOrDefault();
                if(mail==null || mail.WindowObject==null || mail.Drag==null || mail.Drag.DesktopBottomLeft==null || mail.Drag.DesktopTopRight==null)
                    throw new InvalidOperationException("The source desktop needs a working inbox with monitor bounds assigned.");
                if(All<DialogueManager>().FirstOrDefault()==null) throw new InvalidOperationException("The source desktop needs its DialogueManager.");
                if(TMP_Settings.defaultFontAsset==null) throw new InvalidOperationException("Import TextMeshPro Essential Resources first.");
                EnsureFolder(Root+"/Generated");
                runFolder=AssetDatabase.GenerateUniqueAssetPath(Root+"/Generated/Wednesday_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                EnsureFolder(runFolder); Generated=runFolder;
                string path=Generated+"/Wednesday_Desktop.unity";
                if(!EditorSceneManager.SaveScene(source,path,true)) throw new InvalidOperationException("Could not copy the working desktop.");
                EditorSceneManager.OpenScene(path); DisableOldFlow();
                var flow=new GameObject("Wednesday Learning Flow").AddComponent<Day3DesktopFlow>(); flow.enabled=false;
                flow.inbox=All<IntegratedMailInbox>().First();
                var lesson=ScriptableObject.CreateInstance<Day3LessonData>(); lesson.SetDefaults();
                AssetDatabase.CreateAsset(lesson,Generated+"/Wednesday_Lesson.asset"); flow.lesson=lesson;
                CompleteSetup(flow);
                SaveAndValidate(flow);
                var paths=new List<string>{path};
                CopySideMonitors(Path.GetFileNameWithoutExtension(path),Path.GetFileNameWithoutExtension(originalPath),paths);
                EditorSceneManager.OpenScene(path);
                Validate(All<Day3DesktopFlow>().First());
                var scenes=EditorBuildSettings.scenes.ToList();
                foreach(var old in scenes.Where(x=>x.path.StartsWith(Root+"/Generated/") && Path.GetFileNameWithoutExtension(x.path).StartsWith("Wednesday_"))) old.enabled=false;
                foreach(string scene in paths) { scenes.RemoveAll(x=>x.path==scene); scenes.Add(new EditorBuildSettingsScene(scene,true)); }
                EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets();
                Selection.activeGameObject=All<Day3DesktopFlow>().First().gameObject;
                Debug.Log("DAY 3 READY: "+path+". All controller and IDE references verified after scene reload. Play this scene.");
            }
            catch(Exception error)
            {
                EditorSceneManager.OpenScene(originalPath);
                if(runFolder!=null) AssetDatabase.DeleteAsset(runFolder);
                Debug.LogError("Day 3 installation stopped; the incomplete copy was removed and your source desktop reopened. Cause: "+error.Message+"\n"+error.StackTrace);
            }
            finally { Generated=Root+"/Generated"; }
        }
        [MenuItem("Tools/Null Programmer/Repair Current Day 3 Scene")]
        public static void Repair()
        {
            if(Application.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var flow=All<Day3DesktopFlow>().FirstOrDefault();
            if(flow==null) { Debug.LogError("Open a generated Wednesday scene to repair it, or run Install from your working desktop."); return; }
            string path=SceneManager.GetActiveScene().path;
            try
            {
                Generated=Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(Generated);
                flow.enabled=false; DisableOldFlow();
                if(flow.inbox==null) flow.inbox=All<IntegratedMailInbox>().FirstOrDefault();
                if(flow.lesson==null) { flow.lesson=ScriptableObject.CreateInstance<Day3LessonData>(); flow.lesson.SetDefaults(); AssetDatabase.CreateAsset(flow.lesson,AssetDatabase.GenerateUniqueAssetPath(Generated+"/Wednesday_Lesson.asset")); }
                foreach(var view in All<Day3ModalView>().ToArray()) Object.DestroyImmediate(view.gameObject);
                foreach(var adapter in All<Day3InboxAdapter>().ToArray()) Object.DestroyImmediate(adapter);
                foreach(var canvas in All<Canvas>().Where(x=>x.name=="Wednesday Objective" || x.name=="Wednesday Zoom Responses").ToArray()) Object.DestroyImmediate(canvas.gameObject);
                CompleteSetup(flow); SaveAndValidate(flow);
                EditorSceneManager.OpenScene(path); Validate(All<Day3DesktopFlow>().First());
                Debug.Log("DAY 3 READY: repaired and reloaded "+path);
            }
            catch(Exception error) { if(flow!=null) flow.enabled=false; Debug.LogError("Day 3 repair stopped: "+error.Message+"\n"+error.StackTrace); }
            finally { Generated=Root+"/Generated"; }
        }
        private static void CompleteSetup(Day3DesktopFlow flow)
        {
            // Build the view first. No external callback lookup can leave it absent.
            var view=BuildView(); view.gameObject.SetActive(false);
            string prefabPath=AssetDatabase.GenerateUniqueAssetPath(Generated+"/Wednesday_IDE.prefab");
            var prefab=PrefabUtility.SaveAsPrefabAsset(view.gameObject,prefabPath);
            if(prefab==null || prefab.GetComponent<Day3ModalView>()==null) throw new InvalidOperationException("The IDE prefab root does not contain Day3ModalView. Check the script import in Unity.");
            Object.DestroyImmediate(view.gameObject);
            var instance=PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if(instance==null) throw new InvalidOperationException("Could not instantiate the IDE prefab.");
            flow.view=instance.GetComponent<Day3ModalView>();
            var hud=Canvas("Wednesday Objective",5);
            flow.objective=Text("Objective",hud.transform,new Vector2(0,485),new Vector2(1600,55),"Wednesday: Read Steve's email.",28,TextAlignmentOptions.Center);
            flow.stats=Text("Stats",hud.transform,new Vector2(0,435),new Vector2(1600,45),"",25,TextAlignmentOptions.Center);
            flow.submitButton=Button("Submit to Steve",hud.transform,new Vector2(0,370),new Vector2(520,60),"SEND TESTED WORK TO STEVE");
            flow.joinCallButton=Button("Join Zoom",hud.transform,new Vector2(0,370),new Vector2(520,60),"JOIN STEVE'S ZOOM CALL");
            flow.openIDEButton=Button("Open Board IDE",hud.transform,new Vector2(0,370),new Vector2(520,60),"OPEN BOARD.CPP IN IDE");
            flow.submitButton.gameObject.SetActive(false); flow.joinCallButton.gameObject.SetActive(false); flow.openIDEButton.gameObject.SetActive(false);
            var responses=Canvas("Wednesday Zoom Responses",210); flow.zoomChoices=responses.gameObject;
            flow.zoomResponses=new Button[3];
            string[] texts={"I'll validate every cell before placing the ship.","I'll organize placement through the Board.","It looks fine. We can skip testing."};
            for(int i=0;i<3;i++) flow.zoomResponses[i]=Button("Response "+i,responses.transform,new Vector2((i-1)*550,100),new Vector2(500,180),texts[i]);
            flow.zoomChoices.SetActive(false);
            InstallMailAdapter(flow); InstallZoom(flow); BindIDEIcon(flow); EnsureEventSystem();
            var manager=All<DialogueManager>().First();
            manager.enabled=true;
            for(var t=manager.transform;t!=null;t=t.parent) t.gameObject.SetActive(true);
            EditorUtility.SetDirty(manager);
            foreach(var canvas in manager.GetComponentsInChildren<Canvas>(true))
            { canvas.overrideSorting=true; canvas.sortingOrder=200; EditorUtility.SetDirty(canvas); }
            Validate(flow);
            flow.enabled=true; EditorUtility.SetDirty(flow);
        }
        private static void SaveAndValidate(Day3DesktopFlow flow)
        {
            EditorUtility.SetDirty(flow);
            foreach(var adapter in All<Day3InboxAdapter>()) EditorUtility.SetDirty(adapter);
            foreach(var link in All<Day3DesktopIconLink>()) EditorUtility.SetDirty(link);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save generated references.");
            AssetDatabase.SaveAssets(); Validate(flow);
        }
        private static void Validate(Day3DesktopFlow flow)
        {
            if(flow==null || flow.lesson==null || flow.inbox==null || flow.view==null || flow.objective==null || flow.stats==null || flow.submitButton==null || flow.joinCallButton==null || flow.openIDEButton==null || flow.zoom==null || flow.incomingCall==null || flow.zoomPanel==null || flow.zoomChoices==null || flow.zoomResponses==null || flow.zoomResponses.Length!=3)
                throw new InvalidOperationException("A required Wednesday controller reference is missing. The scene was not declared ready.");
            var v=flow.view;
            if(v.title==null || v.teaching==null || v.code==null || v.feedback==null || v.output==null || v.progress==null || v.status==null || v.lessonPanel==null || v.demoPanel==null || v.run==null || v.next==null || v.close==null || v.finish==null || v.rotate==null || v.reset==null || v.architectHint==null || v.sentinelHint==null || v.optionLabels==null || v.optionLabels.Length!=3 || v.options==null || v.options.Length!=3 || v.boardCells==null || v.boardCells.Length!=100 || v.previewCells==null || v.previewCells.Length!=100)
                throw new InvalidOperationException("IDE view was created with incomplete fields.");
            if(v.optionLabels.Any(x=>x==null) || v.options.Any(x=>x==null) || v.boardCells.Any(x=>x==null) || v.previewCells.Any(x=>x==null) || flow.zoomResponses.Any(x=>x==null))
                throw new InvalidOperationException("A generated button or preview cell was not assigned.");
        }
        private static void BindIDEIcon(Day3DesktopFlow flow)
        {
            var icon=All<SpriteRenderer>().FirstOrDefault(x=>x.name=="IDE Icon") ?? All<SpriteRenderer>().FirstOrDefault(x=>x.name.Contains("IDE") && x.name.Contains("Icon"));
            if(icon==null) { Debug.LogWarning("IDE icon not found. Use the generated Open Board.cpp button."); return; }
            foreach(var trigger in icon.GetComponents<EventTrigger>()) { trigger.triggers.Clear(); EditorUtility.SetDirty(trigger); if(PrefabUtility.IsPartOfPrefabInstance(trigger)) PrefabUtility.RecordPrefabInstancePropertyModifications(trigger); }
            var link=icon.GetComponent<Day3DesktopIconLink>() ?? icon.gameObject.AddComponent<Day3DesktopIconLink>();
            link.flow=flow; link.acceptCall=false; EditorUtility.SetDirty(link);
        }
        private static IEnumerable<T> All<T>() where T:Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(x=>x!=null && x.gameObject.scene==SceneManager.GetActiveScene());
        private static void DisableOldFlow()
        {
            foreach(var b in All<MonoBehaviour>())
            {
                if(b==null) continue;
                string name=b.GetType().Name;
                if(new[]{"DayNarrativeExecutor","EmailReplyGate","SuspicionDemoUI","ZoomTestManager","Day3DesktopBridge","Day3Controller"}.Contains(name))
                {
                    if(name=="SuspicionDemoUI")
                    {
                        var so=new SerializedObject(b);
                        foreach(string key in new[]{"replyButton","dialoguePanel","testControls","warningLabelTMP"})
                        { var field=so.FindProperty(key); if(field?.objectReferenceValue is GameObject go) go.SetActive(false); else if(field?.objectReferenceValue is Component c) c.gameObject.SetActive(false); }
                    }
                    b.enabled=false; EditorUtility.SetDirty(b);
                }
            }
        }
        private static void InstallMailAdapter(Day3DesktopFlow flow)
        {
            var so=new SerializedObject(flow.inbox);
            so.FindProperty("sender").stringValue="Steve";
            so.FindProperty("subject").stringValue=flow.lesson.subject;
            so.FindProperty("body").stringValue=flow.lesson.email; so.ApplyModifiedPropertiesWithoutUndo();
            var adapter=flow.gameObject.AddComponent<Day3InboxAdapter>(); adapter.inbox=flow.inbox; adapter.lesson=flow.lesson;
            adapter.emailButton=so.FindProperty("emailButton").objectReferenceValue as Button;
            adapter.badge=(so.FindProperty("unreadBadge").objectReferenceValue as RectTransform)?.gameObject;
            adapter.rowTMP=so.FindProperty("rowLabelTMP").objectReferenceValue as TMP_Text;
            adapter.inboxTMP=so.FindProperty("inboxLabelTMP").objectReferenceValue as TMP_Text;
            adapter.rowLegacy=so.FindProperty("rowLabel").objectReferenceValue as UnityEngine.UI.Text;
            adapter.inboxLegacy=so.FindProperty("inboxLabel").objectReferenceValue as UnityEngine.UI.Text;
            if(adapter.emailButton==null || adapter.badge==null) throw new InvalidOperationException("Mail button or unread badge missing on baseline inbox.");
            flow.inbox.WindowObject.SetActive(false);
        }
        private static void InstallZoom(Day3DesktopFlow flow)
        {
            flow.zoom=All<ZoomScreenShareController>().FirstOrDefault();
            if(flow.zoom==null)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dialogue Manager/Zoom Window.prefab");
                if(prefab==null) throw new InvalidOperationException("The project's Zoom Window prefab is missing.");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab); instance.SetActive(true);
                flow.zoom=instance.GetComponentInChildren<ZoomScreenShareController>(true);
            }
            if(flow.zoom==null) throw new InvalidOperationException("Zoom controller missing.");
            flow.zoom.gameObject.SetActive(true); flow.zoom.enabled=false;
            var so=new SerializedObject(flow.zoom);
            flow.incomingCall=so.FindProperty("incomingCallPopup").objectReferenceValue as GameObject;
            flow.zoomPanel=so.FindProperty("zoomPanel").objectReferenceValue as GameObject;
            if(flow.incomingCall==null || flow.zoomPanel==null) throw new InvalidOperationException("Zoom popup and panel references must be assigned.");
            foreach(string key in new[]{"incomingCallPopup","zoomPanel","screensharePopup","screenShareDisplay"})
                if(so.FindProperty(key)?.objectReferenceValue is GameObject go) go.SetActive(false);
            // The current Zoom prefab predates WindowIdentity. Repair its scene instance only.
            int index=0;
            foreach(var drag in flow.zoom.GetComponentsInChildren<DraggableWindow>(true))
            {
                var ds=new SerializedObject(drag);
                var root=ds.FindProperty("windowRoot").objectReferenceValue as Transform;
                if(root==null) throw new InvalidOperationException("Zoom dragger has no window root.");
                var identity=ds.FindProperty("windowIdentity").objectReferenceValue as WindowIdentity;
                if(identity==null) identity=root.GetComponentsInChildren<WindowIdentity>(true).FirstOrDefault(x=>x.HasValidId);
                if(identity==null) identity=root.gameObject.AddComponent<WindowIdentity>();
                if(!identity.HasValidId)
                { var ids=new SerializedObject(identity); ids.FindProperty("windowId").stringValue="Wednesday_Zoom_"+index; ids.ApplyModifiedPropertiesWithoutUndo(); }
                ds.FindProperty("windowIdentity").objectReferenceValue=identity;
                ds.FindProperty("desktopBottomLeft").objectReferenceValue=flow.inbox.Drag.DesktopBottomLeft;
                ds.FindProperty("desktopTopRight").objectReferenceValue=flow.inbox.Drag.DesktopTopRight;
                ds.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(drag); index++;
            }
            foreach(var max in flow.zoom.GetComponentsInChildren<MaximizeWindow>(true))
            {
                var ms=new SerializedObject(max); var drag=ms.FindProperty("draggableWindow").objectReferenceValue as DraggableWindow;
                if(drag==null) continue;
                var ds=new SerializedObject(drag); ms.FindProperty("windowIdentity").objectReferenceValue=ds.FindProperty("windowIdentity").objectReferenceValue;
                ms.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(max);
            }
            foreach(var trigger in flow.incomingCall.GetComponentsInChildren<EventTrigger>(true))
            {
                bool accepts=trigger.triggers.Any(entry=>Enumerable.Range(0,entry.callback.GetPersistentEventCount()).Any(i=>entry.callback.GetPersistentMethodName(i)=="AcceptZoomCall"));
                if(!accepts) continue;
                trigger.triggers.Clear();
                var link=trigger.GetComponent<Day3DesktopIconLink>() ?? trigger.gameObject.AddComponent<Day3DesktopIconLink>();
                link.flow=flow; link.acceptCall=true; EditorUtility.SetDirty(trigger); EditorUtility.SetDirty(link); if(PrefabUtility.IsPartOfPrefabInstance(trigger)) PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
            }
            foreach(var button in flow.incomingCall.GetComponentsInChildren<Button>(true))
            {
                bool accepts=Enumerable.Range(0,button.onClick.GetPersistentEventCount()).Any(i=>button.onClick.GetPersistentMethodName(i)=="AcceptZoomCall");
                if(!accepts) continue;
                button.onClick=new Button.ButtonClickedEvent();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,flow.AcceptCall); EditorUtility.SetDirty(button); if(PrefabUtility.IsPartOfPrefabInstance(button)) PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
        }
        private static void CopySideMonitors(string desktop,string sourceDesktopName,List<string> paths)
        {
            var active=SceneManager.GetActiveScene(); string desktopPath=active.path;
            foreach(string name in new[]{"Left Monitor","Right Monitor"})
            {
                string source=AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x)==name && !x.StartsWith("Assets/Day3"));
                if(source==null) continue;
                string dest=AssetDatabase.GenerateUniqueAssetPath(Generated+"/Wednesday_"+name.Replace(" ","")+".unity");
                if(!AssetDatabase.CopyAsset(source,dest)) throw new InvalidOperationException("Could not copy monitor scene "+name);
                string side=Path.GetFileNameWithoutExtension(dest); RewriteSwitch(name,side); paths.Add(dest);
                EditorSceneManager.SaveScene(active);
                EditorSceneManager.OpenScene(dest); DisableOldFlow(); RewriteSwitch("DesktopUI",desktop); RewriteSwitch(sourceDesktopName,desktop);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                EditorSceneManager.OpenScene(desktopPath); active=SceneManager.GetActiveScene();
            }
        }
        private static void RewriteSwitch(string oldName,string newName)
        {
            foreach(var c in All<MonoBehaviour>())
            {
                if(c==null || c.GetType().Name!="ClickableSceneSwitch") continue;
                var so=new SerializedObject(c); var field=so.FindProperty("sceneName");
                if(field!=null && field.stringValue==oldName) { field.stringValue=newName; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }
        private static void EnsureEventSystem()
        {
            if(All<EventSystem>().Any()) return;
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
        }
        private static Canvas Canvas(string name,int order)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=order;
            var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            return canvas;
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=position; r.sizeDelta=size;
            return r;
        }
        private static RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        { var r=Rect(name,parent,position,size); r.gameObject.AddComponent<Image>().color=color; return r; }
        private static TMP_Text Text(string name,Transform parent,Vector2 position,Vector2 size,string text,float fontSize,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {
            var r=Rect(name,parent,position,size); var label=r.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=TMP_Settings.defaultFontAsset; label.fontSharedMaterial=label.font.material;
            label.text=text; label.fontSize=fontSize; label.color=Color.white; label.alignment=align; label.raycastTarget=false; label.richText=false;
            return label;
        }
        private static Button Button(string name,Transform parent,Vector2 position,Vector2 size,string text)
        {
            var r=Panel(name,parent,position,size,new Color(.13f,.23f,.32f)); var button=r.gameObject.AddComponent<Button>(); button.targetGraphic=r.GetComponent<Image>();
            Text("Label",r,Vector2.zero,size-new Vector2(22,12),text,26,TextAlignmentOptions.Center); return button;
        }
        private static TMP_Text ScrollText(string name,Transform parent,Vector2 position,Vector2 size,float fontSize)
        {
            var root=Panel(name,parent,position,size,new Color(.055f,.085f,.12f)); var scroll=root.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false;
            var viewport=Rect("Viewport",root,Vector2.zero,size-new Vector2(24,16)); viewport.gameObject.AddComponent<RectMask2D>();
            var label=Text("Content",viewport,Vector2.zero,new Vector2(size.x-35,size.y-20),"",fontSize);
            var rect=label.rectTransform; rect.anchorMin=new Vector2(0,1); rect.anchorMax=new Vector2(1,1); rect.pivot=new Vector2(.5f,1); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=new Vector2(0,size.y-20);
            var fitter=label.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport; scroll.content=rect; return label;
        }
        private static Day3ModalView BuildView()
        {
            var canvas=Canvas("Wednesday IDE Window",50); var view=canvas.gameObject.AddComponent<Day3ModalView>();
            var dim=Panel("Desktop shade",canvas.transform,Vector2.zero,new Vector2(1920,1080),new Color(0,0,0,.82f));
            var window=Panel("IDE",dim,new Vector2(0,65),new Vector2(1780,830),new Color(.065f,.10f,.15f));
            view.title=Text("Title",window,new Vector2(-80,370),new Vector2(1500,55),"Board.cpp",34);
            view.close=Button("Close",window,new Vector2(800,370),new Vector2(120,55),"CLOSE");
            view.progress=Text("Progress",window,new Vector2(0,310),new Vector2(1700,40),"",25);
            view.lessonPanel=Rect("Lesson",window,Vector2.zero,new Vector2(1780,830)).gameObject;
            Transform lesson=view.lessonPanel.transform;
            view.teaching=ScrollText("Voice notes",lesson,new Vector2(-470,180),new Vector2(820,190),28);
            view.code=ScrollText("Faulty code",lesson,new Vector2(-470,-10),new Vector2(820,175),28);
            view.options=new Button[3]; view.optionLabels=new TMP_Text[3];
            for(int i=0;i<3;i++) { view.options[i]=Button("Option "+i,lesson,new Vector2(-470,-155-i*95),new Vector2(820,88),""); view.optionLabels[i]=view.options[i].GetComponentInChildren<TMP_Text>(); view.optionLabels[i].alignment=TextAlignmentOptions.MidlineLeft; view.optionLabels[i].fontSize=25; }
            var grid=Rect("Test preview",lesson,new Vector2(245,120),new Vector2(300,300));
            var layout=grid.gameObject.AddComponent<GridLayoutGroup>(); layout.cellSize=new Vector2(27,27); layout.spacing=new Vector2(3,3); layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount=10;
            view.previewCells=new Image[100];
            for(int i=0;i<100;i++) { var cell=Rect("Cell "+i,grid,Vector2.zero,new Vector2(27,27)); view.previewCells[i]=cell.gameObject.AddComponent<Image>(); view.previewCells[i].color=new Color(.12f,.20f,.27f); view.previewCells[i].raycastTarget=false; }
            view.output=ScrollText("Test output",lesson,new Vector2(650,120),new Vector2(450,300),25);
            view.feedback=ScrollText("Feedback",window,new Vector2(475,-125),new Vector2(800,170),27);
            view.architectHint=Button("Architect hint",lesson,new Vector2(265,-250),new Vector2(360,55),"ASK ARCHITECT");
            view.sentinelHint=Button("Sentinel hint",lesson,new Vector2(675,-250),new Vector2(360,55),"ASK SENTINEL");
            view.run=Button("Run",lesson,new Vector2(265,-330),new Vector2(360,65),"RUN TEST");
            view.next=Button("Next",lesson,new Vector2(675,-330),new Vector2(360,65),"CONTINUE");
            view.demoPanel=Rect("Board demo",window,Vector2.zero,new Vector2(1780,830)).gameObject;
            Transform demo=view.demoPanel.transform;
            var board=Rect("Board grid",demo,new Vector2(-480,-20),new Vector2(570,570));
            var boardLayout=board.gameObject.AddComponent<GridLayoutGroup>(); boardLayout.cellSize=new Vector2(53,53); boardLayout.spacing=new Vector2(4,4); boardLayout.constraint=GridLayoutGroup.Constraint.FixedColumnCount; boardLayout.constraintCount=10;
            view.boardCells=new Button[100];
            for(int i=0;i<100;i++) { view.boardCells[i]=Button("Cell "+i,board,Vector2.zero,new Vector2(53,53),""); view.boardCells[i].gameObject.AddComponent<Day3CellHover>(); Object.DestroyImmediate(view.boardCells[i].GetComponentInChildren<TMP_Text>().gameObject); }
            for(int i=0;i<10;i++)
            {
                Text("Column "+i,demo,new Vector2(-738.5f+i*57,285),new Vector2(53,30),i.ToString(),21,TextAlignmentOptions.Center);
                Text("Row "+i,demo,new Vector2(-805,238.5f-i*57),new Vector2(35,53),i.ToString(),21,TextAlignmentOptions.Center);
            }
            view.status=Text("Checklist",demo,new Vector2(475,110),new Vector2(800,310),"",27);
            view.rotate=Button("Rotate",demo,new Vector2(265,-250),new Vector2(360,55),"ROTATE SHIP");
            view.reset=Button("Reset",demo,new Vector2(675,-250),new Vector2(360,55),"RESET BOARD");
            view.finish=Button("Finish",demo,new Vector2(475,-330),new Vector2(770,65),"FINISH BOARD DEMONSTRATION");
            view.demoPanel.SetActive(false);
            return view;
        }
    }
}
#endif
