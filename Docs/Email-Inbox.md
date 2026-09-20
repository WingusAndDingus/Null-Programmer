# Email inbox prototype

Open `Assets/Scenes/DesktopUI.unity` and press Play, or start from StartScreen.
The existing desktop mail icon now opens an overlay inbox. UI is constructed
by `MailInbox` at runtime; no additional Inspector wiring is required.
The sample sender, subject, and body can be edited on the icon's MailInbox component.
The old empty sprite window remains in the scene for other desktop work.

## Acceptance check in Unity

1. Enter Play mode. A `1` badge appears by the mail icon.
2. Click the icon. Inbox opens with one unread message and a selection prompt.
   The badge and unread count must remain until the message is selected.
3. Close without reading, then reopen: the message is still unread.
4. Select the message. Sender, subject and body appear; use the mouse wheel
   over the message to scroll. Badge disappears and the row says READ.
5. Close and reopen. The message stays read; selecting it shows the same body.
6. Repeated selection never changes any gameplay stats or duplicates messages.
7. Stop and start Play mode: the sample returns to unread.
8. Check at 1280x720, 1920x1080 and a narrow window. Close, selection and body
   should remain usable. Test keyboard Tab/Enter navigation as well.
9. Build using the Windows profile and verify StartScreen can load DesktopUI.

State survives closing/reopening the window within this desktop scene instance.
Scene reloads and application restarts reset it. Cross-scene persistence and
save files are outside this prototype. Reading currently has no task/reward effects.

Implementation is source-reviewed; Unity editor/play-mode validation is still required.
