﻿using System;
using System.Collections.Generic;
using System.Threading;
using DMath;
using JetBrains.Annotations;
using SM;
using UnityEngine;
using static Danmaku.BehOption;
using static Danmaku.Enums;

namespace Danmaku {

/// <summary>
/// Properties that modify the behavior of BEH summons.
/// This includes complex bullets, like pathers, but NOT lasers (<see cref="LaserOption"/>).
/// </summary>
public class BehOption {
    /// <summary>
    /// Make the movement of the bullet smoother. (Pather only)
    /// </summary>
    public static BehOption Smooth() => new SmoothProp();

    /// <summary>
    /// Run a StateMachine on the bullet.
    /// <br/>This SM is run "superfluously": once it is finished, the object will continue to exist.
    /// </summary>
    public static BehOption SM(StateMachine sm) => new SMProp(sm);
    
    /// <summary>
    /// Set the scale of the object. Support depends on object.
    /// <br/>For pathers, sets the y scale.
    /// </summary>
    public static BehOption S(GCXF<float> scale) => new ScaleProp(scale);

    /// <summary>
    /// Set the starting HP of an enemy summon.
    /// <br/>This will throw an error if used on a non-enemy.
    /// </summary>
    public static BehOption HP(GCXF<float> hp) => new HPProp(hp);

    public static BehOption Drops3(int value, int ppp, int life) => Drops4(value, ppp, life, 0);
    public static BehOption Drops4(int value, int ppp, int life, int power) => new ItemsProp(new ItemDrops(value, ppp, life, power, 0));
    
    public static BehOption Low() => new LayerProp(Layer.LowProjectile);
    public static BehOption High() => new LayerProp(Layer.HighProjectile);
    
    /// <summary>
    /// Provide a function that indicates how much to shift the color of the summon (in degrees) at any point in time.
    /// <br/> WARNING: This is a rendering function. Do not use `rand` (`brand` ok), or else replays will desync.
    /// </summary>
    public static BehOption HueShift(GCXU<BPY> shift) => new HueShiftProp(shift);
    
    /// <summary>
    /// Manually construct a two-color gradient for the object.
    /// <br/> Note: This will only have effect if you use it with the `recolor` palette.
    /// <br/> WARNING: This is a rendering function. Do not use `rand` (`brand` ok), or else replays will desync.
    /// </summary>
    public static BehOption Recolor(GCXU<TP4> black, GCXU<TP4> white) => new RecolorProp(black, white);
    
    /// <summary>
    /// Every frame, the entity will check the condition and destroy itself if it is true.
    /// <br/>Note: This is generally only necessary for player lasers. 
    /// </summary>
    public static BehOption Delete(GCXU<Pred> cond) => new DeleteProp(cond);

    public static BehOption Player(int cdFrames, int bossDmg, int stageDmg, string effect) =>
        new PlayerBulletProp(new PlayerBulletCfg(cdFrames, bossDmg, stageDmg, ResourceManager.GetEffect(effect)));

    #region impl
    
    public class CompositeProp : ValueProp<BehOption[]>, IUnrollable<BehOption> {
        public IEnumerable<BehOption> Values => value;
        public CompositeProp(params BehOption[] props) : base(props) { }
    }
    
    public class ValueProp<T> : BehOption {
        public readonly T value;
        public ValueProp(T value) => this.value = value;
    }

    public class ItemsProp : ValueProp<ItemDrops> {
        public ItemsProp(ItemDrops i) : base(i) { }
    }

    public class SmoothProp : BehOption {}
    public class SMProp: ValueProp<StateMachine> {
        public SMProp(StateMachine sm) : base(sm) { } 
    }

    public class ScaleProp : ValueProp<GCXF<float>> {
        public ScaleProp(GCXF<float> f) : base(f) { }
    }
    public class HPProp : ValueProp<GCXF<float>> {
        public HPProp(GCXF<float> f) : base(f) { }
    }

    public class LayerProp : ValueProp<Layer> {
        public LayerProp(Layer l) : base(l) { }
    }
    public class HueShiftProp : ValueProp<GCXU<BPY>> {
        public HueShiftProp(GCXU<BPY> f) : base(f) { }
    }
    public class RecolorProp : BehOption {
        public readonly GCXU<TP4> black;
        public readonly GCXU<TP4> white;

        public RecolorProp(GCXU<TP4> b, GCXU<TP4> w) {
            black = b;
            white = w;
        }
    }
    public class DeleteProp : ValueProp<GCXU<Pred>> {
        public DeleteProp(GCXU<Pred> f) : base(f) { }
    }
    
    public class PlayerBulletProp : ValueProp<PlayerBulletCfg> {
        public PlayerBulletProp(PlayerBulletCfg cfg) : base(cfg) { }
    }
    #endregion
    
}

public readonly struct RealizedBehOptions {
    public readonly bool smooth;
    public readonly SMRunner smr;
    public readonly float scale;
    public readonly int? hp;
    public readonly int? layer;
    public readonly ItemDrops? drops;
    [CanBeNull] public readonly BPY hueShift;
    public readonly (TP4 black, TP4 white)? recolor;
    [CanBeNull] public readonly Pred delete;
    public readonly PlayerBulletCfg? playerBullet;

    public RealizedBehOptions(BehOptions opts, GenCtx gcx, uint bpiid, Vector2 parentOffset, V2RV2 localOffset, ICancellee cT) {
        smooth = opts.smooth;
        smr = SMRunner.Run(opts.sm, cT, gcx);
        scale = opts.scale?.Invoke(gcx) ?? 1f;
        hp = (opts.hp?.Invoke(gcx)).FMap(x => (int) x);
        layer = opts.layer;
        drops = opts.drops;
        hueShift = opts.hueShift?.Add(gcx, bpiid);
        if (opts.recolor.Try(out var rc)) {
            recolor = (rc.black.Add(gcx, bpiid), rc.white.Add(gcx, bpiid));
        } else recolor = null;
        delete = opts.delete?.Add(gcx, bpiid);
        playerBullet = opts.playerBullet;
    }

    public RealizedBehOptions(RealizedLaserOptions rlo) {
        this.smr = rlo.smr;
        this.layer = rlo.layer;
        smooth = false;
        scale = 1f;
        hp = null;
        drops = null;
        hueShift = null; //handled by laser renderer
        recolor = null; //likewise
        this.delete = rlo.delete;
        playerBullet = rlo.playerBullet;
    }
}

public class BehOptions {
    public readonly bool smooth;
    [CanBeNull] public readonly StateMachine sm;
    [CanBeNull] public readonly GCXF<float> scale;
    [CanBeNull] public readonly GCXF<float> hp;
    public readonly GCXU<Pred>? delete;
    public readonly int? layer = null;
    public readonly ItemDrops? drops = null;
    public readonly GCXU<BPY>? hueShift;
    public readonly (GCXU<TP4> black, GCXU<TP4> white)? recolor;
    public readonly PlayerBulletCfg? playerBullet;
    public string ID => "_";

    public BehOptions(params BehOption[] props) : this(props as IEnumerable<BehOption>) { }

    public BehOptions(IEnumerable<BehOption> props) {
        foreach (var p in props.Unroll()) {
            if (p is SmoothProp) smooth = true;
            else if (p is SMProp smp) sm = smp.value;
            else if (p is ScaleProp sp) scale = sp.value;
            else if (p is HPProp hpp) hp = hpp.value;
            else if (p is LayerProp lp) layer = lp.value.Int();
            else if (p is ItemsProp ip) drops = ip.value;
            else if (p is HueShiftProp hsp) hueShift = hsp.value;
            else if (p is RecolorProp rcp) recolor = (rcp.black, rcp.white);
            else if (p is DeleteProp dp) delete = dp.value;
            else if (p is PlayerBulletProp pbp) playerBullet = pbp.value;
            else throw new Exception($"Bullet property {p.GetType()} not handled.");
        }
    }
}
}