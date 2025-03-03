﻿using System;
using BagoumLib.Tasks;
using Danmokou.Core;
using JetBrains.Annotations;
using UnityEngine;

namespace Danmokou.Behavior.Display {

public enum DirectionRelation {
    RUFlipsLD,
    LDFlipsRU,
    RUCopiesLD,
    LDCopiesRU,
    Independent,
    None
}

public class FrameAnimDisplayController : SpriteDisplayController {
    [Serializable]
    public struct Animation {
        [Serializable]
        public struct FrameConfig {
            public Frame[] idleAnim;
            public Frame[] rightAnim;
            public Frame[] leftAnim;
            public Frame[] upAnim;
            public Frame[] downAnim;
            public Frame[] attackAnim;
            public Frame[] deathAnim;
            public FrameRunner runner;

            public Frame[] FramesForAnim(AnimationType typ) {
                if (typ == AnimationType.Attack) return attackAnim;
                if (typ == AnimationType.Right) return rightAnim;
                if (typ == AnimationType.Left) return leftAnim;
                if (typ == AnimationType.Up) return upAnim;
                if (typ == AnimationType.Down) return downAnim;
                if (typ == AnimationType.Death) return deathAnim;
                return idleAnim;
            }
            
            public Sprite? SetAnimationTypeIfPriority(AnimationType typ, bool loop, Action? onLoopOrFinish) => 
                runner.SetAnimationTypeIfPriority(typ, FramesForAnim(typ), loop, onLoopOrFinish);

            public Sprite? ResetToIdle() => 
                runner.SetAnimationType(AnimationType.None, FramesForAnim(AnimationType.None), true, WaitingUtils.NoOp);

            public Sprite? Update(float dT) {
                var (resetMe, updSprite) = runner.Update(dT);
                return resetMe ? ResetToIdle() : updSprite;
            }
        }

        public DirectionRelation LRRelation;
        public DirectionRelation UDRelation;
        public FrameConfig frames;

        private Action<Sprite?> setSprite;
        private Action<bool, bool> setScale;
        public void Initialize(Action<Sprite?> spriteSet, Action<bool, bool> scaleSet) {
            this.setSprite = spriteSet;
            this.setScale = scaleSet;
            setSprite(frames.ResetToIdle());
        }

        private int DirectionToAnimInt(LRUD? d) {
            if (d == LRUD.RIGHT) return 1;
            if (d == LRUD.UP) return 2;
            if (d == LRUD.LEFT) return 3;
            if (d == LRUD.DOWN) return 4;
            return 0;
        }

        private void SetDirection(LRUD?d, bool flipX, bool flipY) {
            if (d != null)
                //Only update scale when actively moving in a direction
                setScale(flipX, flipY);
            setSprite(frames.SetAnimationTypeIfPriority(AsAnimType(d), true, WaitingUtils.NoOp));
        }

        private LRUD? Opposite(LRUD? d) {
            if (d == LRUD.RIGHT) return LRUD.LEFT;
            if (d == LRUD.LEFT) return LRUD.RIGHT;
            if (d == LRUD.UP) return LRUD.DOWN;
            if (d == LRUD.DOWN) return LRUD.UP;
            return null;
        }

        private AnimationType AsAnimType(LRUD? d) {
            if (d == LRUD.RIGHT) return AnimationType.Right;
            if (d == LRUD.LEFT) return AnimationType.Left;
            if (d == LRUD.UP) return AnimationType.Up;
            if (d == LRUD.DOWN) return AnimationType.Down;
            return AnimationType.None;
        }

        private (LRUD? d, bool flipX, bool flipY) ReduceDirection(LRUD? primary, LRUD? secondary) {
            var dfx = ReduceDirection(primary);
            if (dfx.d == null) dfx = ReduceDirection(secondary);
            return dfx;
        }
        private (LRUD? d, bool flipX, bool flipY) ReduceDirection(LRUD? d) {
            bool flipX = false;
            bool flipY = false;
            if        (d == LRUD.LEFT) {
                if (LRRelation == DirectionRelation.None) d = null;
                if (LRRelation == DirectionRelation.LDCopiesRU) d = LRUD.RIGHT;
                if (LRRelation == DirectionRelation.LDFlipsRU) {
                    d = LRUD.RIGHT;
                    flipX = true;
                }
            } else if (d == LRUD.RIGHT) {
                if (LRRelation == DirectionRelation.None) d = null;
                if (LRRelation == DirectionRelation.RUCopiesLD) d = LRUD.LEFT;
                if (LRRelation == DirectionRelation.RUFlipsLD) {
                    d = LRUD.LEFT;
                    flipX = true;
                }
            } else if (d == LRUD.UP) {
                if (UDRelation == DirectionRelation.None) d = null;
                if (UDRelation == DirectionRelation.RUCopiesLD) d = LRUD.DOWN;
                if (UDRelation == DirectionRelation.RUFlipsLD) {
                    d = LRUD.DOWN;
                    flipY = true;
                }
            } else if (d == LRUD.DOWN) {
                if (UDRelation == DirectionRelation.None) d = null;
                if (UDRelation == DirectionRelation.LDCopiesRU) d = LRUD.UP;
                if (UDRelation == DirectionRelation.LDFlipsRU) {
                    d = LRUD.UP;
                    flipY = true;
                }
            }
            return (d, flipX, flipY);
        }

        private const float movCutoff = 0.0000001f;
        /// <summary>
        /// Select the animation according to the direction.
        /// </summary>
        /// <param name="dir">Unnormalized direction vector.</param>
        public void FaceInDirection(Vector2 dir) {
            LRUD? d1 = null;
            LRUD? d2 = null;
            dir = dir.normalized;
            var x = dir.x * dir.x;
            var y = dir.y * dir.y;
            var lr = (x < movCutoff) ? (LRUD?)null : (dir.x > 0) ? LRUD.RIGHT : LRUD.LEFT;
            var ud = (y < movCutoff) ? (LRUD?)null : (dir.y > 0) ? LRUD.UP : LRUD.DOWN;
            if (x > y) {
                d1 = lr;
                d2 = ud;
            } else {
                d1 = ud;
                d2 = lr;
            }
            var (direction, flipX, flipY) = ReduceDirection(d1, d2);
            SetDirection(direction, flipX, flipY);
        }

        public void Update(float dT) {
            setSprite(frames.Update(dT));
        }

        public void Animate(AnimationType typ, bool loop, Action? done) {
            setSprite(frames.SetAnimationTypeIfPriority(typ, loop, done));
        }

        public bool HasAnimation(AnimationType typ) =>
            frames.FramesForAnim(typ).Length > 0;
    }

    public Animation animate;
    
    public override void OnLinkOrResetValues(bool isLink) {
        base.OnLinkOrResetValues(isLink);
        animate.Initialize(SetSprite, SetFlip);
    }
    public override void OnRender(bool isFirstFrame, Vector2 lastDesiredDelta) {
        animate.Update(ETime.FRAME_TIME);
        base.OnRender(isFirstFrame, lastDesiredDelta);
    }

    public override void FaceInDirection(Vector2 delta) {
        animate.FaceInDirection(delta);
        base.FaceInDirection(delta);
    }

    public override void Animate(AnimationType typ, bool loop, Action? done) {
        animate.Animate(typ, loop, done);
    }

    public override void Culled(bool allowFinalize, Action done) {
        if (animate.HasAnimation(AnimationType.Death))
            Animate(AnimationType.Death, false, () => base.Culled(allowFinalize, done));
        else
            base.Culled(allowFinalize, done);
    }
}
}