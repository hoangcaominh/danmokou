﻿using System;
using System.Collections;
using System.Collections.Generic;
using Danmokou.Core;
using Danmokou.GameInstance;
using Danmokou.Scriptables;
using Danmokou.Services;
using UnityEngine.UIElements;
using UnityEngine.Scripting;
using static Danmokou.Core.LocalizedStrings.UI;

namespace Danmokou.UI.XML {
/// <summary>
/// Class to manage the death menu UI.
/// </summary>
public class XMLDeathMenu : PausedGameplayMenu {

    public override void FirstFrame() {
        MainScreen = new UIScreen(this, death_header, UIScreen.Display.OverlayTH)  { Builder = (s, ve) => {
            ve.AddColumn();
        }, MenuBackgroundOpacity = UIScreen.DefaultMenuBGOpacity  };
        
        _ = new UIColumn(MainScreen, null,
            #if UNITY_EDITOR
                new FuncNode("Force continue (EDITOR ONLY)", () => {
                    if (GameManagement.Instance.BasicF.ForceContinue()) {
                        CloseWithAnimationV();
                        return new UIResult.StayOnNode();
                    } else return new UIResult.StayOnNode(true);
                }),
            #endif
            (GameManagement.Instance.BasicF.ContinuesAllowed) ?
                new FuncNode(null, () => {
                    if (GameManagement.Instance.BasicF.TryContinue()) {
                        CloseWithAnimationV();
                        return new UIResult.StayOnNode();
                    } else return new UIResult.StayOnNode(true);
                    }) {EnabledIf = () => GameManagement.Instance.BasicF.ContinuesRemaining}
                    .Bind(new LabelView<int>(new(() => GameManagement.Instance.BasicF.Continues, x => death_continue_ls(x))))
                : null,
            new ConfirmFuncNode(checkpoint_restart, GameManagement.Instance.RestartFromCheckpoint)
                {EnabledIf = () => GameManagement.CanRestart && GameManagement.Instance.CanRestartCheckpoint},
            new ConfirmFuncNode(full_restart, GameManagement.Instance.Restart)
                {EnabledIf = () => GameManagement.CanRestart},
            new ConfirmFuncNode(to_menu, GameManagement.GoToMainMenu)) {
            ExitIndexOverride = 0,
            EntryIndexOverride = () => 0
        };
        
        base.FirstFrame();
    }

    protected override void BindListeners() {
        base.BindListeners();
        
        Listen(GameManagement.EvInstance, i => i.GameOver, () => ShowMeAfterFrames(5, 1f));
    }
}
}