﻿using System;
using System.Linq.Expressions;
using BagoumLib.Expressions;
using Danmokou.Core;
using Danmokou.Expressions;
using Scriptor;
using Scriptor.Expressions;
using Ex = System.Linq.Expressions.Expression;
using static Danmokou.Expressions.ExUtils;
using static Scriptor.Expressions.ExMHelpers;
using tfloat = Scriptor.Expressions.TEx<float>;
using tbool = Scriptor.Expressions.TEx<bool>;
using tv2 = Scriptor.Expressions.TEx<UnityEngine.Vector2>;
using tv3 = Scriptor.Expressions.TEx<UnityEngine.Vector3>;
using trv2 = Scriptor.Expressions.TEx<BagoumLib.Mathematics.V2RV2>;
using static Danmokou.DMath.Functions.ExM;

namespace Danmokou.DMath.Functions {
/// <summary>
/// See <see cref="ExM"/>. This class contains functions related to coordinate conversion.
/// </summary>
[Reflect]
public static class ExMConversions {

    public static tv2 Polar2ToXY(tv2 rt) => TExHelpers.ResolveV2AsXY(rt, (x, y) => PolarToXY(x, y), singleUse: true);
    /// <summary>
    /// Convert polar coordinates (theta in degrees) to Cartesian coordinates.
    /// </summary>
    [Alias("rx")]
    public static tv2 PolarToXY(tfloat r, tfloat theta) => r.Mul(CosSinDeg(theta));
    /// <summary>
    /// Convert polar coordinates (theta in radians) to Cartesian coordinates.
    /// </summary>
    public static tv2 PolarToXYRad(tfloat r, tfloat theta) => r.Mul(CosSin(theta));

    /// <summary>
    /// Convert Cartesian coordinates to polar coordinates (theta in degrees).
    /// </summary>
    public static tv2 XYToPolar(tv2 v2) => TExHelpers.ResolveV2AsXY(v2, (x, y) => V2(Mag2(x, y), ATan2(y, x)));
    
    /// <summary>
    /// Convert Cartesian coordinates to polar coordinates (theta in radians).
    /// </summary>
    public static tv2 XYToPolarRad(tv2 v2) => TExHelpers.ResolveV2AsXY(v2, (x, y) => V2(Mag2(x, y), ATanR2(y, x)));

    /// <summary>
    /// Converts an RV2 to Cartesian coordinates via TrueLocation.
    /// </summary>
    public static tv2 RV2ToXY(trv2 v2rv2) => TEx.Resolve(v2rv2, rv2 => {
        var tex = new TExRV2(rv2);
        return Rotate2(tex.angle, tex.rx, tex.ry).Add(V2(tex.nx, tex.ny));
    });
    /// <summary>
    /// Converts an RV2 to polar coordinates (theta in degrees) via TrueLocation and XYToPolar.
    /// </summary>
    public static tv2 RV2ToPolar(trv2 v2rv2) => XYToPolar(RV2ToXY(v2rv2));
    
    /// <summary>
    /// Rotate a V2 by some degrees counterclockwise.
    /// </summary>
    public static tv2 Rotate(tfloat ang_deg, tv2 v2) => RotateV(CosSinDeg(ang_deg), v2);

    /// <summary>
    /// Rotate a V2 by some radians counterclockwise.
    /// </summary>
    public static tv2 RotateRad(tfloat ang_rad, tv2 v2) => RotateV(CosSin(ang_rad), v2);

    /// <summary>
    /// Rotate an (x,y) pair by some degrees counterclockwise.
    /// </summary>
    [Alias("rxy")]
    public static tv2 Rotate2(tfloat ang_deg, tfloat xv, tfloat yv) => RotateV2(CosSinDeg(ang_deg), xv, yv);
    /// <summary>
    /// Rotate an (x,y) pair by some radians counterclockwise.
    /// </summary>
    public static tv2 RotateRad2(tfloat ang_rad, tfloat xv, tfloat yv) => RotateV2(CosSin(ang_rad), xv, yv);

    /// <summary>
    /// Rotate a V2 by a vector containing cosine and sine values counterclockwise.
    /// </summary>
    public static tv2 RotateV(tv2 cossin, tv2 v2) => TExHelpers.ResolveV2AsXY(cossin, (c, s) =>
        TExHelpers.ResolveV2AsXY(v2, (x, y) => RotateCS2(c, s, x, y), singleUse: true), singleUse: true);
    
    /// <summary>
    /// Rotate a V2 by a vector containing cosine and sine values counterclockwise.
    /// </summary>
    public static tv2 RotateV2(tv2 cossin, tfloat xv, tfloat yv) => 
        TExHelpers.ResolveV2AsXY(cossin, (x, y) => RotateCS2(x, y, xv, yv), singleUse: true);
    /// <summary>
    /// Rotate a V2 by a calculated cosine and sine value counterclockwise.
    /// </summary>
    public static tv2 RotateCS(tfloat cos, tfloat sin, tv2 v2) => 
        TExHelpers.ResolveV2AsXY(v2, (x, y) => RotateCS2(cos, sin, x, y), singleUse: true);
    
    /// <summary>
    /// Rotate an (x,y) pair by a calculated cosine and sine value counterclockwise.
    /// </summary>
    public static tv2 RotateCS2(tfloat cos, tfloat sin, tfloat xv, tfloat yv) => 
        TEx.Resolve(cos, sin, xv, yv, (c, s, x, y) => V2(c.Mul(x).Sub(s.Mul(y)), s.Mul(x).Add(c.Mul(y))));

    //Note that basis conversion is the same as inverse rotation,
    //and basis deconversion is the same as rotation.
    public static tv2 ConvertBasis(tv2 source, tv2 basis1) => TExHelpers.ResolveV2(source, basis1,
        // [ b1.x  -b1.y ]^T  [ x ]
        // [ b1.y   b1.x ]    [ y ]
        (s, b1) => ExUtils.V2(
            b1.x.Mul(s.x).Add(b1.y.Mul(s.y)),
            b1.x.Mul(s.y).Sub(b1.y.Mul(s.x))
        ));

    public static tv2 DeconvertBasis(tv2 source, tv2 basis1) => RotateV(basis1, source);

    /// <summary>
    /// Get the unit spherical coordinates for a vector3.
    /// </summary>
    public static tv2 ToSphere(tv3 source) => TExHelpers.ResolveV3(source, v => Ex.Block(V2(
        ATan2(v.y, v.x),
        ACos(v.z.Div(v3Mag(v)))
    )));

    public static tv3 FromSphere(tfloat radius, tv2 sphere) => radius.Mul(TExHelpers.ResolveV2AsXY(sphere, (sx, sy) => {
        var cst = new TExV2();
        var csp = new TExV2();
        return Ex.Block(new ParameterExpression[] {cst, csp},
            cst.Is(CosSinDeg(sx)),
            csp.Is(CosSinDeg(sy)),
            V3(cst.x.Mul(csp.y), cst.y.Mul(csp.y), csp.x)
        );
    }, singleUse: true));

    public static tv3 CrossProduct(tv3 v1, tv3 v2) => TExHelpers.ResolveV3(v1, v2, (a, b) => V3(
        a.y.Mul(b.z).Sub(a.z.Mul(b.y)), 
        a.z.Mul(b.x).Sub(a.x.Mul(b.z)), 
        a.x.Mul(b.y).Sub(a.y.Mul(b.x))));

    /// <summary>
    /// Note: requires that normalVec and planeVec are perpendicular.
    /// </summary>
    public static tv3 RotateInPlane(tfloat rot, tv3 normalVec, tv3 planeVec) => TEx.Resolve(planeVec, p => {
            var cs = new TExV2();
            var cross = new TExV3();
            return Ex.Block(new ParameterExpression[] {cs, cross},
                cs.Is(CosSinDeg(rot)),
                cross.Is(CrossProduct(normalVec, p)),
                cs.x.Mul(p).Add(cs.y.Mul(v3Mag(p).Div(v3Mag(cross)).Mul(cross)))
            );
        });
    
}
}