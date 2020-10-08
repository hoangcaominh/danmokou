﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Danmaku;
using DMath;
using SM;
using UnityEngine;
using UnityEngine.Serialization;

public class FireOption : BehaviorEntity {
    [TextArea(3, 8)]
    public string offsetFree;
    [TextArea(3, 8)]
    public string offsetFocus;
    public string[] powerOffsetFree;
    public string[] powerOffsetFocus;
    public float freeOffsetMul = 1f;
    public float focusOffsetMul = 1f;
    [TextArea(3, 8)]
    public string opacityFree;
    [TextArea(3, 8)]
    public string opacityFocus;
    public string spriteRotation;
    public bool freeOffsetRotational = true;
    private TP3 freeOffset;
    public bool focusOffsetRotational = true;
    public string freeAngleOffset;
    public string focusAngleOffset;
    private TP3 focusOffset;
    private TP3[] freeOffsetPower;
    private TP3[] focusOffsetPower;
    private bool doOpacity;
    private BPY freeOpacity;
    private BPY focusOpacity;
    private BPY rotator;
    private BPY freeAngle = _ => 0;
    private BPY focusAngle = _ => 0;
    private Color rootColor;
    [FormerlySerializedAs("offsetLerpTime")] public float freeFocusLerpTime;
    public float powerLerpTime;
    /// <summary>
    /// =0 when in free offset, =1 when in focus
    /// </summary>
    private float freeFocusLerp;
    private int lastPower = 0;
    private int currPower = 0;
    private float powerLerp = 0.2f;
    public int findex;
    protected override int Findex => findex;

    private static readonly Dictionary<int, FireOption> optionsByIndex = new Dictionary<int, FireOption>();
    private float shootAngle = 0; //up by default
    public PlayerInput Player { get; private set; }
    protected override void Awake() { }

    public void Initialize(PlayerInput playr) {
        Player = playr;
        //I feel kind of bad about this, but it ensures that PlayerInput is linked before the SM runs.
        base.Awake();
        original_angle = shootAngle = 0; //Shoot up by default
        freeOffset = ReflWrap<TP3>.Wrap(offsetFree);
        if (string.IsNullOrWhiteSpace(offsetFocus)) offsetFocus = offsetFree;
        focusOffset = ReflWrap<TP3>.Wrap(offsetFocus);
        if (true == (doOpacity = !string.IsNullOrWhiteSpace(opacityFree))) {
            freeOpacity = ReflWrap<BPY>.Wrap(opacityFree);
            if (string.IsNullOrWhiteSpace(opacityFocus)) opacityFocus = opacityFree;
            focusOpacity = ReflWrap<BPY>.Wrap(opacityFocus);
            rootColor = sr.color;
        }
        freeOffsetPower = powerOffsetFree.Select(ReflWrap<TP3>.Wrap).ToArray();
        focusOffsetPower = powerOffsetFocus.Select(ReflWrap<TP3>.Wrap).ToArray();
        if (!string.IsNullOrWhiteSpace(spriteRotation)) rotator = ReflWrap<BPY>.Wrap(spriteRotation);
        if (!string.IsNullOrWhiteSpace(freeAngleOffset)) freeAngle = ReflWrap<BPY>.Wrap(freeAngleOffset);
        if (!string.IsNullOrWhiteSpace(focusAngleOffset)) focusAngle = ReflWrap<BPY>.Wrap(focusAngleOffset);
        optionsByIndex[findex] = this;
        lastPower = currPower = Math.Min(freeOffsetPower.Length - 1, GameManagement.campaign.PowerIndex);
        SetLocation();
    }

    public void Preload() {
        ReflWrap<TP3>.Load(offsetFree);
        ReflWrap<TP3>.Load(offsetFocus);
        ReflWrap<BPY>.Load(opacityFree);
        ReflWrap<BPY>.Load(opacityFocus);
        powerOffsetFree.ForEach(ReflWrap<TP3>.Load);
        powerOffsetFocus.ForEach(ReflWrap<TP3>.Load);
        ReflWrap<BPY>.Load(spriteRotation);
        ReflWrap<BPY>.Load(freeAngleOffset);
        ReflWrap<BPY>.Load(focusAngleOffset);
        StateMachineManager.FromText(behaviorScript);
    }

    private float FreeOffsetRotation => freeOffsetRotational ? shootAngle : 0f;
    private float FocusOffsetRotation => focusOffsetRotational ? shootAngle : 0f;

    private Vector3 SelectByPower(TP3[] powers, TP3 otherwise) {
        if (powers.Length == 0) return otherwise(bpi);
        int index = Math.Min(powers.Length - 1, GameManagement.campaign.PowerIndex);
        if (index != currPower) {
            lastPower = currPower;
            currPower = index;
            powerLerp = 0;
        }
        return Vector3.Lerp(powers[lastPower](bpi), powers[currPower](bpi), powerLerp);
    }

    private Vector3 FreeOffset => SelectByPower(freeOffsetPower, freeOffset) * freeOffsetMul;
    private Vector3 FocusOffset => SelectByPower(focusOffsetPower, focusOffset) * focusOffsetMul;
    
    private void SetLocation() {
        powerLerp = Mathf.Clamp01(powerLerp + ETime.FRAME_TIME / powerLerpTime);
        var lerpDir = Player.IsFocus ? 1 : -1;
        freeFocusLerp = Mathf.Clamp01(freeFocusLerp + lerpDir * ETime.FRAME_TIME / freeFocusLerpTime);
        //Shot files are oriented upwards by default
        if (InputManager.FiringAngle.HasValue) shootAngle = InputManager.FiringAngle.Value - 90;
        original_angle = shootAngle + Mathf.Lerp(freeAngle(bpi), focusAngle(bpi), freeFocusLerp);
        tr.localPosition = M.RotateXYDeg(FreeOffset, FreeOffsetRotation) * (1 - freeFocusLerp) + 
                           M.RotateXYDeg(FocusOffset, FocusOffsetRotation) * freeFocusLerp;
        if (doOpacity) {
            var color = rootColor;
            color.a *= freeOpacity(bpi) * (1 - freeFocusLerp) + focusOpacity(bpi) * freeFocusLerp;
            sr.color = color;
        }
        if (rotator != null) tr.localEulerAngles = new Vector3(0, 0, rotator(bpi));
    }
    protected override void RegularUpdateMove() {
        base.RegularUpdateMove();
        SetLocation();
    }
    public override int UpdatePriority => UpdatePriorities.PLAYER2;

    protected override void OnDisable() {
        optionsByIndex.Remove(findex);
        base.OnDisable();
    }

    public static float Power() => (float)GameManagement.campaign.Power;
    public static float PowerIndex() => (float)GameManagement.campaign.PowerIndex;
    public static Vector2 OptionLocation(int index) =>
        optionsByIndex.TryGetValue(index, out var v) ? (Vector2)v.tr.position : Vector2.zero;
    
    public static float OptionAngle(int index) =>
        optionsByIndex.TryGetValue(index, out var v) ? v.original_angle : 0f;

    public static readonly ExFunction optionLocation = ExUtils.Wrap<FireOption>("OptionLocation", typeof(int));
    public static readonly ExFunction optionAngle = ExUtils.Wrap<FireOption>("OptionAngle", typeof(int));
    public static readonly ExFunction power = ExUtils.Wrap<FireOption>("Power");
    public static readonly ExFunction powerIndex = ExUtils.Wrap<FireOption>("PowerIndex");
}