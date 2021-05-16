﻿using System;
using System.Collections;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.DMath;
using Danmokou.Reflection;
using Danmokou.SM;
using UnityEngine;
using static Danmokou.Reflection.Compilers;
using static Danmokou.DMath.Functions.ExMLerps;
using static Danmokou.DMath.Functions.ExMV4;

namespace Danmokou.Player {
public enum PlayerBombType {
    NONE,
    TEST_BOMB_1,
    TEST_POWERBOMB_1
}

public enum PlayerBombContext {
    NORMAL,
    DEATHBOMB
}
public static class PlayerBombs {
    public static readonly Events.IEvent<(PlayerBombType type, PlayerBombContext ctx)> BombFired =
        new Events.Event<(PlayerBombType, PlayerBombContext)>();
    public static bool IsValid(this PlayerBombType bt) => bt != PlayerBombType.NONE;

    public static int DeathbombFrames(this PlayerBombType bt) =>
        bt switch {
            PlayerBombType.TEST_BOMB_1 => 20,
            PlayerBombType.TEST_POWERBOMB_1 => 20,
            _ => 0
        };

    public static double? PowerRequired(this PlayerBombType bt) =>
        bt switch {
            PlayerBombType.TEST_POWERBOMB_1 => 1,
            _ => null
        };
    public static int? BombsRequired(this PlayerBombType bt) =>
        bt switch {
            PlayerBombType.TEST_BOMB_1 => 1,
            _ => null
        };

    private static double ContextCostMultiplier(this PlayerBombType bt, PlayerBombContext ctx) =>
        bt switch {
            _ => 1
        };

    private static IEnumerator BombCoroutine(PlayerBombType bomb, PlayerController bomber, MultiAdder.Token bombDisable) =>
        bomb switch {
            PlayerBombType.TEST_BOMB_1 => DoTestBomb1(bomber, bombDisable),
            PlayerBombType.TEST_POWERBOMB_1 => DoTestBomb1(bomber, bombDisable),
            _ => throw new Exception($"No bomb handling for {bomb}")
        };

    public static bool TryBomb(PlayerBombType bomb, PlayerController bomber, PlayerBombContext ctx) {
        var mult = bomb.ContextCostMultiplier(ctx);
        if (bomb.PowerRequired().Try(out var rp) && !GameManagement.Instance.TryConsumePower(-rp * mult)) 
            return false;
        if (bomb.BombsRequired().Try(out var rb) && !GameManagement.Instance.TryConsumeBombs((int)Math.Round(-rb * mult))) 
            return false;
        BombFired.Publish((bomb, ctx));
        var ienum = BombCoroutine(bomb, bomber, PlayerController.BombDisabler.CreateToken1(MultiOp.Priority.CLEAR_SCENE));
        bomber.RunDroppableRIEnumerator(ienum);
        return true;
    }
    
    private static readonly ReflWrap<TaskPattern> TB1_1 = ReflWrap.FromFunc("PlayerBombs.TB1_1", 
        () => SMReflection.dBossExplode(
            TP4(LerpT(_ => 0.5f, _ => 1.5f, _ => Red(),
                _ => new Vector4(1f, 1f, 1f, 0.9f))),
            TP4(_ => Red())
        ));
    private static readonly ReflWrap<StateMachine> TB1_2 = new ReflWrap<StateMachine>(@"
async gpather-red/w <-90> gcr3 20 1.6s <> {
    frv2 angle(randpm1 * rand 20 50)
} pather(0.5, 0.5, tpnrot(
	truerotatelerprate(lerpt(1.2, 1.7, 170, 0),
		rotify(cx 1),
		(LNearestEnemy - loc)) 
            * lerp3(0.0, 0.3, 1.1, 1.3, t, 14, 2, 17)), { 
	player(120, 800, 100, oh1-red)
	s(2)
})
");
    private static IEnumerator DoTestBomb1(PlayerController bomber, MultiAdder.Token bombDisable) {
        Log.Unity("Starting Test Bomb 1", level: Log.Level.DEBUG2);
        var fireDisable = PlayerController.FiringDisabler.CreateToken1(MultiOp.Priority.CLEAR_SCENE);
        var smh = new SMHandoff(bomber);
        _ = TB1_1.Value(smh);
        _ = TB1_2.Value.Start(smh);
        PlayerController.RequestPlayerInvulnerable.Publish(((int)(120f * (EventLASM.BossExplodeWait + 3f)), true));
        for (float t = 0; t < EventLASM.BossExplodeWait; t += ETime.FRAME_TIME) yield return null;
        var circ = new CCircle(bomber.hitbox.location.x, bomber.hitbox.location.y, 8f);
        BulletManager.Autodelete(new SoftcullProperties(null, null), 
            bpi => CollisionMath.PointInCircle(bpi.loc, circ));
        fireDisable.TryRevoke();
        for (float t = 0; t < 4f; t += ETime.FRAME_TIME) yield return null;
        Log.Unity("Ending Test Bomb 1", level: Log.Level.DEBUG2);
        bombDisable.TryRevoke();
    }
    
}
}