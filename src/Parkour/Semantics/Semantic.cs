namespace Parkour.Semantics;

public abstract class Semantic
{
    public Symbol.Type ResultType { get; }
    public ImmutableList<Diagnostic> Diagnostics { get; }
    public virtual Symbol? ReferencedSymbol => null;

    [Flags]
    internal protected enum ContainsState
    {
        None = 0,
        Unknowns = 2,
        Diagnostics = 4
    }

    internal protected ContainsState State { get; }

    /// <summary>
    /// This semantic or child semantics contains unknown/unbound elements.
    /// </summary>
    public bool ContainsUnknowns => (this.State & ContainsState.Unknowns) != 0;

    /// <summary>
    /// This semantic or child semantics contains diagnostics.
    /// </summary>
    public bool ContainsDiagnostics => (this.State & ContainsState.Diagnostics) != 0;

    /// <summary>
    /// This semantic has diagnostics
    /// </summary>
    public bool HasDiagnostics => this.Diagnostics.Count > 0;

    private protected Semantic(ContainsState state, Symbol.Type? resultType, ImmutableList<Diagnostic>? diagnostics)
    {
        this.State = state;
        this.ResultType = resultType ?? SymbolModel.Unknown;
        this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;

        if (this.ResultType == SymbolModel.Unknown)
            this.State |= ContainsState.Unknowns;

        if (this.Diagnostics.Count > 0)
            this.State |= ContainsState.Diagnostics;
    }

    public string ToText() =>
        new SemanticWriter().WriteExpression(this);

    public ImmutableList<Diagnostic> GetDiagnostics() =>
        this.SelectWhere(s => s.HasDiagnostics, s => s.Diagnostics).SelectMany(dx => dx).ToImmutableList();

    private static ContainsState CombineState(IEnumerable<Semantic> items) =>
        items.Aggregate(ContainsState.None, (s, e) => s | e.State);

    public sealed class Block : Semantic
    {
        public ImmutableList<Semantic> Expressions { get; }

        public Block(
            ImmutableList<Semantic> expressions, 
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(
                  CombineState(expressions), 
                  expressions.Count > 0 ? expressions[^1].ResultType : SymbolModel.Void, 
                  diagnostics)
        {
            this.Expressions = expressions.ToImmutableList();
        }
    }

    public sealed class Branch : Semantic
    {
        public string TargetName { get; }
        public Symbol.Target? Target { get; }
        public Semantic? Expression { get; }

        public Branch(
            string targetName, 
            Semantic? expression, 
            Symbol.Target? target, 
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(
                  expression != null ? expression.State : ContainsState.None, 
                  expression != null ? expression.ResultType : SymbolModel.Void, 
                  diagnostics)
        {
            this.TargetName = targetName;
            this.Target = target;
            this.Expression = expression;
        }

        public bool IsBreak => this.TargetName == "break";
        public bool IsContinue => this.TargetName == "continue";
        public bool IsReturn => this.TargetName == "return";
        public bool IsGoto => !IsBreak && !IsContinue && !IsReturn;

        public static Branch CreateBreak(Semantic? expression = null) =>
            new Branch("break", expression, null);

        public static Branch CreateContinue() =>
            new Branch("continue", null, null);

        public static Branch CreateReturn(Semantic? expression = null) =>
            new Branch("return", expression, null);
    }

    public class Label : Semantic
    {
        public string Name { get; }
        public Symbol.Target? Target { get; }

        public Label(
            string name, 
            Symbol.Target? target, 
            Symbol.Type? resultType)
            : base(ContainsState.None, resultType ?? target?.Type, null)
        {
            this.Name = name;
            this.Target = target;
        }
    }

    public sealed class Call : Semantic
    {
        public Semantic Expression { get; }
        public ImmutableList<Semantic> Arguments { get; }
        public Symbol? CalledSymbol { get; }

        public Call(
            Semantic expression, 
            ImmutableList<Semantic> arguments, 
            Symbol? symbol, 
            Symbol.Type? resultType, 
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(
                  expression.State | arguments.Aggregate(ContainsState.None, (s, e) => e.State | s), 
                  resultType, 
                  diagnostics)
        {
            this.Expression = expression;
            this.Arguments = arguments.ToImmutableList();
            this.CalledSymbol = symbol;
        }
    }

    public sealed class Condition : Semantic
    {
        public Semantic Test { get; }
        public Semantic WhenTrue { get; }
        public Semantic WhenFalse { get; }

        public Condition(
            Semantic test, 
            Semantic whenTrue, 
            Semantic whenFalse, 
            Symbol.Type? resultType,
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(
                  test.State | whenTrue.State | whenFalse.State, 
                  resultType,
                  diagnostics)
        {
            this.Test = test;
            this.WhenTrue = whenTrue;
            this.WhenFalse = whenFalse;
        }
    }

    public sealed class Constant : Semantic
    {
        public object? Value { get; }

        public Constant(
            object? value, 
            Symbol.Type? resultType,
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(ContainsState.None, resultType, diagnostics)
        {
            this.Value = value;
        }
    }

    public enum ConversionKind
    {
        Narrowing,
        Widening
    }

    public sealed class Convert : Semantic
    {
        public ConversionKind Kind { get; }
        public Semantic Expression { get; }
        public Symbol.Type ConvertedType { get; }
        public Symbol? Operator { get;}

        public Convert(
            ConversionKind kind, 
            Semantic expression, 
            Symbol.Type convertedType, 
            Symbol? @operator = null, 
            Symbol.Type? resultType = null, 
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(expression.State, resultType ?? convertedType, diagnostics)
        {
            this.Kind = kind;
            this.Expression = expression;
            this.ConvertedType = convertedType;
            this.Operator = @operator;
        }
    }

    public sealed class Declaration : Semantic
    {
        public string Name { get; }
        public Semantic Initializer { get; }
        public Symbol.Variable? Variable { get; }

        public Declaration(
            string name, 
            Semantic initializer, 
            Symbol.Variable? variable, 
            Symbol.Type? resultType, 
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(initializer.State, resultType , diagnostics)
        {
            this.Name = name;
            this.Initializer = initializer;
            this.Variable = variable;
        }
    }

    public sealed class Function : Semantic
    {
        public string Name { get; }
        public ImmutableList<Parameter> Parameters { get; }
        public Semantic Body { get; }
        public Symbol.Function? Symbol { get; }
        public Symbol.Type? ReturnType { get; }
        public Symbol.Target? ReturnTarget { get; }

        public Function(
            string name,
            ImmutableList<Parameter> parameters, 
            Semantic body, 
            Symbol.Type? returnType,
            Symbol.Function? symbol,
            Symbol.Target? returnTarget,
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(body.State, symbol, diagnostics)
        {
            this.Name = name;
            this.Parameters = parameters;
            this.Body = body;
            this.Symbol = symbol;
            this.ReturnType = returnType;
            this.ReturnTarget = returnTarget;
        }
    }

    public sealed class Parameter
    {
        public string Name { get; }
        public Symbol.Type ParameterType { get; }

        public Parameter(string name, Symbol.Type? parameterType)
        {
            this.Name = name;
            this.ParameterType = parameterType ?? SymbolModel.Any;
        }
    }

    public sealed class Path : Semantic
    {
        public Semantic Expression { get; }
        public new Reference Reference { get; }

        public Path(
            Semantic expression, 
            Reference reference,
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(expression.State | reference.State, reference.ResultType, diagnostics)
        {
            this.Expression = expression;
            this.Reference = reference;
        }
    }

    public sealed class Reference : Semantic
    {
        public string Name { get; }
        public override Symbol? ReferencedSymbol { get; }

        public Reference(
            string name, 
            Symbol? referencedSymbol, 
            Symbol.Type? resultType, 
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(
                ContainsState.None,
                resultType, 
                diagnostics)
        {
            this.Name = name;
            this.ReferencedSymbol = referencedSymbol;
        }
    }

    public abstract class MemberDeclaration : Semantic
    {
        public string Name { get; }
        public SymbolAccess Access { get; }
        public SymbolModifier Modifiers { get; }

        protected MemberDeclaration(
            ContainsState state,
            string name,
            SymbolAccess access,
            SymbolModifier modifiers,
            Symbol.Type? resultType,
            ImmutableList<Diagnostic>? diagnostics)
            : base(state, resultType ?? SymbolModel.Void, diagnostics)
        {
            this.Name = name;
            this.Access = access;
            this.Modifiers = modifiers;
        }
    }

    public sealed class Class : MemberDeclaration
    {
        public ImmutableList<MemberDeclaration> Declarations { get; }
        public Symbol.Type Symbol { get; }

        public Class(
            string name,
            SymbolAccess access,
            SymbolModifier modifiers,
            ImmutableList<MemberDeclaration>? declarations,
            Symbol.Function? symbol,
            ImmutableList<Diagnostic>? diagnostics = null)
        : base(
              declarations != null ? CombineState(declarations) : ContainsState.None,
              name,
              access,
              modifiers,
              symbol,
              diagnostics)
        {
            this.Declarations = declarations ?? ImmutableList<MemberDeclaration>.Empty;
            this.Symbol = symbol ?? SymbolModel.Unknown;
        }
    }

    public sealed class FieldDeclaration : MemberDeclaration
    {
        public Semantic? Initializer { get; }
        public Symbol.Type? FieldType { get; }

        public FieldDeclaration(
            string name, 
            SymbolAccess access, 
            SymbolModifier modifiers, 
            Semantic? initializer,
            Symbol.Type? fieldType, 
            ImmutableList<Diagnostic>? diagnostics = null)
        : base(
              initializer != null ? initializer.State : ContainsState.None,
              name,
              access, 
              modifiers,
              SymbolModel.Void,
              diagnostics)
        {
            this.Initializer = initializer;
            this.FieldType = fieldType;
        }
    }

    public sealed class While : Semantic
    {
        public Semantic Test { get; }
        public Semantic Body { get; }
        public Symbol.Target? BreakTarget { get; }
        public Symbol.Target? ContinueTarget { get; }

        public While(
            Semantic test, 
            Semantic body, 
            Symbol.Type? resultType, 
            Symbol.Target? breakTarget,
            Symbol.Target? continueTarget,
            ImmutableList<Diagnostic>? diagnostics = null)
            : base(test.State | body.State, resultType, diagnostics)
        {
            this.Test = test;
            this.Body = body;
            this.BreakTarget = breakTarget;
            this.ContinueTarget = continueTarget;
        }
    }

    public sealed class Void : Semantic
    {
        private Void() : base(ContainsState.None, SymbolModel.Void, null) { }
        public static Void Instance = new Void();
    }
}
