﻿using BagoumLib.Expressions;
using BagoumLib.Mathematics;
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

namespace Danmokou.DMath.Functions {
/// <summary>
/// Functions that return V2RV2.
/// </summary>
[Reflect]
public static partial class ExMRV2 {
    /// <summary>
    /// Derive a V2RV2 from a nonrotational vector and a rotational vector3.
    /// </summary>
    /// <param name="nrot">Nonrotational component</param>
    /// <param name="rot">Rotational component (x,y,angle)</param>
    /// <returns></returns>
    public static trv2 V2V3(tv2 nrot, tv3 rot) => TEx.Resolve(rot, _r => TExHelpers.ResolveV2AsXY(nrot, (nrx, nry) => {
        var r = new TExV3(_r);
        return VRV2(nrx, nry, r.x, r.y, r.z);
    }, singleUse: true));
    
    /// <summary>
    /// Derive a V2RV2 from two vectors and a float.
    /// </summary>
    /// <param name="nrot">Nonrotational x,y</param>
    /// <param name="rot">Rotational x,y</param>
    /// <param name="angle">Rotational angle (degrees)</param>
    /// <returns></returns>
    public static trv2 V2V2F(tv2 nrot, tv2 rot, tfloat angle) => TExHelpers.ResolveV2AsXY(nrot, (nrx, nry) => 
        TExHelpers.ResolveV2AsXY(rot, (rx, ry) => VRV2(nrx, nry, rx, ry, angle), singleUse: true), 
        singleUse: true);
    
    /// <summary>
    /// Derive a V2RV2 from three floats. RX and RY are set to zero.
    /// </summary>
    /// <param name="nx">Nonrotational x</param>
    /// <param name="ny">Nonrotational y</param>
    /// <param name="angle">Rotational angle (degrees)</param>
    /// <returns></returns>
    public static trv2 NRot(tfloat nx, tfloat ny, tfloat angle) => VRV2(nx, ny, E0, E0, angle);
    
    /// <summary>
    /// Derive a V2RV2 from three floats. NX and NY are set to zero.
    /// </summary>
    /// <param name="rx">Rotational x</param>
    /// <param name="ry">Rotational y</param>
    /// <param name="angle">Rotational angle (degrees)</param>
    /// <returns></returns>
    public static trv2 Rot(tfloat rx, tfloat ry, tfloat angle) => VRV2(E0, E0, rx, ry, angle);

    /// <summary>
    /// Derive a V2RV2 from a nonrotational X component only. Everything else is zero.
    /// </summary>
    /// <returns></returns>
    public static trv2 NX(tfloat nx) => VRV2(nx, E0, E0, E0, E0);
    /// <summary>
    /// Derive a V2RV2 from a nonrotational Y component only. Everything else is zero.
    /// </summary>
    /// <returns></returns>
    public static trv2 NY(tfloat ny) => VRV2(E0, ny, E0, E0, E0);
    /// <summary>
    /// Derive a V2RV2 from a rotational X and Y. Everything else is set to zero.
    /// </summary>
    /// <param name="rx">Rotational x</param>
    /// <param name="ry">Rotational y</param>
    /// <returns></returns>
    public static trv2 RXY(tfloat rx, tfloat ry) => VRV2(E0, E0, rx, ry, E0);
    /// <summary>
    /// Derive a V2RV2 from a rotational X component only. Everything else is zero.
    /// </summary>
    /// <returns></returns>
    public static trv2 RX(tfloat rx) => VRV2(E0, E0, rx, E0, E0);
    /// <summary>
    /// Derive a V2RV2 from a rotational Y component only. Everything else is zero.
    /// </summary>
    /// <returns></returns>
    public static trv2 RY(tfloat ry) => VRV2(E0, E0, E0, ry, E0);
    /// <summary>
    /// Derive a V2RV2 from a rotational angle component only. Everything else is zero.
    /// </summary>
    /// <returns></returns>
    [Alias("a")]
    public static trv2 Angle(tfloat a) => VRV2(E0, E0, E0, E0, a);

    /// <summary>
    /// = Angle(360h / RPT)
    /// </summary>
    [Alias("aphi")]
    public static trv2 AnglePhi(tfloat rpt) => Angle(iphi360.Div(rpt));

    
    /// <summary>
    /// Return the zero V2RV2.
    /// </summary>
    /// <returns></returns>
    public static trv2 Zero() => ExC(V2RV2.Zero);
}
}