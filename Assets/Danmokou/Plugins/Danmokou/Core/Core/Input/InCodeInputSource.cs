﻿using System.Collections.Generic;

namespace Danmokou.Core.DInput {
public class InCodeInputSource : IInputHandlerInputSource {
    public List<IInputHandler> Handlers { get; } = new();
    public bool AnyKeyPressedThisFrame { get; private set; }
    bool IInputHandlerInputSource.AnyKeyPressedThisFrame {
        get => AnyKeyPressedThisFrame;
        set => AnyKeyPressedThisFrame = value;
    }
    private readonly Queue<(MockInputBinding, bool)> delayedEnables = new();
    public InCodeInputSource() {
        mDialogueConfirm = new(this);
        mDialogueSkipAll = new(this);
        dialogueConfirm = InputHandler.Trigger(mDialogueConfirm);
        dialogueSkipAll = InputHandler.Trigger(mDialogueSkipAll);
        Handlers.AddRange(new[] {
            dialogueConfirm, dialogueSkipAll
        });
    }
    
    public bool OncePerUnityFrameToggleControls() {
        var nUpdates = delayedEnables.Count;
        while (nUpdates-- > 0) {
            var (m, e) = delayedEnables.Dequeue();
            m._AddCounter(e ? 1 : -1);
            if (e)
                delayedEnables.Enqueue((m, false));
        }
        return ((IInputHandlerInputSource)this).OncePerUnityFrameUpdateHandlers();
    }

    public void SetActive(MockInputBinding m) => delayedEnables.Enqueue((m, true));

    public MockInputBinding mDialogueConfirm { get; }
    public MockInputBinding mDialogueSkipAll { get; }
    private IInputHandler dialogueConfirm { get; }
    private IInputHandler? dialogueSkipAll { get; }
    public bool? DialogueConfirm => dialogueConfirm.Active ? true : null;
    public bool? DialogueSkipAll => (dialogueSkipAll?.Active is true) ? true : null;
}
}