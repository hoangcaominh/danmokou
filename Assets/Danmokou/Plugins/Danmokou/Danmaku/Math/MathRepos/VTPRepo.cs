﻿using System;
using System.Linq;
using System.Linq.Expressions;
using BagoumLib.Expressions;
using Danmokou.Core;
using Danmokou.Expressions;
using Scriptor;
using Scriptor.Analysis;
using Scriptor.Expressions;
using Scriptor.Math;
using UnityEngine;
using Ex = System.Linq.Expressions.Expression;
using static Danmokou.DMath.Functions.ExM;
using static Danmokou.DMath.Functions.VTPConstructors;
using static Danmokou.DMath.Functions.ExMConversions;
using static Danmokou.Reflection.Aliases;
using static Scriptor.Expressions.ExMHelpers;
using ExVTP = System.Func<Scriptor.Expressions.TExArgCtx, Scriptor.Expressions.TEx<Danmokou.Expressions.VTPExpr>>;
using ExBPY = System.Func<Scriptor.Expressions.TExArgCtx, Scriptor.Expressions.TEx<float>>;
using ExPred = System.Func<Scriptor.Expressions.TExArgCtx, Scriptor.Expressions.TEx<bool>>;
using ExTP = System.Func<Scriptor.Expressions.TExArgCtx, Scriptor.Expressions.TEx<UnityEngine.Vector2>>;
using ExTP3 = System.Func<Scriptor.Expressions.TExArgCtx, Scriptor.Expressions.TEx<UnityEngine.Vector3>>;


namespace Danmokou.DMath.Functions {
//We don't compile V2ToCoords. This is why its structure is different from CoordF. Instead of
// having out Vector3, it takes a delegate that operates over the Vector3 that it would otherwise return.
public delegate TEx<VTPExpr> V2ToCoords(TEx<float> cosRot, TEx<float> sinRot, TExArgCtx tac,
    Func<TEx<float>, TEx<float>, TEx<float>, TEx> continuation);

public delegate TEx<VTPExpr> CoordsToDelta(ITexMovement vel, TEx<float> dt, TExV3 delta,
    TEx<float> x, TEx<float> y, TEx<float> z);
/// <summary>
/// Repository for constructing path expressions by converting lesser computations into Cartesian coordinates
/// and applying appropriate rotation.
/// <br/>These functions should not be invoked by users; instead, use the functions in <see cref="VTPRepo" />.
/// </summary>
public static class VTPConstructors {
    public static V2ToCoords CartesianRot(ExTP erv) => (c, s, bpi, fxy) => 
        TExHelpers.ResolveV2AsXY(erv(bpi), (x, y) => 
            fxy(Ex.Subtract(Ex.Multiply(c, x), Ex.Multiply(s, y)),
                Ex.Add(Ex.Multiply(s, x), Ex.Multiply(c, y)),
                E0));

    public static CoordF CartesianRot(TP rv) => delegate(float c, float s, ParametricInfo bpi, ref Vector3 vec) {
        var v2 = rv(bpi);
        vec.x = c * v2.x - s * v2.y;
        vec.y = s * v2.x + c * v2.y;
        vec.z = 0;
    };

    public static V2ToCoords CartesianNRot(ExTP enrv) => (c, s, bpi, fxy) => 
        TExHelpers.ResolveV2AsXY(enrv(bpi), (x, y) => fxy(x, y, E0), singleUse: true);

    // ReSharper disable once RedundantAssignment
    public static CoordF CartesianNRot(TP tpnrv) => delegate(float c, float s, ParametricInfo bpi, ref Vector3 vec) {
        vec = tpnrv(bpi);
    };

    public static V2ToCoords Cartesian(ExTP erv, ExTP enrv) {
        var nrv = new TExV2();
        var v2 = new TExV2();
        return (c, s, bpi, fxy) => Ex.Block(
            new ParameterExpression[] { nrv, v2 },
            Ex.Assign(nrv, enrv(bpi)),
            Ex.Assign(v2, erv(bpi)),
            fxy(nrv.x.Add(Ex.Subtract(Ex.Multiply(c, v2.x), Ex.Multiply(s, v2.y))),
                nrv.y.Add(Ex.Add(Ex.Multiply(s, v2.x), Ex.Multiply(c, v2.y))),
                E0)
        );
    }

    public static V2ToCoords Cartesian3D(ExTP3 erv, ExTP3 enrv) {
        var nrv = new TExV3();
        var rv = new TExV3();
        return (c, s, bpi, fxy) => Ex.Block(
            new ParameterExpression[] { nrv, rv },
            Ex.Assign(nrv, enrv(bpi)),
            Ex.Assign(rv, erv(bpi)),
            fxy(nrv.x.Add(Ex.Subtract(Ex.Multiply(c, rv.x), Ex.Multiply(s, rv.y))),
                nrv.y.Add(Ex.Add(Ex.Multiply(s, rv.x), Ex.Multiply(c, rv.y))),
                nrv.z.Add(rv.z))
        );
    }

    public static CoordF Cartesian(TP rv, TP nrv) => delegate(float c, float s, ParametricInfo bpi, ref Vector3 vec) {
        var v2n = nrv(bpi);
        var v2 = rv(bpi);
        vec.x = v2n.x + c * v2.x - s * v2.y;
        vec.y = v2n.y + s * v2.x + c * v2.y;
    };

    public static V2ToCoords Polar(ExBPY r, ExBPY theta) {
        var vr = ExUtils.VFloat();
        var lookup = new TExV2();
        return (c, s, bpi, fxy) => Ex.Block(new[] { vr, lookup },
            Ex.Assign(lookup, ExM.CosSinDeg(theta(bpi))),
            Ex.Assign(vr, r(bpi)),
            fxy(Ex.Subtract(Ex.Multiply(c, lookup.x), Ex.Multiply(s, lookup.y)).Mul(vr),
                Ex.Add(Ex.Multiply(s, lookup.x), Ex.Multiply(c, lookup.y)).Mul(vr),
                E0)
        );
    }

    public static V2ToCoords Polar2(ExTP radThetaDeg) {
        var rt = new TExV2();
        var lookup = new TExV2();
        return (c, s, bpi, fxy) => Ex.Block(new ParameterExpression[] { rt, lookup },
            Ex.Assign(rt, radThetaDeg(bpi)),
            Ex.Assign(lookup, ExM.CosSinDeg(rt.y)),
            fxy(Ex.Subtract(Ex.Multiply(c, lookup.x), Ex.Multiply(s, lookup.y)).Mul(rt.x),
                Ex.Add(Ex.Multiply(s, lookup.x), Ex.Multiply(c, lookup.y)).Mul(rt.x),
                E0)
        );
    }

    public static CoordF Polar(BPY r, BPY theta) => delegate(float c, float s, ParametricInfo bpi, ref Vector3 vec) {
        var cs = M.CosSinDeg(theta(bpi));
        var rad = r(bpi);
        vec.x = rad * (c * cs.x - s * cs.y);
        vec.y = rad * (s * cs.x + c * cs.y);
    };
}

/// <summary>
/// Repository for constructing path expressions by converting coordinates into movement instructions.
/// <br/>These functions should not be invoked by users; instead, use the functions in <see cref="VTPRepo" />.
/// </summary>
public static class VTPControllers {
    private static TEx<VTPExpr> InLetCtx(TExArgCtx tac, V2ToCoords coords, CoordsToDelta next) {
        var vel =  tac.MaybeGetByExprType<TExMov>(out _) ?? (tac.GetByExprType<TExLMov>() as ITexMovement);
        var dt = tac.GetByName<float>("vtp_dt");
        var delta = tac.GetByExprType<TExV3>();
        using var root = tac.Let(MOV_ROOT_ALIAS, vel.root);
        using var ang = tac.Let(MOV_ANGLE_ALIAS, vel.angle);
        using var angc = tac.Let(MOV_COS_ALIAS, vel.cos);
        using var angs = tac.Let(MOV_SIN_ALIAS, vel.sin);
        return coords(vel.cos, vel.sin, tac, (x, y, z) => next(vel, dt, delta, x, y, z));
    }

    private static void FindFlipVars(TExArgCtx tac, out Ex? flipX, out Ex? flipY) {
        flipX = null;
        flipY = null;
        if (!tac.Ctx.Scope.TryPeek(out var ls)) return;
        if (ls.FindVariable("flipX") is { } fxDecl)
            flipX = ls.LocalOrParentVariable(tac, tac.EnvFrame, fxDecl);
        if (ls.FindVariable("flipY") is { } fyDecl)
            flipY = ls.LocalOrParentVariable(tac, tac.EnvFrame, fyDecl);
    }
    
    private static Ex MulN(Ex? a, Ex b) => a is null ? b : a.Mul(b);

    public static ExVTP Velocity(V2ToCoords cf) => tac => InLetCtx(tac, cf, (vel, dt, delta, x, y, z) => 
        //TODO flipX: remove vel.flipX and move it to the ef reference
            Ex.Block(
                delta.x.Is(vel.flipX.Mul(x).Mul(dt)),
                delta.y.Is(vel.flipY.Mul(y).Mul(dt))
            ));

    public static ExVTP Velocity3D(V2ToCoords cf) => tac => InLetCtx(tac, cf, (vel, dt, delta, x, y, z) =>
            Ex.Block(
                delta.x.Is(vel.flipX.Mul(x).Mul(dt)),
                delta.y.Is(vel.flipY.Mul(y).Mul(dt)),
                delta.z.Is(z.Mul(dt))
            ));

    public static VTP Velocity(CoordF coordF) =>
        delegate(ref Movement vel, in float dT, ref ParametricInfo bpi, ref Vector3 delta) {
            coordF(vel.cos_rot, vel.sin_rot, bpi, ref delta);
            delta.x *= vel.flipX * dT;
            delta.y *= vel.flipY * dT;
        };

    public static ExVTP Offset(V2ToCoords cf) => tac => InLetCtx(tac, cf, (vel, dt, delta, x, y, z) =>
            Ex.Block(
                delta.x.Is(vel.flipX.Mul(x).Add(vel.rootX).Sub(tac.locx())),
                delta.y.Is(vel.flipY.Mul(y).Add(vel.rootY).Sub(tac.locy()))
            ));

    public static ExVTP Offset3D(V2ToCoords cf) => tac => InLetCtx(tac, cf, (vel, dt, delta, x, y, z) =>
            Ex.Block(
                delta.x.Is(vel.flipX.Mul(x).Add(vel.rootX).Sub(tac.locx())),
                delta.y.Is(vel.flipY.Mul(y).Add(vel.rootY).Sub(tac.locy())),
                delta.z.Is(z.Sub(tac.locz()))
            ));

    public static VTP Offset(CoordF coordF) =>
        delegate(ref Movement vel, in float dT, ref ParametricInfo bpi, ref Vector3 delta) {
            coordF(vel.cos_rot, vel.sin_rot, bpi, ref delta);
            delta.x = delta.x * vel.flipX + vel.rootPos.x - bpi.loc.x;
            delta.y = delta.y * vel.flipY + vel.rootPos.y - bpi.loc.y;
        };
}

/// <summary>
/// Repository for movement functions.
/// <br/>All functions are in two dimensions unless they have the "3D" suffix.
/// </summary>
[Reflect]
public static class VTPRepo {
    [DontReflect]
    public static bool IsNone(this VTP func) => ReferenceEquals(func, NoVTP);

    public static readonly ExVTP ExNoVTP = VTPControllers.Velocity(CartesianNRot(Parametrics.Zero()));
    public static readonly VTP NoVTP =
        delegate(ref Movement vel, in float dT, ref ParametricInfo bpi, ref Vector3 nrv) { nrv.x = nrv.y = nrv.z = 0; };

    /// <summary>
    /// No movement.
    /// </summary>
    [BDSL1Only]
    public static ExVTP Null() => ExNoVTP;
    
    /// <summary>
    /// No movement.
    /// </summary>
    public static VTP None() => NoVTP;

    /// <summary>
    /// Switch between path functions based on a condition.
    /// <br/>You can use this to smoothly switch from offset to velocity equations,
    /// but switching from velocity to offset will give you strange results. 
    /// </summary>
    [Alias("switch")]
    public static ExVTP If(ExPred cond, ExVTP ifTrue, ExVTP ifFalse) => tac =>
        Ex.Condition(cond(tac), ifTrue(tac), ifFalse(tac));

    /// <summary>
    /// Movement with Cartesian rotational velocity only.
    /// </summary>
    /// <param name="rv">Rotational velocity parametric</param>
    /// <returns></returns>
    [Alias("tprot")] [Alias("rvel")]
    public static ExVTP RVelocity(ExTP rv) => VTPControllers.Velocity(CartesianRot(rv));

    /// <summary>
    /// Movement with Cartesian nonrotational velocity only.
    /// </summary>
    /// <param name="nrv">Nonrotational velocity parametric</param>
    /// <returns></returns>
    [Alias("tpnrot")] [Alias("nrvel")]
    public static ExVTP NRVelocity(ExTP nrv) => VTPControllers.Velocity(CartesianNRot(nrv));

    /// <summary>
    /// Movement with Cartesian rotational velocity and nonrotational velocity.
    /// </summary>
    /// <param name="rv">Rotational velocity parametric</param>
    /// <param name="nrv">Nonrotational velocity parametric</param>
    /// <returns></returns>
    [Alias("tp")]
    public static ExVTP Velocity(ExTP rv, ExTP nrv) => VTPControllers.Velocity(Cartesian(rv, nrv));

    /// <summary>
    /// Movement with Cartesian rotational velocity and nonrotational velocity
    ///  in three dimensions.
    /// </summary>
    /// <param name="rv">Rotational velocity parametric</param>
    /// <param name="nrv">Nonrotational velocity parametric</param>
    /// <returns></returns>
    public static ExVTP Velocity3D(ExTP3 rv, ExTP3 nrv) => VTPControllers.Velocity3D(Cartesian3D(rv, nrv));

    /// <summary>
    /// Movement with Cartesian rotational offset only.
    /// </summary>
    /// <param name="rp">Rotational offset parametric</param>
    /// <returns></returns>
    public static ExVTP ROffset(ExTP rp) => VTPControllers.Offset(CartesianRot(rp));

    /// <summary>
    /// Movement with Cartesian nonrotational offset only.
    /// </summary>
    /// <param name="nrp">Nonrotational offset parametric</param>
    /// <returns></returns>
    public static ExVTP NROffset(ExTP nrp) => VTPControllers.Offset(CartesianNRot(nrp));

    /// <summary>
    /// Movement with Cartesian rotational offset and nonrotational offset.
    /// </summary>
    /// <param name="rp">Rotational offset parametric</param>
    /// <param name="nrp">Nonrotational offset parametric</param>
    /// <returns></returns>
    public static ExVTP Offset(ExTP rp, ExTP nrp) => VTPControllers.Offset(Cartesian(rp, nrp));


    /// <summary>
    /// Movement with Cartesian rotational offset and nonrotational offset
    ///  in three dimensions.
    /// </summary>
    /// <param name="rp">Rotational offset parametric</param>
    /// <param name="nrp">Nonrotational offset parametric</param>
    /// <returns></returns>
    public static ExVTP Offset3D(ExTP3 rp, ExTP3 nrp) => VTPControllers.Offset3D(Cartesian3D(rp, nrp));

    /// <summary>
    /// Offset function for dependent (empty-guided) fires.
    /// Reduces to `offset (RADIUS * (@ HOISTDIR p)) (@ HOISTLOC p)`
    /// </summary>
    /// <param name="hoistLoc">Location of empty guider</param>
    /// <param name="hoistDir">Direction of empty guider</param>
    /// <param name="indexer">Indexer function for public hoisting</param>
    /// <param name="radius">Radial offset of guided</param>
    /// <returns></returns>
    public static ExVTP DOffset(ReflectEx.Hoist<Vector2> hoistLoc, ReflectEx.Hoist<Vector2> hoistDir,
        ExBPY indexer, ExBPY radius) => Offset(
        bpi => ExMOperators.Mul(radius(bpi), RetrieveHoisted(hoistDir, indexer)(bpi)),
        RetrieveHoisted(hoistLoc, indexer)
    );

    /// <summary>
    /// Offset function for dependent (empty-guided) fires.
    /// Reduces to `offset (rotatev (@ HOISTDIR p) OFFSET) (@ HOISTLOC p)`
    /// </summary>
    /// <param name="hoistLoc">Location of empty guider</param>
    /// <param name="hoistDir">Direction of empty guider</param>
    /// <param name="indexer">Indexer function for public hoisting</param>
    /// <param name="offset">Parametric offset of guided</param>
    /// <returns></returns>
    public static ExVTP DTPOffset(ReflectEx.Hoist<Vector2> hoistLoc, ReflectEx.Hoist<Vector2> hoistDir,
        ExBPY indexer, ExTP offset) => Offset(
        bpi => RotateV(RetrieveHoisted(hoistDir, indexer)(bpi), offset(bpi)),
        RetrieveHoisted(hoistLoc, indexer)
    );

    /// <summary>
    /// Movement with polar rotational offset.
    /// </summary>
    /// <param name="radius">Radius function</param>
    /// <param name="theta">Theta function (degrees)</param>
    public static ExVTP Polar(ExBPY radius, ExBPY theta) => VTPControllers.Offset(VTPConstructors.Polar(radius, theta));

    /// <summary>
    /// Movement with polar rotational offset. Uses a vector2 instead of two floats. (This is slower.)
    /// </summary>
    /// <param name="rt">Radius function (X), Theta function (Y) (degrees)</param>
    /// <returns></returns>
    public static ExVTP Polar2(ExTP rt) => VTPControllers.Offset(VTPConstructors.Polar2(rt));

    /// <summary>
    /// Movement with polar rotational velocity.
    /// <br/>Note: I'm pretty sure this doesn't work at all.
    /// </summary>
    /// <param name="radius">Radius derivative function</param>
    /// <param name="theta">Theta derivative function (degrees)</param>
    /// <returns></returns>
    public static ExVTP VPolar(ExBPY radius, ExBPY theta) =>
        VTPControllers.Velocity(VTPConstructors.Polar(radius, theta));
    
    private static ExVTP WrapLet<T>((string, Func<TExArgCtx, TEx<T>>)[] aliases, ExVTP inner) =>
        tac => ReflectEx.Let(aliases, () => inner(tac), tac);

    /// <summary>
    /// Bind float values to the aliases and then execute the inner content with those aliases.
    /// </summary>
    [Alias("::")]
    public static ExVTP LetFloats((string, ExBPY)[] aliases, ExVTP inner) => WrapLet(aliases, inner);

    /// <summary>
    /// Bind vector2 values to the aliases and then execute the inner content with those aliases.
    /// </summary>
    [Alias("::v2")]
    public static ExVTP LetV2s((string, ExTP)[] aliases, ExVTP inner) => WrapLet(aliases, inner);

    /// <summary>
    /// Bind values to the aliases and then execute the inner content with those aliases.
    /// </summary>
    public static ExVTP Let(ReflectEx.Alias[] aliases, ExVTP inner) => tac =>
        ReflectEx.LetAlias(aliases, () => inner(tac), tac);
}

public static class CSVTPRepo {
    /// <summary>
    /// (C# code use) Movement with Cartesian rotational velocity only.
    /// </summary>
    /// <param name="rv">Rotational velocity parametric</param>
    /// <returns></returns>
    public static VTP RVelocity(TP rv) => VTPControllers.Velocity(CartesianRot(rv));

    /// <summary>
    /// (C# code use) Movement with Cartesian nonrotational velocity only.
    /// </summary>
    /// <param name="nrv">Nonrotational velocity parametric</param>
    /// <returns></returns>
    public static VTP NRVelocity(TP nrv) => VTPControllers.Velocity(CartesianNRot(nrv));

    /// <summary>
    /// (C# code use) Movement with Cartesian rotational velocity and nonrotational velocity.
    /// </summary>
    /// <param name="rv">Rotational velocity parametric</param>
    /// <param name="nrv">Nonrotational velocity parametric</param>
    /// <returns></returns>
    public static VTP Velocity(TP rv, TP nrv) => VTPControllers.Velocity(Cartesian(rv, nrv));

    /// <summary>
    /// (C# code use) Movement with Cartesian rotational offset only.
    /// </summary>
    /// <param name="rp">Rotational offset parametric</param>
    /// <returns></returns>
    public static VTP ROffset(TP rp) => VTPControllers.Offset(CartesianRot(rp));

    /// <summary>
    /// (C# code use) Movement with Cartesian nonrotational offset only.
    /// </summary>
    /// <param name="nrp">Nonrotational offset parametric</param>
    /// <returns></returns>
    public static VTP NROffset(TP nrp) => VTPControllers.Offset(CartesianNRot(nrp));

    /// <summary>
    /// (C# code use) Movement with Cartesian rotational offset and nonrotational offset.
    /// </summary>
    /// <param name="rp">Rotational offset parametric</param>
    /// <param name="nrp">Nonrotational offset parametric</param>
    /// <returns></returns>
    public static VTP Offset(TP rp, TP nrp) => VTPControllers.Offset(Cartesian(rp, nrp));
    
    /// <summary>
    /// (C# code use) Movement with polar rotational offset.
    /// </summary>
    /// <param name="r">Radius function</param>
    /// <param name="theta">Theta function (degrees)</param>
    public static VTP Polar(BPY r, BPY theta) => VTPControllers.Offset(VTPConstructors.Polar(r, theta));
}
}