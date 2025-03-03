﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BagoumLib;
using BagoumLib.DataStructures;
using BagoumLib.Functional;
using BagoumLib.Reflection;
using BagoumLib.Unification;
using Danmokou.Core;
using Danmokou.DMath;
using Danmokou.Expressions;
using Danmokou.SM;
using Danmokou.SM.Parsing;
using JetBrains.Annotations;
using Mizuhashi;
using Scriptor;
using Scriptor.Analysis;
using Scriptor.Compile;
using Scriptor.Expressions;
using Scriptor.Reflection;
using UnityEngine.Profiling;
using Parser = Danmokou.DMath.Parser;

namespace Danmokou.Reflection {
public static partial class Reflector {
    public class ReflCtx {
        public enum Strictness {
            NONE = 0,
            COMMAS = 1
        }

        public ParsingProperties Props { get; private set; } = new(Array.Empty<ParsingProperty>());
        public bool AllowPostAggregate => Props.strict >= Strictness.COMMAS;
        public bool UseFileLinks { get; set; } = true;
        public List<IAST<PhaseProperty>> QueuedProps { get; } = new();
        /// <summary>
        /// Errors that are not critical, but should be reported if the end of parsing has leftovers and
        /// does not parse past the error's position.
        /// </summary>
        public List<(ReflectionException exc, Type targetType)> NonfatalErrors { get; } = new();

        private class NonfatalErrorComparer : IEqualityComparer<(ReflectionException, Type)> {
            public static readonly NonfatalErrorComparer Comparer = new();
            
            public bool Equals((ReflectionException, Type) x, (ReflectionException, Type) y) => 
                x.Item1.Message == y.Item1.Message;
            public int GetHashCode((ReflectionException, Type) obj) => 
                obj.Item1.Message.GetHashCode();
        }

        public IEnumerable<(ReflectionException, Type)> NonfatalErrorsForPosition(PositionRange pos) =>
            NonfatalErrors
                .Where(e => pos == e.exc.Position)
                .Distinct(NonfatalErrorComparer.Comparer);

        public Exception? ParseEndFailure(IParseQueue q, IAST ast) {
            if (q.HasLeftovers(out var qpi)) {
                //Nonfatals only get reported if there are leftovers
                var errs = q.Ctx.NonfatalErrorsForPosition(q.GetCurrentUnit(out _).Position).Select(f => f.Item1);
                //errors thrown during evaluate are thrown together with nonfatals
                if (ast.IsUnsound)
                    errs = errs.Prepend(ast.Exceptions.First());
                return Exceptions.MaybeAggregate(errs.ToList()) ??
                      q.WrapThrowLeftovers(qpi, 
                          "Behavior script has extra text. Check the text in ≪≫ below for an illegal command.", ast.Exceptions.FirstOrDefault());
            } else if (ast.IsUnsound)
                return ast.Exceptions.First();
            return null;
        }

        public void ParseProperties(IParseQueue q) {
            List<ParsingProperty> properties = new();
            while (q.MaybeScan() == SMParser.PROP2_KW) {
                q.Advance();
                var child = q.NextChild();
                properties.Add(child.Into<ParsingProperty>());
                if (!q.IsNewline)
                    throw child.WrapThrow($"Missing a newline at the end of the property " +
                                          $"declaration. Instead, it found \"{q.Scan()}\".");
            }
            Props = new ParsingProperties(properties);
        }

        /// <summary>
        /// Generates a file link for the method signature if permitted by the
        /// <see cref="UseFileLinks"/> property, else just use the method name.
        /// </summary>
        public string AsFileLink(InvokedMethod sig) => 
            UseFileLinks ? sig.FileLink : sig.TypeEnclosedName;

        public static ReflCtx Neutral = new ReflCtx();
    }

    public record ASTArrayFill(IAST[] ASTs, PositionRange? ArgRange) {
        public bool Parenthesized { get; set; }
        public ReflectionException? Error { get; set; }
    }
    
    
    /// <summary>
    /// Number of parameters that must be parsed by reflection.
    /// </summary>
    private static int ExplicitParameterCount(IMethodSignature sig, int start = 0, int? end = null) {
        var ct = 0;
        for (int ii = start; ii < (end ?? sig.Params.Length); ++ii)
            if (sig.FeaturesAt(ii)?.NonExplicit is not true)
                ++ct;
        return ct;
    }

    /// <summary>
    /// Fill the argument array `asts` by parsing elements from q according to type information in prms.
    /// </summary>
    /// <param name="asts">Argument array to fill.</param>
    /// <param name="starti">Index of `asts` to start from, inclusive.</param>
    /// <param name="endi">Index of `asts` to end at, exclusive.</param>
    /// <param name="sig">Type information of arguments.</param>
    /// <param name="q">Queue from which to parse elements.</param>
    /// <returns>Filled array of ASTs, range covered by the ASTs, and an exception that should enclose the caller if nonnull.</returns>
    public static ASTArrayFill FillASTArray(IAST[] asts, int starti, int endi, InvokedMethod sig, IParseQueue q) {
        int nargs = ExplicitParameterCount(sig.Mi, starti, endi);
        var prms = sig.Mi.Params;
        if (nargs == 0) {
            if (!(q is ParenParseQueue) && !q.Empty) {
                //Zero-arg functions may absorb empty parentheses
                if (q.MaybeGetCurrentUnit(out _) is SMParser.ParsedUnit.Paren p) {
                    if (p.Items.Length == 0) {
                        q.Advance();
                        return new(asts, p.Position);
                    }
                }
            }
            return new(asts, null);
        }
        if (!(q is ParenParseQueue)) {
            if (q.MaybeGetCurrentUnit(out _) is SMParser.ParsedUnit.Paren p && p.Items.Length == 1 && nargs != 1) {
                // mod | (x) 3
                //Leave the parentheses to be parsed by the first argument
            } else if (!q.Empty)
                // mod | (x, 3)
                //Use the parentheses to fill arguments
                //OR
                // mod | x 3
                //Use NLParseList to fill arguments
                q = q.NextChild();
        }

        for (int ii = starti; ii < endi; ++ii) {
            if (sig.Mi.FeaturesAt(ii)?.NonExplicit is true) {
                asts[ii] = ReflectNonExplicitParam(q, prms[ii]);
            } else {
                bool ThrowEmpty(IParseQueue lq) {
                    if (lq.Empty) {
                        var endP = lq.PositionUpToCurrentObject.End;
                        
                        asts[ii] = new AST.Failure(
                            new ReflectionException(new(endP, endP.Increment()), lq.WrapThrow(
                            $"Tried to construct {q.AsFileLink(sig)}, but the parser ran out of text when looking for argument " +
                            $"#{ii + 1}/{prms.Length} ({prms[ii].AsParameter}). " +
                            "This probably means you have parentheses that do not enclose the entire function.") +
                            $" | [Arg#{ii + 1} Missing]"), prms[ii]);
                        return true;
                    } else return false;
                }
                if (ThrowEmpty(q)) continue;
                var local = q.NextChild();
                if (ThrowEmpty(local)) continue;
                string MakeErr() => $"Tried to construct {q.AsFileLink(sig)}, but failed to create argument " +
                                    $"#{ii + 1}/{prms.Length} ({prms[ii].AsParameter})";
                try {
                    if ((asts[ii] = ReflectParam(local, prms[ii], sig.Mi.FeaturesAt(ii))).IsUnsound) {
                        asts[ii] = new AST.Failure(new(local.Position, MakeErr()), prms[ii]) { Basis = asts[ii] };
                        continue;
                    }
                } catch (Exception ex) {
                    asts[ii] = new AST.Failure(new(local.Position, MakeErr(), ex), prms[ii]);
                    continue;
                }
                if (local.HasLeftovers(out var lpi))
                    asts[ii] = new AST.Failure(local.WrapThrowLeftovers(lpi,
                            $"Argument #{ii + 1}/{prms.Length} ({prms[ii].AsParameter}) has extra text."),
                        prms[ii]) { Basis = asts[ii] };
            }
        }
        ReflectionException? fail = null;
        if (q is ParenParseQueue p2 && nargs != p2.Items.Length) {
            fail = p2.WrapThrow($"Expected {nargs} explicit arguments for {q.AsFileLink(sig)}, " +
                               $"but the parentheses contains {p2.Items.Length}.");
        } else if (q.HasLeftovers(out var qpi))
            fail = q.WrapThrowLeftovers(qpi, 
                $"{q.AsFileLink(sig)} has extra text after all {prms.Length} arguments.");
        return new(asts, q is ParenParseQueue pq ? pq.Position : asts.ToRange()) {
            Error = fail,
            Parenthesized = q is ParenParseQueue
        };
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ASTArrayFill FillASTArray(InvokedMethod sig, IParseQueue q)
        => FillASTArray(new IAST[sig.Mi.Params.Length], 0, sig.Mi.Params.Length, sig, q);

    

    #region TargetTypeReflect

    
    /// <summary>
    /// Parse a string into an object of type T.
    /// <br/>May throw an exception if parsing fails.
    /// </summary>
    public static object? Into(this string argstring, Type t) {
        return intoMeth.Specialize(t).Invoke(argstring);
    }
    
    /// <summary>
    /// Parse a string into an object of type T.
    /// <br/>May throw an exception if parsing fails.
    /// </summary>
    public static T Into<T>(this string argstring) {
        using var _ = BakeCodeGenerator.OpenContext(CookingContext.KeyType.INTO, argstring);
        try {
            Profiler.BeginSample("Generic Into AST (BDSL2) Parsing/Compilation");
            var (val, ef) = CompileHelpers.ParseAndCompileValue<T>(argstring);
            Profiler.EndSample();
            return val;
        } catch (Exception e) {
            throw new Exception($"Failed to parse below string into type {typeof(T).RName()}:\n{argstring}", e);
        }
    }

    public static Func<TExArgCtx, TEx<T>> IntoDelayed<T>(this string argstring) {
        var bake = BakeCodeGenerator.OpenContext(CookingContext.KeyType.INTO, argstring);
        try {
            var (ast, gs) = CompileHelpers.ParseAnnotate(ref argstring);
            var typechecked = ast.Typecheck(gs, typeof(T), out _);
            var verified = typechecked.Finalize();
            return tac => {
                var ex = verified.Realize(tac);
                bake?.Dispose();
                bake = null;
                return (TEx<T>)ex;
            };
        } catch (Exception e) {
            throw new Exception($"Failed to parse below string into type {typeof(T).RName()}:\n{argstring}", e);
        }
        
    }
    
    private static readonly GenericMethodSignature intoMeth = (GenericMethodSignature)
        MethodSignature.Get(typeof(Reflector).GetMethod(nameof(Into), new[]{typeof(string)})!);

    /// <summary>
    /// Parse a string into an object of type T. Returns null if the string is null or whitespace-only.
    /// <br/>May throw an exception if parsing fails.
    /// </summary>
    public static T? IntoIfNotNull<T>(this string? argstring) where T : class {
        if (string.IsNullOrWhiteSpace(argstring)) return null;
        return Into<T>(argstring!);
    }
    
    /// <summary>
    /// Parse a string into an object of type T. Returns null if the string is null or whitespace-only.
    /// <br/>May throw an exception if parsing fails.
    /// </summary>
    public static object? IntoIfNotNull(this string? argstring, Type t) {
        if (string.IsNullOrWhiteSpace(argstring)) return null;
        return Into(argstring!, t);
    }

    public static T Into<T>(this IParseQueue q) => ((T) q.IntoAST(typeof(T)).EvaluateObject()!);

    public static T IntoBDSL1<T>(this string argstring) {
        using var _ = BakeCodeGenerator.OpenContext(CookingContext.KeyType.INTO, argstring);
        try {
            var p = IParseQueue.Lex(argstring);
            var ast = p.IntoAST(typeof(T));
            Profiler.EndSample();
            foreach (var d in ast.WarnUsage(p.Ctx))
                d.Log();
            if (p.Ctx.ParseEndFailure(p, ast) is { } exc)
                throw exc;
            //In-code scopes can get called arbitrarily
            var rootScope = LexicalScope.NewTopLevelDynamicScope();
            using var __ = new ParsingScope(rootScope);
            ast.AttachLexicalScope(rootScope);
            if (rootScope.FinalizeVariableTypes(Unifier.Empty).TryR(out var err))
                throw Scriptor.Compile.IAST.EnrichError(err);
            Profiler.BeginSample("AST realization");
            return (T)ast.EvaluateObject()!;
        } catch (Exception e) {
            throw new Exception($"Failed to parse below string into type {typeof(T).RName()}:\n{argstring}", e);
        }
    }
/*
    private static object? Into(this IParseQueue q, Type t) {
        var ast = q.IntoAST(t);
        //There's an odd circularity where the first constructed PUParseList
        // creates a ReflCtx, which then calls Into to construct parsing properties
        // *before* its constructor is complete and it gets assigned to q.Ctx.
        if (q.Ctx != null)
            foreach (var d in ast.WarnUsage(q.Ctx))
               d.Log();
        Profiler.BeginSample("AST realization");
        var obj = ast.EvaluateObject();
        Profiler.EndSample();
        return obj;
    }*/
    
    public static IAST<T> IntoAST<T>(this IParseQueue ctx) => new ASTRuntimeCast<T>(IntoAST(ctx, typeof(T)));
    public static IAST IntoAST(this IParseQueue ctx, Type t) => ReflectTargetType(ctx, t);

    /// <summary>
    /// Remove superfluous ParenParseQueue wrappers,
    /// eg. by converting ParenPQ (mod 4 2). into PUListPQ {mod 4 2}.
    /// </summary>
    private static void RecurseParens(ref IParseQueue q, Type t) {
        //Note: this will only ever remove one layer, since ParenPQ.NextChild always returns PUListPQ.
        //That is why we also need to run RecurseScan below.
        while (q is ParenParseQueue p) {
            if (p.Items.Length == 1) q = p.NextChild();
            else
                throw p.WrapThrow(
                    $"Tried to find an object of type {t.SimpRName()}, but there is a parentheses with" +
                    $" {p.Items.Length} elements. Any parentheses should only have one element.");
        }
    }

    /// <summary>
    /// A fallthrough parse queue has the ability but not the obligation to post-aggregate.
    /// </summary>
    private static IParseQueue MakeFallthrough(IParseQueue q) {
        if (q is PUListParseQueue p)
            return new NonLocalPUListParseQueue(p, true);
        return q;
    }

    private static IAST ReflectNonExplicitParam(IParseQueue q, NamedParam p) {
        if (p.Type == tPhaseProperties) {
            var props = q.Ctx.QueuedProps.Count > 0 ?
                new ASTFmap<List<PhaseProperty>, PhaseProperties>(ps => new PhaseProperties(ps), 
                new AST.SequenceList<PhaseProperty>(
                    q.Ctx.QueuedProps[0].Position.Merge(q.Ctx.QueuedProps[^1].Position), 
                    q.Ctx.QueuedProps.ToList())) :
                (IAST<PhaseProperties>)new AST.Preconstructed<PhaseProperties>(q.Position,
                    new PhaseProperties(Array.Empty<PhaseProperty>()), "No phase properties");
            q.Ctx.QueuedProps.Clear();
            return props;
        } else
            throw new StaticException($"No non-explicit reflection handling exists for type {p.Type.SimpRName()}");
    }

    private static IAST ReflectParam(IParseQueue q, NamedParam p, ParamFeatures? pf) {
        if (pf?.LookupMethod is true) {
            if (p.Type.GenericTypeArguments.Length == 0) 
                throw new StaticException("Method-Lookup parameter must be generic");
            RecurseParens(ref q, p.Type);
            var (method, loc) = q.ScanUnit(out _);
            //This can throw if the method is invalid, in which case let's not consume
            var ast = new AST.MethodLookup(loc, p.Type, method.ToLower());
            q.Advance();
            return q.WrapInErrorIfHasLeftovers(ast, p.Type);
        } else {
            return ReflectTargetType(q, p.Type);
        }
    }

    private static readonly Type tPhaseProperties = typeof(PhaseProperties);

    /// <summary>
    /// Top-level resolution function to create an object from a parse queue.
    /// </summary>
    /// <param name="q">Parsing queue to read from.</param>
    /// <param name="t">Type to construct.</param>
    /// <param name="postAggregateContinuation">Optional code to execute after post-aggregation is complete.</param>
    private static IAST ReflectTargetType(IParseQueue q, Type t, Func<IAST, Type, IAST>? postAggregateContinuation=null) {
        //While try/catch is not the best way to handle partial parses,
        // there are way too many internal cases where exceptions may be thrown,
        // and the 'correct' way would be using an Either-like monad with extensive bind operations,
        // which is not possible in C#. 
        try {
            RecurseParens(ref q, t);
            IAST? ast;
            var pu = q.GetCurrentUnit(out var index);
            if (pu is SMParser.ParsedUnit.Paren p) {
                //given `PUList | (mod) 1 2`, the next child is (mod), which must be recursed.
                //However, we cannot accept `PUList | (mod, 1) 2` as this is not an arglist.
                if (p.Items.Length != 1)
                    throw q.WrapThrowHighlight(index, "This parentheses must have exactly one argument.");
                var rec = new PUListParseQueue(p.Items[0], q.Ctx);
                q.Advance();
                ast = ReflectTargetType(
                    rec, t, (x, pt) => (postAggregateContinuation ?? ((y, _) => y))(DoPostAggregation(pt, q, x), pt));
                return rec.HasLeftovers(out _) ?
                    rec.WrapInErrorIfHasLeftovers(ast, t) :
                    q.WrapInErrorIfHasLeftovers(ast, t);
            }
            var arg = (pu as SMParser.ParsedUnit.Str) ?? throw new StaticException($"Couldn't result {pu.GetType()}");
            if (q.Empty)
                throw q.WrapThrow($"Ran out of text when trying to create an object of type {t.SimpRName()}.");
            else if (t == typeof(StateMachine))
                ast = ReflectSM(q);
            else if (ReflectMethod(arg, t, q) is { } methodAST) {
                //this advances inside
                ast = methodAST;
            } else if (referenceVarFuncs.TryGetValue(t, out var f) && arg.Item[0] == Parser.SM_REF_KEY_C) {
                q.Advance();
                ast = new AST.Preconstructed<object?>(arg.Position, f(arg.Item), arg.Item);
            } else if (FuncTypeResolve(q, arg, t) is { } simpleParsedAST) {
                q.Advance();
                ast = simpleParsedAST;
            } else if (FallThroughOptions.TryGetValue(t, out var ftmi)) {
                //MakeFallthrough allows the nested lookup to not be required to consume all post-aggregation.
                var ftype = ftmi.mi.Params[0].Type;
                ast = ReflectTargetType(MakeFallthrough(q), ftype, postAggregateContinuation);
                ast = new AST.MethodInvoke(ast, ftmi.mi.Call(null)) { Type = AST.MethodInvoke.InvokeType.Fallthrough };
                if (ast.IsUnsound)
                    ast = new AST.Failure(q.WrapThrowHighlight(index,
                        $"Failed to construct an object of type {t.RName()}. Instead, tried to construct a" +
                        $" similar object of type {ftype.RName()}, but that also failed."), t) { Basis = ast };
            } else if (TryCompileOption(t, out var cmp)) {
                ast = ReflectTargetType(MakeFallthrough(q), cmp.source, postAggregateContinuation);
                ast = new AST.MethodInvoke(ast, cmp.mi.Call(null)) { Type = AST.MethodInvoke.InvokeType.Compiler };
            } else if (ResolveSpecialHandling(q, t) is { } specialTypeAST) {
                ast = specialTypeAST;
            } else if (t.IsArray)
                ast = ResolveAsArray(t.GetElementType()!, q);
            else if (MatchesGeneric(t, gtype_ienum))
                ast = ResolveAsArray(t.GenericTypeArguments[0], q);
            else if (CastToType(arg.Item, t, out var x)) {
                ast = new AST.Preconstructed<object?>(arg.Position, x);
                q.Advance();
            } else {
                throw q.WrapThrowHighlight(index, $"Couldn't convert the text in ≪≫ to type {t.RName()}.");
            }
            if (!ast.IsUnsound) {
                ast = DoPostAggregation(t, q, ast);
                if (q.HasLeftovers(out _))
                    return q.WrapInErrorIfHasLeftovers(ast, t);
                if (q.Empty && postAggregateContinuation != null) {
                    ast = postAggregateContinuation(ast, t);
                }
            }
            return ast;
        } catch (Exception e) {
            if (e is ReflectionException re)
                return new AST.Failure(re, t);
            return new AST.Failure(new ReflectionException(q.Position, e.Message, e.InnerException), t);
        }
    }

    private static bool CastToType(string arg, Type rt, out object result) {
        if (arg == "_") {
            // Max value shortcut for eg. repeating until cancel
            if (rt == tint) {
                result = M.IntFloatMax;
                return true;
            }
        }
        try {
            result = Convert.ChangeType(arg, rt);
            return true;
        } catch (Exception) {
            result = null!;
            return false;
        }
    }

    private static IAST DoPostAggregation(Type rt, IParseQueue q, IAST result) {
        if (!q.AllowPostAggregate || q.Empty) return result;
        if (!postAggregators.TryGet2(rt, q.MaybeScan() ?? "", out _)) return result;
        var varStack1 = new StackList<IAST>();
        var varStack2 = new StackList<IAST>();
        var opStack1 = new StackList<(PostAggregate pa, PositionRange loc)>();
        var opStack2 = new StackList<(PostAggregate pa, PositionRange loc)>();
        varStack1.Push(result);
        while (!q.Empty && postAggregators.TryGet2(rt, q.MaybeScan() ?? "", out var pa)) {
            var op = q.NextUnit(out var opInd);
            opStack1.Push((pa, op.Position));
            try {
                varStack1.Push(ReflectTargetType(q.NextChild(), pa.searchType));
            } catch (Exception e) {
                throw q.WrapThrowHighlight(opInd, 
                    $"Tried to construct infix operator {op}, but could not parse the second argument.", e);
            }
        }
        while (opStack1.Count > 0) {
            varStack2.Clear();
            opStack2.Clear();
            varStack2.Push(varStack1[0]);
            var resolvePriority = opStack1.Min(o => o.pa.priority);
            for (int ii = 0; ii < opStack1.Count; ++ii) {
                var op = opStack1[ii];
                if (op.pa.priority == resolvePriority) {
                    var arg1 = varStack2.Pop();
                    var arg2 = varStack1[ii + 1];
                    varStack2.Push(
                        new AST.MethodInvoke(arg1.Position.Merge(arg2.Position), op.loc, op.pa.sig.Call(null), arg1, arg2) 
                            {Type = AST.MethodInvoke.InvokeType.PostAggregate });
                } else {
                    varStack2.Push(varStack1[ii + 1]);
                    opStack2.Push(op);
                }
            }
            (varStack1, varStack2) = (varStack2, varStack1);
            (opStack1, opStack2) = (opStack2, opStack1);
        }
        return varStack1.Pop();
    }
    
    public static InvokedMethod? TryGetSignature<T>(string member) =>
        TryGetSignature(member, typeof(T));

    public static InvokedMethod? TryGetSignature(string member, Type rt) {
        var res = ASTTryLookForMethod(rt, member);
        return res?.Call(member);
    }

    private static IAST? ReflectMethod(SMParser.ParsedUnit.Str member, Type rt, IParseQueue q) {
        if (TryGetSignature(member.Item, rt) is { } sig) {
            q.Advance();
            var fill = FillASTArray(sig, q);
            return AST.Failure.MaybeEnclose(InvokedMethodToAST(
                sig, 
                member.Position.Merge(fill.ArgRange ?? member.Position), 
                member.Position, fill.ASTs, fill.Parenthesized), fill.Error);
        }
        return null;
    }
    
    private static IAST InvokedMethodToAST(InvokedMethod inv, PositionRange pos, PositionRange callPos, IAST[] arguments, bool parenthesized) {
        if (inv is LiftedInvokedMethod) {
            var ityp = inv.GetType();
            if (ityp.IsGenericType) {
                return (IAST)liftedInvokedMethodToAST.Specialize(ityp.GetGenericArguments())
                    .Invoke(inv, pos, callPos, arguments, parenthesized)!;
            }
        }
        return new AST.MethodInvoke(pos, callPos, inv, arguments) { Parenthesized = parenthesized };
    }

    public static IAST LiftedInvokedMethodToAST<T,R>(LiftedInvokedMethod<T, R> inv, PositionRange pos, PositionRange callPos, IAST[] arguments, bool parenthesized) =>
        new AST.FuncedMethodInvoke<T, R>(pos, callPos, inv, arguments) { Parenthesized = parenthesized };
    private static readonly GenericMethodSignature liftedInvokedMethodToAST = (GenericMethodSignature)
        MethodSignature.Get(typeof(Reflector).GetMethod(nameof(LiftedInvokedMethodToAST))!);

    #endregion
}
}