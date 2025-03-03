﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using BagoumLib;
using BagoumLib.Expressions;
using BagoumLib.Mathematics;
using Danmokou.Core;
using Danmokou.Expressions;
using Danmokou.Scenes;
using JetBrains.Annotations;
using UnityEngine;
using Random = System.Random;

namespace Danmokou.DMath {
/// <summary>
/// Centralized class for access to randomness.
/// Provides functions for deterministic randomness, as well as non-reproducible
/// "offFrame" functions that should be used for non-gameplay-related randomness
/// (camera shake, particle effects, etc).  
/// </summary>
public static class RNG {
    private static Random rand = new Random();
    //Use this instead when the code is not in the RU loop
    private static readonly Random offFrame = new Random();

    public static void Seed(int seed) {
        rand = new Random(seed);
    }

    public static bool RNG_ALLOWED = true;

    private static void RNGGuard() {
        if (!RNG_ALLOWED) {
            throw new Exception(
                "You are invoking random functions either outside of the regular update loop, or in parallelized pather movement code. This will desync replays and/or cause the random generation to fail.");
        }
        if (EngineStateManager.State > EngineState.RUN && !SceneIntermediary.LOADING) {
            Logs.Log("You are invoking random functions while replay data is not being saved. " +
                      "This will desync replays.", true, LogLevel.WARNING);
        }
    }

    public static uint GetUInt() {
        RNGGuard();
        /*var u = (((uint) rand.Next(1 << 30)) << 2) | (uint) rand.Next(1 << 2);
        Log.Unity(u.ToString());
        return u;*/
        return (((uint) rand.Next(1 << 30)) << 2) | (uint) rand.Next(1 << 2);
    }
    public static uint GetUIntOffFrame() {
        return (((uint) offFrame.Next(1 << 30)) << 2) | (uint) offFrame.Next(1 << 2);
    }

    public const uint HalfMax = uint.MaxValue / 2;

    private const long knuth = 2654435761;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Rehash(in uint x) => (uint) (knuth * x);

    public static Expression Rehash(Expression uintx) => Expression.Convert(
        Expression.Multiply(Expression.Constant(knuth), Expression.Convert(uintx, typeof(long))), typeof(uint));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Rehash(in int x) => (int) (knuth * x);

    public static T Random<T>(this IList<T> arr) => arr[GetInt(0, arr.Count)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetInt(in int low, in int high, in Random r) {
        return r.Next(low, high);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetInt(int low, int high) {
        RNGGuard();
        return GetInt(low, high, rand);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIntOffFrame(in int low, in int high) => GetInt(low, high, offFrame);
    
    private static readonly ExFunction getInt =
        ExFunction.Wrap(typeof(RNG), nameof(GetInt), new[] {typeof(int), typeof(int)});
    public static Expression GetInt(Expression low, Expression high) => getInt.Of(low, high);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetFloat(in float low, in float high, in Random r) {
        return low + (high - low) * (float) r.NextDouble();
    }

    public static float GetFloat(float low, float high) {
        RNGGuard();
        return GetFloat(low, high, rand);
    }

    public static Vector2 GetPointInCircle(float lowR, float highR) =>
        GetFloat(lowR, highR) * M.CosSin(GetFloat(0, BMath.TAU));

    private static readonly ExFunction getFloat =
        ExFunction.Wrap(typeof(RNG), nameof(GetFloat), new[] {typeof(float), typeof(float)});
    public static Expression GetFloat(Expression low, Expression high) => getFloat.Of(low, high);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetFloatOffFrame(in float low, in float high) => GetFloat(low, high, offFrame);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetV3OffFrame(in Vector3 low, in Vector3 high) => new Vector3(
        GetFloatOffFrame(low.x, high.x),
        GetFloatOffFrame(low.y, high.y),
        GetFloatOffFrame(low.z, high.z)
    );

    [UsedImplicitly]
    public static float GetSeededFloat(float low, float high, uint seedv) {
        return low + (high - low) * Rehash(seedv) / uint.MaxValue;
    }

    [UsedImplicitly]
    public static float GetSeededFloat(float low, float high, int seedv) {
        return low + (high - low) * (0.5f + ((float) Rehash(seedv) / int.MaxValue));
    }

    private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string _RandString(Random r, int len) {
        var stringChars = new char[len];
        for (int i = 0; i < stringChars.Length; i++) {
            stringChars[i] = CHARS[r.Next(CHARS.Length)];
        }
        return new string(stringChars);
    }

    public static string RandString(int len = 8) {
        RNGGuard();
        return _RandString(rand, len);
    }

    public static string RandStringOffFrame(int len = 16) => _RandString(offFrame, len);

    public static T RandSelectOffFrame<T>(T[] arr) => arr[GetIntOffFrame(0, arr.Length)];
}
}
