﻿using System;
using System.Diagnostics.CodeAnalysis;
using Danmokou.Core;
using Danmokou.Expressions;
using Scriptor;
using Scriptor.Expressions;
using Ex = System.Linq.Expressions.Expression;
using static Danmokou.Expressions.ExUtils;
using ExBPY = System.Func<Scriptor.Expressions.TExArgCtx, Scriptor.Expressions.TEx<float>>;

namespace Danmokou.DMath.Functions {

/// <summary>
/// Number>number functions. 
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[Reflect]
public static class FXYRepo {

    /// <summary>
    /// Apply a ease function on top of a target function that uses time as a controller.
    /// </summary>
    /// <param name="smoother">Smoothing function (<see cref="ExMEasers"/>)</param>
    /// <param name="maxTime">Time over which to perform easing</param>
    /// <param name="f">Target function</param>
    /// <returns></returns>
    public static ExBPY EaseF([LookupMethod] Func<TExArgCtx, TEx<Func<float, float>>> smoother, float maxTime, ExBPY f) 
    => DMKExMHelpers.Ease(smoother, maxTime, f, x => x.FloatVal, (x, y) => x.MakeCopyForType<float>(y));

    
    /// <summary>
    /// Apply a ease function on top of a target derivative function that uses time as a controller.
    /// </summary>
    /// <param name="smoother">Name of a float->float smoothing function (<see cref="ExMEasers"/>)</param>
    /// <param name="maxTime">Time over which to perform easing</param>
    /// <param name="fd">Target function</param>
    /// <returns></returns>
    public static ExBPY EaseFD(string smoother, float maxTime, ExBPY fd) 
        => DMKExMHelpers.EaseD(smoother, maxTime, fd, x => x.FloatVal, (x, y) => x.MakeCopyForType<float>(y));

    /// <summary>
    /// See <see cref="BPYRepo.SoftmaxShift"/>.
    /// </summary>
    public static ExBPY SoftmaxShift(ExBPY sharpness, ExBPY pivot, ExBPY f1, ExBPY f2) =>
        DMKExMHelpers.SoftmaxShift<TEx<float>>(sharpness, pivot, f1, f2, "x");

}

}
