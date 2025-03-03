﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BagoumLib;
using BagoumLib.Culture;
using BagoumLib.Mathematics;
using BagoumLib.Reflection;
using Danmokou.Behavior;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.Danmaku.Options;
using Danmokou.Danmaku.Patterns;
using Danmokou.DMath;
using Danmokou.Expressions;
using Danmokou.SM;
using Danmokou.SM.Parsing;
using JetBrains.Annotations;
using Scriptor;
using Scriptor.Compile;
using Scriptor.Definition;
using Scriptor.Expressions;
using Scriptor.Math;
using Scriptor.Reflection;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Danmokou.Reflection {
public static partial class Reflector {
    private static IAST<StateMachine?> ReflectSM(IParseQueue q) {
        var (method, pos) = q.ScanUnit(out _);
        if (method == "file") {
            q.Advance();
            var (file, fpos) = q.NextUnit(out _);
            return new AST.SMFromFile(pos, fpos, file);
        } else if (method == "null") {
            q.Advance();
            return new AST.Preconstructed<StateMachine?>(pos, null);
        } else if (method == "stall") {
            q.Advance();
            return new AST.Preconstructed<StateMachine?>(pos, HelperStateMachines.Stall());
        } else {
            var nested = q.NextChild();
            try {
                var result = StateMachine.Create(nested);
                return nested.HasLeftovers(out var npi) ?
                    new AST.Failure<StateMachine>(nested.WrapThrowLeftovers(npi, 
                        "Nested StateMachine construction has extra text at the end.")) { Basis = result } :
                    result;
            } catch (Exception ex) {
                
                throw new ReflectionException(pos, $"Nested StateMachine construction starting at {pos} failed.", ex);
            }
        }
    }

    /// <summary>
    /// Maps types to a function that parses that type from a single word.
    /// </summary>
    private static readonly Dictionary<Type, Func<string, object?>> SimpleFunctionResolver =
        new() {
            {typeof(float), arg => SimpleParser.Float(arg)},
            {typeof(V2RV2), arg => SimpleParser.ParseV2RV2(arg)},
            {typeof(CRect), arg => DMath.Parser.ParseRect(arg)},
            {typeof(CCircle), arg => DMath.Parser.ParseCircle(arg)},
            {typeof(ETime.Timer), ETime.Timer.GetTimer},
        };


    private static readonly Type tint = typeof(int);
    private static readonly Type type_stylesel = typeof(StyleSelector);
    private static readonly Type type_alias = typeof(ReflectEx.Alias);
    private static readonly Type type_gcrule = typeof(GCRule);
    private static readonly Type gtype_ienum = typeof(IEnumerable<>);
    private static readonly Type type_locstring = typeof(LString);


    private static bool MatchesGeneric(Type target, Type generic) =>
        target.IsConstructedGenericType && target.GetGenericTypeDefinition() == generic;

    /// <summary>
    /// A cached dictionary of constructor signatures from GetConstructorSignature.
    /// </summary>
    private static readonly Dictionary<Type, MethodSignature?> constructorSigs = new();
    
    /// <summary>
    /// Finds a public constructor (preferably one with at least one argument) for the given type.
    /// </summary>
    /// <exception cref="StaticException">Thrown if the type has no public constructors.</exception>
    public static MethodSignature GetConstructorSignature(Type t) {
        return TryGetConstructorSignature(t) ?? throw new StaticException($"Type {t.SimpRName()} has no applicable constructors.");
    }

    /// <summary>
    /// Finds a public constructor (preferably one with at least one argument) for the given type.
    /// </summary>
    public static MethodSignature? TryGetConstructorSignature(Type t) {
        if (constructorSigs.TryGetValue(t, out var args)) return args;
        return constructorSigs[t] = TryGetConstructorSignature_Uncached(t);
    }
    private static MethodSignature? TryGetConstructorSignature_Uncached(Type t) {
        var constrs = t.GetConstructors();
        (ConstructorInfo c, ParameterInfo[] prms)? constr = null;
        foreach (var c in constrs) {
            if (c.GetCustomAttribute<DontReflectAttribute>() == null &&
                c.GetParameters() is { } prms && (prms.Length > 0 || constr == null)) {
                constr = (c, prms);
            }
        }
        if (!constr.Try(out var cp))
            return null;
        return MethodSignature.Get(cp.c);
    }

    private static IAST? ResolveSpecialHandling(IParseQueue p, Type targetType) {
        if (targetType == type_locstring) {
            var (str, pos) = p.NextUnit(out var lsi);
            if (LocalizedStrings.IsLocalizedStringReference(str)) {
                if (LocalizedStrings.TryFindReference(str) is { } ls)
                    return new AST.Preconstructed<LString>(pos, ls);
                else if (LangParser.SoftFailOnUnmatchedLString)
                    return new AST.Preconstructed<LString>(pos,
                        $"Unresolved LocalizedString {str}") {
                        Diagnostics = new ReflectDiagnostic[] {
                            new ReflectDiagnostic.Warning(pos, $"Couldn't resolve LocalizedString {str}. It may work properly in-game.")
                        }
                    };
                else
                    throw p.WrapThrowHighlight(lsi, $"Couldn't resolve LocalizedString {str}");
            } else 
                return new AST.Preconstructed<LString>(pos, str);
        } else if (targetType == type_stylesel) {
            return new ASTFmap<Array, StyleSelector>(
                s => new StyleSelector(s as string[][] ?? throw new StaticException(""), false),
                ResolveAsArray(typeof(string[]), p)
            );
        } else if (targetType == type_gcrule) {
            var (rfrString, rfrLoc) = p.NextUnit(out _);
            ReferenceMember rfr = new ReferenceMember(rfrString);
            var (OpAndMaybeType, opLoc) = p.NextUnit(out var opInd);
            var op = (GCOperator)ForceFuncTypeResolve(OpAndMaybeType, typeof(GCOperator))!;
            var latter = OpAndMaybeType.Split('=').Try(1) ??
                         throw p.WrapThrowHighlight(opInd,
                             $"Trying to parse GCRule, but found an invalid operator {OpAndMaybeType}.\n" +
                             "Make sure to put parentheses around the right-hand side of GCRule.");
            var ext = (ExType)ForceFuncTypeResolve(latter.Length > 0 ? latter : p.Next(), typeof(ExType))!;
            var nested = p.NextChild();
            IAST Handle<T>() {
                return new AST.GCRule<T>(rfrLoc, opLoc, ext, rfr, op, nested.IntoAST<GCXF<T>>());
            }
            return ext switch {
                ExType.Float => Handle<float>(),
                ExType.V2 => Handle<Vector2>(),
                ExType.V3 => Handle<Vector3>(),
                ExType.RV2 => Handle<V2RV2>(),
                _ => throw new StaticException($"No GCRule handling for ExType {ext}")
            };
        } else if (targetType == type_alias) {
            var (declStr, declPos) = p.NextUnit(out _);
            ExType declTyp = (ExType) ForceFuncTypeResolve(declStr, typeof(ExType))!;
            var (alias, aliasPos) = p.NextUnit(out _);
            var req_type = typeof(Func<,>).MakeGenericType(typeof(TExArgCtx), AsWeakTExType(declTyp));
            return new AST.Alias(declPos, aliasPos, AsType(declTyp), alias, ReflectTargetType(p, req_type));
        } else if (UseConstructor(targetType)) {
            //generic struct/tuple handling
            var sig = GetConstructorSignature(targetType).Call(null);
            var fill = FillASTArray(sig, p);
            var loc = fill.ArgRange ?? p.Position;
            return AST.Failure.MaybeEnclose(new AST.MethodInvoke(loc, new(loc.Start, loc.Start), sig, fill.ASTs), fill.Error);
        } else return null;
    }

    public static bool UseConstructor(Type t) => autoConstructorTypes.Contains(t) ||
                                                 (t.IsValueType && !t.IsPrimitive && !t.IsEnum);

    //note: duplicated in Reflector2.DMKScope
    public static readonly HashSet<Type> autoConstructorTypes = new() {
        typeof(GenCtxProperties<SyncPattern>),
        typeof(GenCtxProperties<AsyncPattern>),
        typeof(GenCtxProperties<StateMachine>),
        typeof(SBOptions),
        typeof(LaserOptions),
        typeof(BehOptions),
        typeof(PowerAuraOptions),
        typeof(PatternProperties)
    };
    
    /// <summary>
    /// Resolves types for which there is a simple parser (such as <see cref="V2RV2"/>).
    /// </summary>
    private static AST? FuncTypeResolve(IParseQueue q, SMParser.ParsedUnit.Str s, Type targetType) {
        if (SimpleFunctionResolver.TryGetValue(targetType, out Func<string, object?> resolver))
            try {
                return new AST.Preconstructed<object?>(s.Position, resolver(s.Item));
            } catch (Exception) {
                // ignored
            }
        return null;
    }

    /// <summary>
    /// Resolves types for which there is a simple parser (such as <see cref="V2RV2"/>)
    /// or throws an exception.
    /// </summary>
    private static object? ForceFuncTypeResolve(string s, Type targetType) {
        if (SimpleFunctionResolver.TryGetValue(targetType, out Func<string, object?> resolver)) {
            return resolver(s);
        } else throw new StaticException($"ForceFuncTypeResolve was used for a type {targetType} without a simple resolver");
    }

    public static IAST<Array> ResolveAsArray(Type eleType, IParseQueue q) {
        if (IParseQueue.ARR_EMPTY.Contains(q.MaybeScan() ?? "")) {
            var (_, loc) = q.NextUnit(out _);
            var empty = Array.CreateInstance(eleType, 0);
            return new AST.Preconstructed<Array>(loc, empty);
        }
        if (q.MaybeScan() != IParseQueue.ARR_OPEN) {
            var ele = ReflectTargetType(q, eleType);
            return new AST.SequenceArray(ele.Position, eleType, new[] { ele });
        }
        var (_, open) = q.NextUnit(out _); // {
        var tempList = new List<IAST>();
        while (q.MaybeScan() != IParseQueue.ARR_CLOSE) {
            tempList.Add(ReflectTargetType(q.NextChild(), eleType));
            //If there's an error, advance a token to prevent an infinite loop
            if (tempList[^1].IsUnsound)
                if (q.MaybeScan() != IParseQueue.ARR_CLOSE)
                    q.Advance();
        }
        var (_, close) = q.NextUnit(out _); // }
        return new AST.SequenceArray(open.Merge(close), eleType, tempList.ToArray());
    }
}
}

