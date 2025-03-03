﻿using System;
using BagoumLib.Cancellation;
using BagoumLib.Tasks;
using Danmokou.Core;
using Danmokou.DMath;
using UnityEngine;

namespace Danmokou.Behavior.Display {
public class OneOfDisplayController : DisplayController, IMultiDisplayController {
    public DisplayController[] all = null!;
    public int baseSelIndex = 0;
    private int _selIndex;
    public int SelIndex {
        get => _selIndex;
        set {
            _selIndex = value;
            Show();
        }
    }
    public DisplayController recvSprite => all[SelIndex];

    protected override void Awake() {
        foreach (var controller in all)
            controller.IsPartOf(this);
        base.Awake();
    }

    public override void OnLinkOrResetValues(bool isLink) {
        base.OnLinkOrResetValues(isLink);
        for (int ii = 0; ii < all.Length; ++ii) all[ii].OnLinkOrResetValues(isLink);
        SelIndex = baseSelIndex;
    }

    public override void SetMaterial(Material mat) {
        recvSprite.SetMaterial(mat);
    }

    public override void StyleChanged(BehaviorEntity.StyleMetadata style) {
        for (int ii = 0; ii < all.Length; ++ii) all[ii].StyleChanged(style);
    }

    public override void Show() {
        Hide();
        recvSprite.Show();
    }
    public override void Hide() {
        for (int ii = 0; ii < all.Length; ++ii) all[ii].Hide();
    }

    public override void SetSortingOrder(int x) {
        for (int ii = 0; ii < all.Length; ++ii) all[ii].SetSortingOrder(x);
    }

    public override void SetProperty(int id, float val) => recvSprite.SetProperty(id, val);

    public override void OnRender(bool isFirstFrame, Vector2 lastDesiredDelta) {
        base.OnRender(isFirstFrame, lastDesiredDelta);
        for (int ii = 0; ii < all.Length; ++ii) all[ii].OnRender(isFirstFrame, lastDesiredDelta);
    }

    public override void FaceInDirection(Vector2 delta) {
        base.FaceInDirection(delta);
        for (int ii = 0; ii < all.Length; ++ii) all[ii].FaceInDirection(delta);
    }

    public override void SetSprite(Sprite? s) {
        recvSprite.SetSprite(s);
    }

    public override void FadeSpriteOpacity(BPY fader01, float over, ICancellee cT, Action done) =>
        recvSprite.FadeSpriteOpacity(fader01, over, cT, done);

    public override void Animate(AnimationType typ, bool loop, Action? done) =>
        recvSprite.Animate(typ, loop, done);

    public override void Culled(bool allowFinalize, Action done) {
        for (int ii = 0; ii < all.Length; ++ii)
            all[ii].Culled(allowFinalize, ii == SelIndex ? done : WaitingUtils.NoOp);
        base.Culled(allowFinalize, WaitingUtils.NoOp);
    }
}
}