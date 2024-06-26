﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BagoumLib.Expressions;
using BagoumLib.Reflection;
using Danmokou.Core;
using Danmokou.Danmaku;
using Danmokou.DMath;
using Danmokou.DMath.Functions;
using Danmokou.Reflection;
using Danmokou.Reflection2;
using JetBrains.Annotations;
using UnityEngine;
using Ex = System.Linq.Expressions.Expression;
using static Danmokou.Expressions.ExUtils;
using AST = Danmokou.Reflection2.AST;

namespace Danmokou.Expressions {

/// <summary>
/// An arbitrary set of arguments to an expression function.
/// <br/>Expression functions are written in the general form Func&lt;TExArgCtx, TEx&lt;R&gt;&gt;
///  and compiled to Func&lt;T1, T2..., R&gt;, where T1,T2... are types that have stored their information
///  in TExArgCtx (usually <see cref="ParametricInfo"/> or <see cref="BulletManager.SimpleBullet"/>),
///  and R is some standard return type like float or Vector2.
/// </summary>
public class TExArgCtx {
    /// <summary>
    /// Context that is shared by any copies of this.
    /// </summary>
    public class RootCtx {
        /// <summary>
        /// The lexical scope in which this expression is being compiled.
        /// </summary>
        public LexicalScope Scope => LexicalScope.CurrentOpenParsingScope;
        
        /// <summary>
        /// When the type of the custom data (<see cref="PIData"/>) is known, this contains
        ///  the type, as well as a function to get the custom data downcast to that type.
        /// </summary>
        public (Type type, Func<TExArgCtx, Ex> bpiAsType)? CustomDataType { get; set; }
        public Dictionary<string, Stack<Ex>> AliasStack { get; } =
            new();
        public readonly Dictionary<(string, Type), (ParameterExpression, ParameterExpression, ParameterExpression)>
            UnscopedEnvframeAcess = new();

        private static uint nextCtxIndex = 0;
        public uint CtxIndex { get; } = nextCtxIndex++;
        private uint suffixNum = 0;
        public string NameWithSuffix(string s) => $"{s}CG{CtxIndex}_{suffixNum++}";
       
#if EXBAKE_SAVE || EXBAKE_LOAD
        private uint proxyArgNum = 0;
        public string NextProxyArg() => $"proxy{proxyArgNum++}";
#endif
#if EXBAKE_SAVE
        private Ex args = Ex.Variable(typeof(object[]), "args");
        public List<string> HoistedVariables { get; } = new List<string>();
        public Dictionary<Expression, Expression> HoistedReplacements { get; } = new();
        public Dictionary<object, Expression> HoistedConstants { get; } = new();
        public List<(Type, string)> ProxyTypes { get; } = new();
#elif EXBAKE_LOAD
        public List<object> ProxyArguments { get; } = new List<object>();
#endif
        public bool CompileToField { get; set; } = false;
        public AST.ScriptFunctionDef? ScriptFunctionDef { get; set; }

        /// <summary>
        /// Handle baking/loading an argument to an expression-reflected function that is not itself an expression
        ///  and cannot be trivially converted into a code representation.
        /// <br/>Returns the argument for chaining convenience.
        /// </summary>
        public T Proxy<T>(T replacee) {
            Proxy(replacee, typeof(T));
            return replacee;
        }

        public object Proxy(object replacee, Type t) {
#if EXBAKE_SAVE
            var name = NextProxyArg();
            HoistedConstants[replacee] = args.Index(Ex.Constant(ProxyTypes.Count)).Cast(t);  
            ProxyTypes.Add((t, name));
#elif EXBAKE_LOAD
            ProxyArguments.Add(replacee);
#endif
            return replacee;
        }
    }

    /// <inheritdoc cref="RootCtx.Proxy{T}"/>
    public T Proxy<T>(T replacee) => Ctx.Proxy(replacee);
    
    /// <inheritdoc cref="RootCtx.Proxy{T}"/>
    public object Proxy(object replacee) => Ctx.Proxy(replacee, replacee.GetType());
    public readonly struct Arg {
        public readonly string name;
        //typeof(TExPI)
        public readonly Type texType;
        public readonly TEx expr;
        public readonly bool hasTypePriority;

        private Arg(string name, Type texType, TEx expr, bool hasTypePriority) {
            this.name = name;
            this.texType = texType;
            this.expr = expr;
            this.hasTypePriority = hasTypePriority;
        }

        public static Arg MakeFromTEx(string name, TEx expr, bool hasTypePriority) =>
            new(name, expr.GetType(), expr, hasTypePriority);

        //t = typeof(float) or similar
        public static Arg Make<T>(string name, bool hasTypePriority, bool isRef = false) {
            var expr = TEx.MakeParameter<T>(isRef, name);
            return MakeFromTEx(name, expr, hasTypePriority);
        }

        public static Arg MakeAny(Type t, string name, bool hasTypePriority, bool isRef = false) =>
            (Arg)makeArg.Specialize(t).Invoke(null, new object[] { name, hasTypePriority, isRef })!;
        public static Arg MakeBPI => Arg.Make<ParametricInfo>("bpi", true);

        private static readonly GenericMethodSignature makeArg = (GenericMethodSignature)
            MethodSignature.Get(typeof(Arg).GetMethod(nameof(Make))!);
        
    }
    
    public class LocalLet : IDisposable {
        private readonly string alias;
        private readonly TExArgCtx ctx;

        public LocalLet(TExArgCtx ctx, string alias, Ex val) {
            this.alias = alias;
            (this.ctx = ctx).Ctx.AliasStack.Push(alias, val);
        }

        public void Dispose() {
            ctx.Ctx.AliasStack.Pop(alias);
        }
    }

    public LocalLet Let(string alias, Ex val) => new(this, alias, val);
    
    private readonly Arg[] args;
    public IEnumerable<Ex> Expressions => args.Select(a => (Ex)a.expr);
    private readonly Dictionary<string, int> argNameToIndexMap;
    //Maps typeof(TExPI) to index
    private readonly Dictionary<Type, int> argExTypeToIndexMap;
    //Maps typeof(ParametricInfo) to index
    private readonly Dictionary<Type, int> argTypeToIndexMap;

    private readonly RootCtx? ctx;
    private readonly TExArgCtx? parent;
    public RootCtx Ctx => ctx ?? parent?.Ctx ?? throw new StaticException("No RootCtx found");
    private TExPI? _bpi;
    public TExPI BPI => MaybeBPI ?? throw new CompileException(
        "You are refencing fields on ParametricInfo, but no variable with this type is provided. This is most likely " +
        "because you need to use `Wrap` to make a GCXF<T> instead of a compile-time function call.");
    public TExPI? MaybeBPI => _bpi ??= MaybeGetByExprType<TExPI>(out _);
    public Ex FCTX => BPI.FiringCtx;

    public UnaryExpression findex => BPI.findex;
    public MemberExpression id => BPI.id;
    public MemberExpression index => BPI.index;
    public MemberExpression LocV2 => BPI.locV2;
    public MemberExpression LocV3 => BPI.locV3;
    public MemberExpression locx => BPI.locx;
    public MemberExpression locy => BPI.locy;
    public MemberExpression locz => BPI.locz;
    public Ex t => BPI.t;
    public TEx<float> FloatVal => GetByExprType<TEx<float>>();
    public TExSB SB => GetByExprType<TExSB>();
    public TExSB? MaybeSB => MaybeGetByExprType<TExSB>(out _);
    public TExGCX GCX => GetByExprType<TExGCX>();
    public TEx EnvFrame => GetByType<EnvFrame>();

    public TExArgCtx(params Arg[] args) : this(null, args) { }
    public TExArgCtx(TExArgCtx? parent, params Arg[] args) {
        this.parent = parent;
        if (parent == null)
            this.ctx = new RootCtx();
        this.args = args;
        argNameToIndexMap = new Dictionary<string, int>();
        argTypeToIndexMap = new Dictionary<Type, int>();
        argExTypeToIndexMap = new Dictionary<Type, int>();
        for (int ii = 0; ii < args.Length; ++ii) {
            if (argNameToIndexMap.ContainsKey(args[ii].name)) {
                throw new CompileException($"Duplicate argument name: {args[ii].name}");
            }
            argNameToIndexMap[args[ii].name] = ii;
            
            if (!argTypeToIndexMap.TryGetValue(args[ii].expr.type, out var i)
                || !args[i].hasTypePriority
                || args[ii].hasTypePriority) {
                argTypeToIndexMap[args[ii].expr.type] = ii;
            }
            if (!argExTypeToIndexMap.TryGetValue(args[ii].texType, out i)
                || !args[i].hasTypePriority
                || args[ii].hasTypePriority) {
                argExTypeToIndexMap[args[ii].texType] = ii;
            }
        }
    }

    public TEx<T> GetByName<T>(string name) {
        if (!argNameToIndexMap.TryGetValue(name, out var idx))
            throw new CompileException($"The variable \"{name}\" is not provided as an argument.");
        return args[idx].expr as TEx<T> ?? throw new BadTypeException($"The variable \"{name}\" (#{idx+1}/{args.Length}) is not of type {typeof(T).SimpRName()}");
    }
    public TEx<T>? MaybeGetByName<T>(string name) {
        if (!argNameToIndexMap.TryGetValue(name, out var idx))
            return null;
        return args[idx].expr is TEx<T> arg ?
            arg :
            //Still throw an error in this case
            throw new BadTypeException($"The variable \"{name}\" (#{idx+1}/{args.Length}) is not of type {typeof(T).SimpRName()}");
    }
    public TEx GetByName(Type typ, string name) {
        if (!argNameToIndexMap.TryGetValue(name, out var idx))
            throw new CompileException($"The variable \"{name}\" is not provided as an argument.");
        return TEx.TExTypeMatches(typ, args[idx].expr.GetType()) ?
                args[idx].expr :
                throw new BadTypeException($"The variable \"{name}\" (#{idx+1}/{args.Length}) is not of type {typ.SimpRName()}");
    }
    
    public TEx? MaybeGetByName(Type typ, string name) {
        if (!argNameToIndexMap.TryGetValue(name, out var idx))
            return null;
        return TEx.TExTypeMatches(typ, args[idx].expr.GetType()) ?
            args[idx].expr :
            //Still throw an error in this case
            throw new BadTypeException($"The variable \"{name}\" (#{idx+1}/{args.Length}) is not of type {typ.SimpRName()}");
    }
    
    public TEx GetByType<T>(out int idx) {
        if (!argTypeToIndexMap.TryGetValue(typeof(T), out idx))
            throw new CompileException($"No variable of type {typeof(T).SimpRName()} is provided as an argument.");
        return args[idx].expr;
    }
    public TEx GetByType<T>() => GetByType<T>(out _);
    public TEx? MaybeGetByType<T>(out int idx) => 
        argTypeToIndexMap.TryGetValue(typeof(T), out idx) ? 
            args[idx].expr : 
            null;
    
    public Tx GetByExprType<Tx>(out int idx) where Tx : TEx {
        if (!argExTypeToIndexMap.TryGetValue(typeof(Tx), out idx))
            throw new CompileException($"No variable of type {typeof(Tx).SimpRName()} is provided as an argument.");
        return (Tx)args[idx].expr;
    }
    public Tx GetByExprType<Tx>() where Tx : TEx => GetByExprType<Tx>(out _);
    public Tx? MaybeGetByExprType<Tx>(out int idx) where Tx : TEx => 
        argExTypeToIndexMap.TryGetValue(typeof(Tx), out idx) ? 
            (Tx) args[idx].expr : 
            null;

    /*private Type GetTExInnerType(Type tx) {
        
    }*/
    
    public TExArgCtx Rehash() {
        var bpi = GetByExprType<TExPI>(out var bidx);
        return MakeCopyWith(bidx, Arg.MakeFromTEx(args[bidx].name, new TExPI(bpi.Rehash()), args[bidx].hasTypePriority));
    }
    public TExArgCtx CopyWithT(Ex newT) {
        var bpi = GetByExprType<TExPI>(out var bidx);
        return MakeCopyWith(bidx, Arg.MakeFromTEx(args[bidx].name, new TExPI(bpi.CopyWithT(newT)), args[bidx].hasTypePriority));
    }

    private TExArgCtx MakeCopyWith(int idx, Arg newArg) {
        var newargs = args.ToArray();
        newargs[idx] = newArg;
        return new TExArgCtx(this, newargs);
    }

    public TExArgCtx MakeCopyForType<T>(out TEx<T> currEx, out TEx<T> copyEx)  {
        currEx = (Ex)GetByType<T>(out int idx);
        copyEx = new TEx<T>();
        return MakeCopyWith(idx, Arg.MakeFromTEx(args[idx].name, copyEx, args[idx].hasTypePriority));
    }
    
    public TExArgCtx MakeCopyForType<T>(TEx<T> newEx) {
        _ = GetByType<T>(out int idx);
        return MakeCopyWith(idx, Arg.MakeFromTEx(args[idx].name, newEx, args[idx].hasTypePriority));
    }
    
    public TExArgCtx MakeCopyForExType<T>(out T currEx, out T copyEx) where T: TEx, new() {
        currEx = GetByExprType<T>(out int idx);
        copyEx = new T();
        return MakeCopyWith(idx, Arg.MakeFromTEx(args[idx].name, copyEx, args[idx].hasTypePriority));
    }
    
    public TExArgCtx MakeCopyForExType<T>(T newEx) where T: TEx {
        _ = GetByExprType<T>(out int idx);
        return MakeCopyWith(idx, Arg.MakeFromTEx(args[idx].name, newEx, args[idx].hasTypePriority));
    }

    public TExArgCtx Append(string name, TEx ex, bool hasPriority=true) {
        var newArgs = args.Append(Arg.MakeFromTEx(name, ex, hasPriority)).ToArray();
        return new TExArgCtx(this, newArgs);
    }
    public TExArgCtx AppendSB(string name, TExSB ex, bool hasPriority=true) {
        var nargs = args.Append(Arg.MakeFromTEx(name, ex, hasPriority));
        if (MaybeGetByExprType<TExPI>(out _) == null) nargs = nargs.Append(Arg.MakeFromTEx(name + "_bpi", ex.bpi, true));
        return new TExArgCtx(this, nargs.ToArray());
    }
    
    public Ex When(Func<TExArgCtx, TEx<bool>> pred, Ex then) => Ex.IfThen(pred(this), then);

    //Methods for dynamic (dict-based) data lookup
    public Ex DynamicHas<T>(string key) => PIData.ContainsDynamic<T>(this, key);
    public Ex DynamicGet<T>(string key) => PIData.GetValueDynamic<T>(this, key);
    public Ex DynamicSet<T>(string key, Ex val) => PIData.SetValueDynamic<T>(this, key, val);
}

/// <summary>
/// Base class for <see cref="TEx{T}"/> used for type constraints.
/// </summary>
public class TEx {
    internal readonly Expression ex;
    public readonly Type type;
    protected TEx(Expression ex) {
        this.ex = ex;
        this.type = ex.Type;
    }
    private static readonly IReadOnlyDictionary<Type, Type> TExBoxMap = new Dictionary<Type, Type> {
        { typeof(Vector2), typeof(TExV2) },
        { typeof(Vector3), typeof(TExV3) },
        { typeof(ParametricInfo), typeof(TExPI) },
        { typeof(float), typeof(TEx<float>) },
        { typeof(V2RV2), typeof(TExRV2) },
    };
    private static readonly Type TypeTExT = typeof(TEx<>);
    
    public static TEx Box(Expression ex) {
        var ext = ex.Type;
        if (!TExBoxMap.TryGetValue(ext, out var tt)) throw new Exception($"Cannot box expression of type {ext}");
        return Activator.CreateInstance(tt, ex) as TEx ?? throw new Exception("Boxing failed");
    }

    public static bool TExTypeMatches(Type basicType, Type texType) {
        if (texType.IsGenericType && texType.GetGenericTypeDefinition() == typeof(TEx<>))
            return texType.GetGenericArguments()[0] == basicType;
        else if (texType.IsSubclassOf(typeof(TEx)))
            return TExTypeMatches(basicType, texType.BaseType!);
        return false;
    }

    //t = typeof(float) or similr
    public static TEx MakeParameter<T>(bool isRef, string name) {
        var t = typeof(T);
        var rt = (isRef) ? t.MakeByRefType() : t;
        var ex = Expression.Parameter(rt, name);
        return _MakeSpecialParameter(t, ex) ?? new TEx<T>(ex);
    }
    private static TEx? _MakeSpecialParameter(Type t, ParameterExpression ex) {
        if (t == tv2)
            return new TExV2(ex);
        else if (t == tv3)
            return new TExV3(ex);
        else if (t == typeof(ParametricInfo))
            return new TExPI(ex);
        else if (t == typeof(BulletManager.SimpleBullet))
            return new TExSB(ex);
        else if (t == typeof(V2RV2))
            return new TExRV2(ex);
        else if (t == typeof(BulletManager.SimpleBulletCollection))
            return new TExSBC(ex);
        else if (t == typeof(BulletManager.SimpleBulletCollection.VelocityUpdateState))
            return new TExSBCUpdater(ex);
        else if (t == typeof(Movement))
            return new TExMov(ex);
        else if (t == typeof(LaserMovement))
            return new TExLMov(ex);
        else if (t == typeof(GenCtx))
            return new TExGCX(ex);
        else
            return null;
    }
    

    protected TEx(ExMode mode, Type t, string? name) {
        if (mode == ExMode.RefParameter) {
            t = t.MakeByRefType();
        }
        ex = name == null ? Expression.Parameter(t) : Expression.Parameter(t, name);
        this.type = ex.Type;
    }
    public static implicit operator TEx(Expression ex) {
        return new(ex);
    }
    public static implicit operator Expression(TEx me) {
        return me.ex;
    }
    public static implicit operator ParameterExpression(TEx me) {
        return (ParameterExpression)me.ex;
    }

    public struct ResolveArg {
        public readonly Ex ex;
        public readonly bool reqCopy;
        public readonly string? name;
        
        public ResolveArg(Ex ex, bool reqCopy, string? name = null) {
            this.ex = ex;
            this.reqCopy = reqCopy;
            this.name = name;
        }
    
        public static implicit operator ResolveArg(TEx exx) => new(exx.ex, RequiresCopyOnRepeat(exx.ex));
        public static implicit operator ResolveArg(Ex exx) => new(exx, RequiresCopyOnRepeat(exx));
    }
    
    private static Ex ResolveCopy(Func<Ex[], Ex> func, params ResolveArg[] args) {
        var newvars = ListCache<ParameterExpression>.Get();
        var setters = ListCache<Expression>.Get();
        var usevars = new Expression[args.Length];
        for (int ii = 0; ii < args.Length; ++ii) {
            if (args[ii].reqCopy) {
                //Don't name this, as nested TEx should not overlap
                var copy = V(args[ii].ex.Type, args[ii].name);
                usevars[ii] = copy;
                newvars.Add(copy);
                setters.Add(copy.Is(args[ii].ex));
            } else {
                usevars[ii] = args[ii].ex;
            }
        }
        setters.Add(func(usevars));
        var block = Ex.Block(newvars, setters);
        ListCache<ParameterExpression>.Consign(newvars);
        ListCache<Expression>.Consign(setters);
        return block;
    }

    private static Ex ResolveFieldsMaybeDeconstructNew(Func<Ex[], Ex> func, ResolveArg arg, bool singleUse, params string[] fields) {
        if (!arg.reqCopy)
            return func(fields.Select(f => arg.ex.Field(f)).ToArray());
        if (arg.ex is NewExpression newe)
            return singleUse ?
                func(newe.Arguments.ToArray()) :
                ResolveCopy(func, newe.Arguments.Select((x, i) => new ResolveArg(x, RequiresCopyOnRepeat(x), 
                    $"{(x as ParameterExpression)?.Name ?? "anon"}_{fields[i]}")).ToArray());
        if (IsBlockWithLastNew(arg.ex)) {
            var bex = FlattenNestedBlock((BlockExpression)arg.ex);
            return Ex.Block(bex.Variables, bex.Expressions.Take(bex.Expressions.Count - 1).Append(
                ResolveFieldsMaybeDeconstructNew(func, new(bex.Expressions[^1], true), singleUse, fields)));
        }

        var let = V(arg.ex.Type);
        return Ex.Block(new[] { let }, let.Is(arg.ex), func(fields.Select(f => let.Field(f)).ToArray()));
    }

    private static bool IsBlockWithLastNew(Ex ex) {
        while (ex is BlockExpression bex) {
            ex = bex.Expressions[^1];
            if (ex is NewExpression) return true;
        }
        return false;
    }

    private static BlockExpression FlattenNestedBlock(BlockExpression bex) {
        if (bex.Expressions[^1] is not BlockExpression rbex)
            return bex;
        rbex = FlattenNestedBlock(rbex);
        return Ex.Block(bex.Variables.Concat(rbex.Variables),
            bex.Expressions.Take(bex.Expressions.Count - 1).Concat(rbex.Expressions));
    }

    /// <summary>
    /// Feed the X,Y components of a V2 into a resolver.
    /// <br/>If the V2 is a `new Vector2` expression,
    ///  then skips the constructor and resolves its arguments directly.
    /// <br/>If singleUse is set to true and the V2 is a `new Vector2` expression,
    ///  then provide the constructor arguments directly to the resolver without copying.
    /// </summary>
    public static Ex ResolveV2AsXY(TEx<Vector2> v2, Func<TEx<float>, TEx<float>, Ex> resolver, bool singleUse = false) =>
        ResolveFieldsMaybeDeconstructNew(x => resolver(x[0], x[1]), v2, singleUse, "x", "y");
    
    public static Ex ResolveF(TEx<float> t1, Func<TEx<float>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0]), t1);
    public static Ex Resolve<T1>(TEx<T1> t1, Func<TEx<T1>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0]), t1);
    public static Ex ResolveV2(TEx<Vector2> t1, Func<TExV2, Ex> resolver) =>
        ResolveCopy(x => resolver(new TExV2(x[0])), t1);
    public static Ex ResolveV3(TEx<Vector3> t1, Func<TExV3, Ex> resolver) =>
        ResolveCopy(x => resolver(new TExV3(x[0])), t1);
    public static Ex Resolve<T1,T2>(TEx<T1> t1, TEx<T2> t2, Func<TEx<T1>, TEx<T2>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0], x[1]), t1, t2);
    public static Ex ResolveV2(TEx<Vector2> t1, TEx<Vector2> t2, 
        Func<TExV2, TExV2, Ex> resolver) =>
        ResolveCopy(x => resolver(new TExV2(x[0]), new TExV2(x[1])), t1, t2);
    
    /// <inheritdoc cref="Resolve{T1,T2,T3}"/>
    public static Ex ResolveV3(TEx<Vector3> t1, TEx<Vector3> t2, 
        Func<TExV3, TExV3, Ex> resolver) =>
        ResolveCopy(x => resolver(new TExV3(x[0]), new TExV3(x[1])), t1, t2);
    /// <summary>
    /// Copy the provided expressions into temporary variables that can be reused without recalculating the expression.
    /// </summary>
    public static Ex Resolve<T1,T2,T3>(TEx<T1> t1, TEx<T2> t2, TEx<T3> t3, 
        Func<TEx<T1>, TEx<T2>, TEx<T3>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0], x[1], x[2]), t1, t2, t3);
    public static Ex ResolveV2(TEx<Vector2> t1, TEx<Vector2> t2, TEx<Vector2> t3, 
        Func<TExV2, TExV2, TExV2, Ex> resolver) =>
        ResolveCopy(x => resolver(new TExV2(x[0]), new TExV2(x[1]), new TExV2(x[2])), t1, t2, t3);
    public static Ex Resolve<T1,T2,T3,T4>(TEx<T1> t1, TEx<T2> t2, TEx<T3> t3, TEx<T4> t4, 
        Func<TEx<T1>, TEx<T2>, TEx<T3>, TEx<T4>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0], x[1], x[2], x[3]), t1, t2, t3, t4);
    public static Ex Resolve<T1,T2,T3,T4,T5>(TEx<T1> t1, TEx<T2> t2, TEx<T3> t3, TEx<T4> t4, TEx<T5> t5, 
        Func<TEx<T1>, TEx<T2>, TEx<T3>, TEx<T4>, TEx<T5>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0], x[1], x[2], x[3], x[4]), t1, t2, t3, t4, t5);
    
    public static Ex Resolve<T1,T2,T3,T4,T5,T6>(TEx<T1> t1, TEx<T2> t2, TEx<T3> t3, TEx<T4> t4, TEx<T5> t5, 
        TEx<T6> t6, Func<TEx<T1>, TEx<T2>, TEx<T3>, TEx<T4>, TEx<T5>, TEx<T6>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0], x[1], x[2], x[3], x[4], x[5]), t1, t2, t3, t4, t5, t6);
    public static Ex Resolve<T1,T2,T3,T4,T5,T6,T7>(TEx<T1> t1, TEx<T2> t2, TEx<T3> t3, TEx<T4> t4, TEx<T5> t5, 
        TEx<T6> t6, TEx<T7> t7, Func<TEx<T1>, TEx<T2>, TEx<T3>, TEx<T4>, TEx<T5>, TEx<T6>, TEx<T7>, Ex> resolver) =>
        ResolveCopy(x => resolver(x[0], x[1], x[2], x[3], x[4], x[5], x[6]), t1, t2, t3, t4, t5, t6, t7);
    
    public static bool RequiresCopyOnRepeat(Expression e) => !(
        e.NodeType == ExpressionType.Parameter ||
        e.NodeType == ExpressionType.Constant ||
        e.NodeType == ExpressionType.MemberAccess ||
        (e.NodeType == ExpressionType.Convert && !RequiresCopyOnRepeat((e as UnaryExpression)!.Operand)));
    
}
/// <summary>
/// A typed expression.
/// <br/>This typing is syntactic sugar: any expression, regardless of type, can be cast as eg. TEx{float}.
/// <br/>However, constructing a parameter expression via TEx{T} will type the expression appropriately.
/// By default, creates a ParameterExpression.
/// </summary>
/// <typeparam name="T">Type of expression eg(float).</typeparam>
public class TEx<T> : TEx {

    public TEx() : this(ExMode.Parameter, null) {}

    public TEx(Expression ex) : base(ex) { }

    public TEx(ExMode m, string? name) : base(m, typeof(T), name) {}
    
    public static implicit operator TEx<T>(Expression ex) {
        return new(ex);
    }

    public static implicit operator TEx<T>(T obj) => Expression.Constant(obj);
}
}