﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using BagoumLib;
using BagoumLib.Cancellation;
using BagoumLib.DataStructures;
using BagoumLib.Events;
using BagoumLib.Mathematics;
using BagoumLib.Tasks;
using BagoumLib.Transitions;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.Scriptables;
using Danmokou.Services;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using static Danmokou.Core.DInput.InputManager;

namespace Danmokou.UI.XML {

/// <summary>
/// A UI menu that manages a set of UIScreen by rendering them to game view and handling navigation.
/// </summary>
public abstract class UIController : CoroutineRegularUpdater {
    public static readonly Event<Unit> UIEventQueued = new();
    public abstract record CacheInstruction {
        public record ToOption(int OptionIndex) : CacheInstruction;
        public record ToGroup(int? ScreenIndex, int GroupIndex) : CacheInstruction;

        public record ToGroupNode(int NodeIndex) : CacheInstruction;
    }

    /// <summary>
    /// True if the UI HTML has been built (in FirstFrame)
    /// </summary>
    protected bool Built { get; private set; } = false;
    public MVVMManager MVVM { get; private set; } = null!;
    
    /// <summary>
    /// The TemplateContainer instantiated from <see cref="UIDocument"/>.<see cref="UIDocument.sourceAsset"/>.
    /// Each UI pane can have multiple UIDocuments, each with their own <see cref="UIRoot"/>.
    /// </summary>
    public VisualElement UIRoot { get; private set; } = null!;
    
    /// <summary>
    /// The container for all UI screens on this menu. Normally, direct/only child of <see cref="UIRoot"/>.
    /// </summary>
    public VisualElement UIContainer { get; private set; } = null!;
    public PanelSettings UISettings { get; private set; } = null!;

    private VisualCursor? _visualCursor;
    public VisualCursor VisualCursor => _visualCursor ??= new(this);

    public virtual bool CloseOnUnscopedBack => false;
    protected virtual bool CaptureFallthroughInteraction => true;
    protected virtual bool OpenOnInit => true;
    protected virtual Color BackgroundTint => new(0.17f, 0.05f, 0.20f);
    protected virtual UINode? StartingNode => null;
    protected virtual UIScreen?[] Screens => new[] {MainScreen};
    
    /// <summary>
    /// Event issued when the UI receives a visual update. Screens may subscribe to this
    ///  in order to control their rendering.
    /// </summary>
    public Event<float> UIVisualUpdateEv { get; } = new();
    
    /// <summary>
    /// Controls the opacity of the entire menu.
    /// Generally 0, except on pause menus.
    /// <br/>The background that is displayed is generally a flat color.
    /// </summary>
    public PushLerper<float> BackgroundOpacity { get; } = 
        new(0.5f, (a, b, t) => M.Lerp(a, b, Easers.EIOSine(t)));
    
    public UIScreen MainScreen { get; protected set; } = null!;
    
    /// <summary>
    /// Whether or not the UI should update based on player input in RegularUpdate.
    /// </summary>
    public DisturbedAnd PlayerInputEnabled { get; } = new();
    /// <summary>
    /// Whether or not the UI should allow any update operations at all.
    /// </summary>
    public DisturbedAnd OperationsEnabled { get; } = new();
    protected bool RegularUpdateGuard => ETime.FirstUpdateForScreen && PlayerInputEnabled;
    public long LastOperationFrame { get; private set; } = -1;
    public override EngineState UpdateDuring => EngineState.MENU_PAUSE;
    public override int UpdatePriority => UpdatePriorities.UI;
    
    public Stack<UINode> ScreenCall { get; } = new();
    public Stack<(UIGroup group, UINode? node)> GroupCall { get; } = new();
    public UINode? NextNodeInGroupCall {
        get {
            foreach (var (_, n) in GroupCall)
                if (n != null)
                    return n;
            return null;
        }
    }
    
    private readonly TaskQueue openClose = new();
    public UINode? Current { get; private set; }
    
    /// <summary>
    /// True if the menu is active (ie. some node on the menu is selected and the menu is open).
    /// </summary>
    public bool MenuActive => Current != null;
    
    /// <summary>
    /// True if this menu is capable of consuming input. This is true by default, except for certain
    ///  "pass-through" menus which allow input to fall through to lower-priority menus
    ///  when their Unselector node is current.
    /// </summary>
    public virtual bool CanConsumeInput => true;
    
    /// <summary>
    /// Returns true iff this menu is <see cref="MenuActive"/> and <see cref="CanConsumeInput"/>,
    ///  and is the highest-priority such menu. This is only true for one menu at a time, and only that
    ///  menu should perform input handling.
    /// </summary>
    protected bool IsActiveCurrentMenu => uiRenderer.IsHighestPriorityActiveMenu(this);
    
    public OverrideEvented<ICursorState> CursorState { get; } = new(new NullCursorState());
    /// <summary>
    /// Event-driven UI input (based on UITK bindings).
    /// Stored temporarily and only read during <see cref="RegularUpdate"/>.
    /// </summary>
    public UIPointerCommand? QueuedInput { get; private set; }
    private UICommand? CurrentInputCommand =>
        UILeft ? UICommand.Left :
        UIRight ? UICommand.Right :
        UIUp ? UICommand.Up :
        UIDown ? UICommand.Down :
        UIConfirm ? UICommand.Confirm :
        UIBack ? UICommand.Back :
        UIContextMenu ? UICommand.ContextMenu :
        null;
    /// <summary>
    /// The most recent location of the mouse pointer. Note that this will not capture changes
    ///  in the mouse pointer when it is not hovering over this menu's pickable HTML.
    /// </summary>
    public Vector2? LastPointerLocation { get; private set; } = null;
    
    //The reason for this virtual structure is because implementers (eg. XMLMainMenuCampaign)
    // need to store the list *statically*, since the specific menu object will be deleted on scene change.
    protected virtual List<CacheInstruction>? ReturnTo { get; set; }
    //The point we want to return to (eg. boss card selection) is usually not the node that causes the action
    // (eg. the difficulty select button), so we have a two-step add-commit process.
    private List<CacheInstruction>? tentativeReturnTo;

    public PopupUIGroup? TargetedPopup { get; set; } = null;
    private UIBuilderRenderer uiRenderer = null!;
    private readonly Queue<Func<Task>> uiOperations = new();

    //Note: it should be possible to use Awake instead of FirstFrame w.r.t UIDocument being instantiated, 
    // but many menus depend on binding to services,
    // and services are not reliably queryable until FirstFrame.
    public override void FirstFrame() {
        uiRenderer = ServiceLocator.Find<UIBuilderRenderer>();
        MVVM = new();
        var uid = GetComponent<UIDocument>();
        //higher sort order is more visible, so give them lower priority
        tokens.Add(uiRenderer.RegisterController(this, -(int)(uid.panelSettings.sortingOrder * 1000 + uid.sortingOrder)));
        UIRoot = uid.rootVisualElement;
        UIRoot.RegisterCallback<PointerMoveEvent>(ev => LastPointerLocation = ev.position);
        UIRoot.RegisterCallback<PointerUpEvent>(ev => TargetedPopup = null, TrickleDown.TrickleDown);
        UIRoot.RegisterCallback<PointerUpEvent>(ev => {
            //Use default context menu only; don't call the real context menu functions for Curr
            if (Current is { UseDefaultContextMenu: true } curr && ev.button is 1)
                uiOperations.Enqueue(() => 
                    Current != curr ? Task.CompletedTask : 
                        OperateOnResultAnim(PopupUIGroup.CreateContextMenu(curr, null, true, false)));
        });
        UIContainer = UIRoot.Q("UIContainer");
        UISettings = uid.panelSettings;
        Build();
        UIRoot.style.opacity = 0;
        UIRoot.style.display = OpenOnInit.ToStyle();
        UIRoot.style.width = new Length(100, LengthUnit.Percent);
        UIRoot.style.height = new Length(100, LengthUnit.Percent);
        if (!CaptureFallthroughInteraction) {
            //Normally, the UI container will capture any pointer events not on nodes,
            //but for the persistent interactive menu, we want such events to fall through
            //to canvas/etc.
            UIRoot.pickingMode = PickingMode.Ignore;
            UIContainer.pickingMode = PickingMode.Ignore;
        }
        tokens.Add(BackgroundOpacity.Subscribe(f => UIContainer.style.unityBackgroundImageTintColor = 
            BackgroundTint.WithA(f)));
        BackgroundOpacity.Push(0);
        tokens.Add(PlayerInputEnabled.AddDisturbance(OperationsEnabled));
        if (OpenOnInit) {
            ((Func<Task>)(async () => {
                await Open();
                await DoReturn();
            }))().Log();
        }
    }
    
    public void QueueInput(UIPointerCommand cmd, bool overrideIfExists = true) {
        //We allow move-to-node commands to be queued while the menu is animating
        if (!OperationsEnabled && cmd is not UIPointerCommand.Goto) return;
        if (!overrideIfExists && QueuedInput is not null) return;
        QueuedInput = cmd;
        UIEventQueued.OnNext(default);
    }

    private async Task DoReturn() {
        if (ReturnTo == null) return;
        if (Current == null)
            throw new Exception("ReturnTo exists, but Current is null");
        foreach (var inst in ReturnTo) {
            switch (inst) {
                case CacheInstruction.ToGroup toGroup:
                    await OperateOnResultFast(new UIResult.GoToNode(
                        toGroup.ScreenIndex == null ? 
                            Current.Group.Screen.Groups[toGroup.GroupIndex].EntryNode :
                            Screens[toGroup.ScreenIndex.Value]!.Groups[toGroup.GroupIndex].EntryNode));
                    break;
                case CacheInstruction.ToGroupNode toGroupNode:
                    await OperateOnResultFast(new UIResult.GoToNode(Current.Group.Nodes[toGroupNode.NodeIndex]));
                    break;
                case CacheInstruction.ToOption toOption:
                    if (Current is IBaseLROptionNode opt) {
                        opt.Index = toOption.OptionIndex;
                    } else
                        throw new Exception("Couldn't rebuild menu position: node is not an option");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inst));
            }
        }
        ReturnTo = null;
    }

    protected void Build() {
        foreach (var s in Screens)
            if (s != null)
                UIContainer.Add(s.Build(XMLUtils.Prefabs.TypeMap));
        Built = true;
    }
    protected void BuildLate(UIScreen s) => 
        UIContainer.Add(s.Build(XMLUtils.Prefabs.TypeMap));


    private readonly Dictionary<UINode, UINodeSelection> states = new();
    /// <summary>
    /// Redraws the current screen, or disables the UI display if there is no current node.
    /// <br/>Other screens are not affected.
    /// </summary>
    public void Redraw() {
        //This can occur if this gets called before FirstFrame
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (UIRoot == null) return;
        if (Current == null) {
            UIRoot.style.display = DisplayStyle.None;
            return;
        }
        Profiler.BeginSample("UI Redraw");
        UIRoot.style.display = DisplayStyle.Flex;
        states.Clear();
        var nextIsPopupSrc = Current.Group is PopupUIGroup;
        foreach (var (grp, node) in GroupCall) {
            if (node != null)
                states[node] = nextIsPopupSrc ? UINodeSelection.PopupSource : UINodeSelection.GroupCaller;
            nextIsPopupSrc = grp is PopupUIGroup;
        }
        foreach (var n in Current.Group.Nodes)
            states[n] = UINodeSelection.GroupFocused;
        states[Current] = UINodeSelection.Focused;
        //Other screens don't need to be redrawn
        var dependentGroups = Current.Group.Hierarchy.SelectMany(g => (g as CompositeUIGroup)?.Components ?? (IEnumerable<UIGroup>)Array.Empty<CompositeUIGroup>()).ToHashSet();
        foreach (var g in Current.Screen.Groups) {
            var fallback = dependentGroups.Contains(g) ? UINodeSelection.GroupFocused : UINodeSelection.Default;
            foreach (var n in g.Nodes)
                n.UpdateSelection(states.GetValueOrDefault(n, fallback));
        }
        RunDroppableRIEnumerator(scrollToCurrent());
        Profiler.EndSample();
    }
    //Workaround for limitation that cannot ScrollTo to objects that have just been constructed or made visible
    private IEnumerator scrollToCurrent() {
        yield return null;
        while (!ETime.FirstUpdateForScreen) yield return null;
        Current?.ScrollTo();
    }

    public UIResult Navigate(UINode from, UICommand cmd) => 
        CursorState.Value.Navigate(from, cmd);
    
    /// <inheritdoc cref="OperateOnResult"/>
    private async Task _OperateOnResult(UIResult? result, UITransitionOptions? opts) {
        if (result == null || (result is UIResult.GoToNode {NoOpIfSameNode:true} gTo && gTo.Target == Current) ||
            (result is UIResult.GoToSibling {NoOpIfSameNode:true} gS && gS.Index == Current?.IndexInGroup) ||
            result is UIResult.StayOnNode) {
            if (result != null) Redraw();
            return;
        }
        LastOperationFrame = ETime.FrameNumber;
        using var token = OperationsEnabled.AddConst(false);
        var closeMenuAfter = false;
        opts ??= UITransitionOptions.DontAnimate;
        var next = Current;
        List<(UIGroup group, UINode? node)> LeftCalls = new();
        List<(UIGroup group, UINode? node)> EntryCalls = new();
        List<(UINode src, bool isPush)>? screenChanges = null;
        void RecordScreenChange(UINode? prev, bool isDescend) {
            if (prev != null)
                (screenChanges ??= new()).Add((prev, isDescend));
        }
        foreach (var _r in new[] { result }.Unroll()) {
            var r = _r;
            while (true) {
                if (r is UIResult.Lazy lazy)
                    r = lazy.Delayed();
                else if (r is UIResult.AfterTask afterTask)
                    r = await afterTask.Delayed();
                else
                    break;
            }
            var prev = next;
            void TransferToNodeSameScreen(UINode target) {
                if (target.Group != prev?.Group) {  
                    var th = target.Group.Hierarchy;
                    var ch = prev?.Group.Hierarchy;
                    var intersection = UIGroupHierarchy.GetIntersection(ch, th);
                    var lastSource = (group: prev?.Group!, node: prev);
                    //Pop until we reach the intersection
                    for (var x = ch; x != intersection; x = x!.Parent) {
                        if (lastSource.group != null)
                            LeftCalls.Add(lastSource);
                        lastSource = GroupCall.Pop();
                    }
                    //If the intersection is the target group, then add an extra LeftCall and EntryCall for the node
                    // being swapped out at the intersection, so RemovedFromGroupStack can be called properly
                    if (th == intersection) {
                        if (lastSource.node != target) {
                            LeftCalls.Add(lastSource);
                            EntryCalls.Add((lastSource.group, target));
                        }
                    } else
                        //Push until we reach the target
                        foreach (var x in th.PrefixRemainder(intersection)) {
                            if (lastSource.group != null)
                                GroupCall.Push(lastSource);
                            lastSource = (x, x == target.Group ? target : 
                                (x.HasInteractableNodes ? 
                                    (x.Nodes.Contains(x.EntryNode) ? x.EntryNode : x.FirstInteractableNode) : 
                                    null));
                            EntryCalls.Add(lastSource);
                        }
                }
                next = target;
            }
            void ReturnToGroupOf((UIGroup grp, UINode? target) n) {
                var (grp, target) = n;
                //pass group separately since target may be destroyed and detached from group
                if (target == null)
                    throw new Exception("Return-to-group resulted in a null node");
                if (target.Destroyed || !target.AllowInteraction) {
                    if (grp.MaybeEntryNode is {} en)
                        next = en;
                    else {
                        //if the group is now empty due to node deletion,
                        // move navigation upwards
                        while (GroupCall.TryPop(out var _n)) {
                            LeftCalls.Add((grp, target));
                            (grp, target) = _n;
                            if (target != null) {
                                ReturnToGroupOf((grp, target));
                                return;
                            }
                        }
                        throw new Exception("Couldn't send navigation upwards after group destruction");
                    }
                } else
                    next = target;
            }
            switch (r) {
                case UIResult.CloseMenuFast:
                    next = null;
                    opts = UITransitionOptions.DontAnimate;
                    if (prev != null)
                        LeftCalls.Add((prev.Group, prev));
                    LeftCalls.AddRange(GroupCall);
                    GroupCall.Clear();
                    ScreenCall.Clear();
                    break;
                case UIResult.CloseMenu:
                    closeMenuAfter = true;
                    break;
                case UIResult.GoToScreen goToScreen:
                    if (goToScreen.Screen != prev?.Screen && (goToScreen.ReturnToOverride ?? prev) is { } returnTo) {
                        RecordScreenChange(prev, true);
                        ScreenCall.Push(returnTo);
                        prev = null;
                        TransferToNodeSameScreen(goToScreen.Screen.Groups[0].EntryNode);
                    }
                    break;
                case UIResult.GoToNode goToNode:
                    if (goToNode.Target.Destroyed)
                        break;
                    if (goToNode.Target.Screen != prev?.Screen && prev != null) {
                        RecordScreenChange(prev, true);
                        ScreenCall.Push(prev);
                        prev = null;
                    }
                    TransferToNodeSameScreen(goToNode.Target);
                    break;
                case UIResult.GoToSibling gSib:
                    if (next is null)
                        throw new Exception("Cannot go to the sibling of a null node");
                    var target = next.Group.Nodes.Try(gSib.Index) ??
                                 throw new Exception($"Node {next}'s group does not have {gSib.Index} nodes");
                    if (target.Destroyed)
                        break;
                    TransferToNodeSameScreen(target);
                    break;
                case UIResult.ReturnToGroupCaller rg:
                    var pn = (prev!.Group, node: prev);
                    for (int ii = 0; ii < rg.Ascensions; ++ii) {
                        do {
                            if (GroupCall.Count == 0)
                                throw new Exception("Stack empty, couldn't return to group caller");
                            LeftCalls.Add(pn);
                            pn = GroupCall.Pop()!;
                        } while (pn.node is null);
                    }
                    ReturnToGroupOf(pn);
                    break;
                case UIResult.ReturnToTargetGroupCaller rtg:
                    if (prev == null) throw new Exception("Current must be present for return-to-target op");
                    var pgn = (prev.Group, node: prev);
                    while (pgn.Group != rtg.Target) {
                        if (GroupCall.Count == 0)
                            throw new Exception("Stack empty, couldn't return to target group caller");
                        LeftCalls.Add(pgn);
                        pgn = GroupCall.Pop()!;
                    }
                    if (pgn.node != prev)
                        ReturnToGroupOf(pgn);
                    break;
                case UIResult.ReturnToScreenCaller sc:
                    if (prev == null) throw new Exception("Current must be present for return-to-screen op");
                    var ngn = (prev.Group, (UINode?)prev);
                    for (int ii = 0; ii < sc.Ascensions; ++ii)
                        if (ScreenCall.TryPop(out var s)) {
                            RecordScreenChange(s, false);
                            while (GroupCall.TryPeek(out var g) && (g.group.Screen == ngn.Group.Screen)) {
                                LeftCalls.Add(ngn);
                                ngn = GroupCall.Pop();
                            }
                            LeftCalls.Add(ngn);
                            ngn = (s.Group, next = s);
                        }
                    if (next == null)
                        throw new Exception("Return-to-screen resulted in a null node");
                    break;
            }
        }

        var leftGroups = new List<UIGroup>();
        foreach (var (gl, nl) in LeftCalls) {
            foreach (var (ge, ne) in EntryCalls) {
                if (gl == ge) {
                    if (nl != ne)
                        nl?.RemovedFromNavHierarchy();
                    goto end;
                }
            }
            nl?.RemovedFromNavHierarchy();
            leftGroups.Add(gl);
            end: ;
        }
        var entryGroups = new List<UIGroup>();
        foreach (var (ge, ne) in EntryCalls) {
            foreach (var (gl, nl) in LeftCalls) {
                if (gl == ge) {
                    if (nl != ne)
                        ne?.AddedToNavHierarchy();
                    goto end;
                }
            }
            ne?.AddedToNavHierarchy();
            entryGroups.Add(ge);
            end: ;
        }
        if (LeftCalls.Count == 0 && EntryCalls.Count == 0 && next != Current) {
            Current?.RemovedFromNavHierarchy();
            next?.AddedToNavHierarchy();
        }
        //Logs.Log($"Left: {string.Join(",", LeftCalls.Select(x => x.ToString()))}, Entered: {string.Join(",", EntryCalls.Select(x => x.ToString()))}");
        //Logs.Log($"Left: {string.Join(",", leftGroups.Select(x => x.ToString()))}, Entered: {string.Join(",", entryGroups.Select(x => x.ToString()))}");
        await TransitionToNode(next, opts, leftGroups, entryGroups, screenChanges);
        result.OnPostTransition?.Invoke();
        if (closeMenuAfter)
            await CloseWithAnimation();
        LastOperationFrame = ETime.FrameNumber;
    }

    /// <summary>
    /// Modify the UI based on <see cref="UIResult"/>.
    /// </summary>
    /// <param name="result">Result to execute.</param>
    /// <param name="opts">UI transition options. If null, defaults to DontAnimate.</param>
    public Task OperateOnResult(UIResult? result, UITransitionOptions? opts) {
        if (uiOperations.Count == 0 && OperationsEnabled)
            return _OperateOnResult(result, opts);
        var tcs = new TaskCompletionSource<Unit>();
        uiOperations.Enqueue(() => _OperateOnResult(result, opts).Pipe(tcs));
        return tcs.Task;
    }

    public Task OperateOnResultAnim(UIResult? result) => OperateOnResult(result, UITransitionOptions.Default);

    public Task OperateOnResultFast(UIResult? result) => OperateOnResult(result, UITransitionOptions.DontAnimate);
    
    public override void RegularUpdate() {
        base.RegularUpdate();
        if (ETime.FirstUpdateForScreen && Current != null) {
            UIVisualUpdateEv.OnNext(ETime.ASSUME_SCREEN_FRAME_TIME);
            BackgroundOpacity.Update(ETime.ASSUME_SCREEN_FRAME_TIME);
        }
        while (ETime.FirstUpdateForScreen) {
            if (OperationsEnabled && uiOperations.TryDequeue(out var nxt))
                _ = nxt();
            else if (PlayerInputEnabled && IsActiveCurrentMenu && Current != null) {
                bool doCustomSFX = false;
                UICommand? command = null;
                UIResult? result = null;
                bool silence = false;
                if (QueuedInput is UIPointerCommand.Goto mgt) {
                    if (!mgt.Target.Destroyed && mgt.Target.Screen == Current.Screen &&
                        mgt.Target != Current && mgt.CanTraverse) {
                        result = CursorState.Value.PointerGoto(Current, mgt.Target);
                        if (result is UIResult.GoToNode { Target: {} target} && target != Current)
                            command = (Current.Group is UIColumn) == (target.Group == Current.Group) ?
                                UICommand.Up : UICommand.Right;
                    }
                } else if (CursorState.Value.CustomEventHandling(Current).Try(out var r)) {
                    result = r;
                    doCustomSFX = true;
                } else {
                    command =
                        (QueuedInput is UIPointerCommand.NormalCommand uic && QueuedInput.ValidForCurrent(Current)) ?
                            uic.Command :
                            CurrentInputCommand;
                    if (command.Try(out var cmd))
                        result = Navigate(Current, cmd);
                    silence = (QueuedInput as UIPointerCommand.NormalCommand)?.Silent ?? silence;
                }
                QueuedInput = null;
                var prefabs = XMLUtils.Prefabs;
                if (result is UIResult.GoToNode gTo && gTo.Target == Current)
                    result = new UIResult.StayOnNode(gTo.NoOpIfSameNode);
                if (result != null && !silence) {
                    ISFXService.SFXService.Request(
                        result switch {
                            UIResult.StayOnNode { Action: UIResult.StayOnNodeType.NoOp } => prefabs.FailureSound,
                            UIResult.StayOnNode { Action: UIResult.StayOnNodeType.Silent } => null,
                            _ => command switch {
                                UICommand.Left => prefabs.LeftRightSound,
                                UICommand.Right => prefabs.LeftRightSound,
                                UICommand.Up => prefabs.UpDownSound,
                                UICommand.Down => prefabs.UpDownSound,
                                UICommand.Confirm => prefabs.ConfirmSound,
                                UICommand.Back => prefabs.BackSound,
                                UICommand.ContextMenu => prefabs.ShowOptsSound,
                                _ => null
                            }
                        });
                } else if (doCustomSFX)
                    //Probably need to get this from the custom node handling? idk
                    ISFXService.SFXService.Request(prefabs.LeftRightSound);
                if (result != null && result is not UIResult.StayOnNode)
                    OperateOnResult(result, result.Options ?? UITransitionOptions.Default).Log();
                break;
            } else
                break;
        }
    }

    private void LateUpdate() {
        MVVM.UpdateViews();
        _visualCursor?.Update();
    }

    #region Transition

    public void GoToNth(int grpIndex, int nodeIndex) =>
        OperateOnResultFast(new UIResult.GoToNode(MainScreen.Groups[grpIndex].Nodes[nodeIndex]));
    
    [ContextMenu("Open menu")]
    protected Task Open() {
        if (MenuActive) return Task.CompletedTask;
        UIRoot.style.opacity = 1;
        if (StartingNode != null)
            return OperateOnResultFast(new UIResult.GoToNode(StartingNode));
        else {
            foreach (var g in Screens.First(x => x != null)!.Groups) 
                if (g.MaybeEntryNode is {} en) 
                    return OperateOnResultFast(new UIResult.GoToNode(en));
            throw new Exception($"Couldn't open menu {gameObject.name}");
        }
    }

    [ContextMenu("Close menu")]
    protected async Task Close() {
        if (!MenuActive) return;
        UIRoot.style.opacity = 0;
        //if this is called within an enclosing CloseMenu, then OperateOnResult will hang
        await _OperateOnResult(new UIResult.CloseMenuFast(), UITransitionOptions.DontAnimate);
        OnClosed();
    }
    
    protected virtual void OnWillOpen() { }
    protected virtual void OnWillClose() { }
    protected virtual void OnClosed() { }
    
    protected Task OpenWithAnimation(Task? anim = null) {
        return openClose.EnqueueTask(async () => {
            if (MenuActive) return;
            OnWillOpen();
            using var disable = PlayerInputEnabled.AddConst(false);
            var openTask = Open(); //sets opacity to 1
            UIRoot.style.opacity = 0f;
            anim ??= UIRoot.FadeTo(1, 0.3f, x => x).Run(this);
            await Task.WhenAll(openTask, anim);
        });
    }
    
    protected Task CloseWithAnimation(Task? anim = null) {
        return openClose.EnqueueTask(async () => {
            if (!MenuActive) return;
            OnWillClose();
            anim ??= UIRoot.FadeTo(0, 0.3f, x => x).Run(this);
            using var disable = PlayerInputEnabled.AddConst(false);
            await anim;
            await Close();
        });
    }

    [ContextMenu("Animate open menu")]
    protected void OpenWithAnimationV() {
        OpenWithAnimation().Log();
    }
    
    [ContextMenu("Animate close menu")]
    protected void CloseWithAnimationV() {
        CloseWithAnimation().Log();
    }

    protected List<CacheInstruction> GetInstructionsToNode(UINode? c) {
        var revInds = new List<CacheInstruction>();
        var groupStack = new Stack<(UIGroup group, UINode? node)>(GroupCall.Reverse());
        var screenStack = new Stack<UINode>(ScreenCall.Reverse());
        void GoToThisNode() {
            revInds.Add(new CacheInstruction.ToGroupNode(c.IndexInGroup));
            for (int ii = c.Group.Nodes.Count - 1; ii >= 0; --ii)
                if (c.Group.Nodes[ii] is IBaseLROptionNode opt) {
                    revInds.Add(new CacheInstruction.ToOption(opt.Index));
                    revInds.Add(new CacheInstruction.ToGroupNode(((UINode)opt).IndexInGroup));
                }
        }
        while (c != null) {
            if ((groupStack.TryPeek(out var g) && (g.group.Screen == c.Screen))) {
                GoToThisNode();
                revInds.Add(new CacheInstruction.ToGroup(null, c.Screen.Groups.IndexOf(c.Group)));
                var (g_, c_) = groupStack.Pop();
                c = c_ ?? g_.EntryNode;
            } else if (screenStack.TryPeek(out _)) {
                GoToThisNode();
                revInds.Add(new CacheInstruction.ToGroup(Screens.IndexOf(c.Screen), c.Screen.Groups.IndexOf(c.Group)));
                c = screenStack.Pop();
            }  else {
                GoToThisNode();
                c = null;
            }
        }
        revInds.Reverse();
        return revInds;
    }
    public void TentativeCache(UINode node) {
        tentativeReturnTo = GetInstructionsToNode(node);
    }

    public void ConfirmCache() {
        if (tentativeReturnTo != null)
            Logs.Log($"Caching menu position with indices " +
                     $"{string.Join(", ", tentativeReturnTo.Select(x => x.ToString()))}");
        ReturnTo = tentativeReturnTo;
    }

    private async Task TransitionToNode(UINode? next, UITransitionOptions opts, List<UIGroup> leftGroups, List<UIGroup> enteredGroups, List<(UINode src, bool isPush)>? screenChanges) {
        var prev = Current;
        if (prev == next) {
            return;
        }
        //NB: screenChanges only records screen->screen transitions, not null->screen or screen->null transitions.
        // That's why we define screenChanged separately.
        var screenChanged = next?.Screen != prev?.Screen;
        if (screenChanged) {
            prev?.Screen.NextState(UIScreenState.ActiveWillGoInactive);
            next?.Screen.NextState(UIScreenState.InactiveWillGoActive);
        }
        PopupUIGroup.Type? popupType = (
            (leftGroups.Count == 0 || leftGroups[^1] is PopupUIGroup { Typ: PopupUIGroup.Type.CtxMenu })
                && enteredGroups.Try(0) is PopupUIGroup pug) 
                    ? pug.Typ : null;
        prev?.Leave(opts.Animate, CursorState.Value, popupType);
        //Also issue leave command to the parent nodes of actioning context menus
        for (var ii = 0; ii < leftGroups.Count; ii++) {
            if (leftGroups[ii] is PopupUIGroup { Typ: PopupUIGroup.Type.CtxMenu } pug1 
                    && (enteredGroups.Count > 0 || ii + 1 < leftGroups.Count))
                pug1.Source.Leave(opts.Animate, CursorState.Value, popupType);
        }

        //First phase tasks
        var tasks = new List<Task>();
        //The last ReturnFromChild should be executed in the second phase,
        // since it usually has functionality similar to Enter
        //Store this before running LeaveGroup since LeaveGroup may destroy groups and set Parent null
        var lastReturnParent = leftGroups.Try(leftGroups.Count - 1)?.Parent;
        for (var ii = 0; ii < leftGroups.Count; ii++) {
            var g = leftGroups[ii];
            if (ii < leftGroups.Count - 1 && g.Parent?.ReturnFromChild() is { IsCompletedSuccessfully: false } ptask)
                tasks.Add(ptask);
            if (g.LeaveGroup() is { IsCompletedSuccessfully: false } task)
                tasks.Add(task);
        }
        //Handle first DescendToChild in EnterGroups
        if (enteredGroups.Try(0)?.Parent?.DescendToChild(popupType) is { IsCompletedSuccessfully: false} dtask)
            tasks.Add(dtask);
        
        if (screenChanges != null)
            foreach (var (src, isPush) in screenChanges)
                if (!isPush && src.Group.ReturnFromChild() is { IsCompletedSuccessfully: false } task)
                    tasks.Add(task);
        if (tasks.Count > 0)
            await tasks!.All();
        //Screen change
        if (screenChanged) {
            prev?.Screen.NextState(UIScreenState.ActiveGoingInactive);
            next?.Screen.NextState(UIScreenState.InactiveGoingActive);
        }
        tasks.Clear();
        //Second phase tasks
        if (screenChanges != null)
            foreach (var (src, isPush) in screenChanges)
                if (isPush && src.Group.DescendToChild(popupType) is { IsCompletedSuccessfully: false } task)
                    tasks.Add(task);
        //Handle last ReturnFromChild in LeftGroups
        if (lastReturnParent?.ReturnFromChild() is { IsCompletedSuccessfully: false} ltask)
            tasks.Add(ltask);
        for (var ii = 0; ii < enteredGroups.Count; ii++) {
            var g = enteredGroups[ii];
            //The first DescendToChild should be executed in the first phase,
            // since it usually has functionality similar to Leave
            if (ii > 0 && g.Parent?.DescendToChild(popupType) is { IsCompletedSuccessfully: false } ptask)
                tasks.Add(ptask);
            if (g.EnterGroup() is { IsCompletedSuccessfully: false } task)
                tasks.Add(task);
        }

        Current = next;
        next?.Enter(opts.Animate, CursorState.Value);
        Redraw();
        
        if (screenChanged && next != null)
            tasks.Add(TransitionScreen(prev?.Screen, next.Screen, opts));
        if (tasks.Count > 0)
            await tasks!.All();

        if (screenChanged) {
            prev?.Screen.NextState(UIScreenState.Inactive);
            next?.Screen.NextState(UIScreenState.Active);
        }
        if (next == null) {
            ScreenCall.Clear();
            GroupCall.Clear();
            //uiOperations.Clear();
        }
    }

    public virtual async Task TransitionScreen(UIScreen? from, UIScreen to, UITransitionOptions opts) {
        var hideFrom = from != null && from.State == UIScreenState.ActiveGoingInactive;
        var showTo = to.State == UIScreenState.InactiveGoingActive;
        if (from == null || !opts.Animate) {
            if (hideFrom) {
                from!.HTML.transform.position = Vector3.zero;
                from.HTML.style.opacity = 0;
                if (from.SceneObjects != null)
                    from.SceneObjects.transform.position = Vector3.zero;
            }
            if (showTo) {
                to.HTML.transform.position = Vector3.zero;
                to.HTML.style.opacity = 1;
                if (to.SceneObjects != null)
                    to.SceneObjects.transform.position = Vector3.zero;
            }
            return;
        }
        var t = opts.ScreenTransitionTime;
        var ep = GetRandomSlideEndpoint();
        var eput = new Vector2(ep.x * MainCamera.MCamInfo.ScreenWidth, ep.y * MainCamera.MCamInfo.ScreenHeight);
        async Task FadeIn() {
            if (to.SceneObjects != null)
                to.SceneObjects.transform.position = -eput;
            to.HTML.style.opacity = 0;
            if (opts.DelayScreenFadeInRatio > 0)
                await RUWaitingUtils.WaitForUnchecked(this, Cancellable.Null, t * opts.DelayScreenFadeInRatio,
                    false);
            await Task.WhenAll(
                to.HTML.FadeTo(1, t, Easers.EIOSine).Run(this),
                to.SceneObjects == null ?
                    Task.CompletedTask :
                    to.SceneObjects.transform.GoTo(Vector3.zero, t, Easers.EIOSine).Run(this)
            );
        }
        await Task.WhenAll(
            !hideFrom ? Task.CompletedTask :
                from.HTML.FadeTo(0, opts.ScreenTransitionTime, Easers.EIOSine).Run(this),
            from.SceneObjects == null || !hideFrom ? Task.CompletedTask : 
                from.SceneObjects.transform.GoTo(eput, opts.ScreenTransitionTime, Easers.EIOSine).Run(this),
            !showTo ? Task.CompletedTask : FadeIn()
        );
    }
    
    
    private Vector2[] slideEndpoints => new[] {
        new Vector2(-1, 0),
        new Vector2(1, 0),
        new Vector2(0, -1),
        new Vector2(0, 1)
    };

    private Vector2 GetRandomSlideEndpoint() {
        return RNG.RandSelectOffFrame(slideEndpoints);
    }
    
    #endregion

    /// <summary>
    /// If a node dynamically becomes passthrough/invisible/deleted,
    ///  or for any other reason needs to ensure that it is not the current node,
    ///  it should call this function.
    /// </summary>
    /// <returns>True iff the cursor was moved (which also redraws the screen).</returns>
    public bool MoveCursorAwayFromNode(UINode n) {
        if (n == Current && OperationsEnabled) {
            if (!n.Destroyed) n.Leave(false, CursorState.Value, null);
            if (n.Group.Destroyed)
                Current = null;
            else
                Current = n.Group.ExitNode;
            Redraw();
            return true;
        } else
            return false;
    }

    public Task PlayAnimation(ITransition anim) => anim.Run(this, CoroutineOptions.DroppableDefault);

    protected override void OnDisable() {
        foreach (var s in Screens) {
            s?.MarkScreenDestroyed();
        }
        base.OnDisable();
    }


    [ContextMenu("Debug group call stack")]
    public void DebugGroupCallStack() => Logs.Log(string.Join("; ", 
        GroupCall.Select(gn => $"{gn.node?.IndexInGroup}::{gn.group}")));
    [ContextMenu("Debug group hierarchy")]
    public void DebugGroupHierarcy() => Logs.Log(Current?.Group.ToString() ?? "No current node");
}
}