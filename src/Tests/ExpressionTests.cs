using Parkour;
using Parkour.Semantics;
using Parkour.Binding;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class ExpressionTests
{
    private readonly SymbolCache _symbols;
    private readonly BindingScope _defaultTestScope;

    public ExpressionTests()
    {       
        _symbols = RuntimeSymbols.GetOrCreateCache();
        _defaultTestScope = CreateBindingScope(_symbols);
    }

    public static BindingScope CreateBindingScope(SymbolCache symbols)
    {
        return BindingScope.Default.AddSymbolMembers(
            new[]
            {
                symbols.GlobalNamespace,
                symbols.System
            });
    }

    [TestMethod]
    public void TestAssign()
    {
        TestBinding(
            Block(
                Variable(Type(_symbols.Int32), "x"),
                Assign(Name("x"), Constant(1))),
            _symbols.Int32
            );
    }

    [TestMethod]
    public void TestBlock()
    {
        // block can contain just one expression
        TestBinding(
            Block(Constant(1)), 
            _symbols.Int32);

        // last expression determines block type
        TestBinding(
            Block(
                Constant("zero"),
                Constant(1)),
            _symbols.Int32);


        // declare in block
        TestBinding(
            Block(
                Variable("x", Constant(1)),
                Name("x")),
            _symbols.Int32);
    }

    [TestMethod]
    public void TestBranch()
    {
        // branch w/o expression to label w/o type
        TestBinding(
            Block(
                Condition(
                    Constant(true),
                    Branch("label")),
                Constant("3"),
                Label("label")
                ));

        // branch with expression & label with matching type
        TestBinding(
            Block(
                Condition(
                    Constant(true),
                    Branch("label", Constant(1))),
                Constant(2),
                Label("label", Type(_symbols.Int32))
                ));

        // branch with expression & label with convertable type
        TestBinding(
            Block(
                Condition(
                    Constant(true),
                    Branch("label", Constant(1))),
                Constant(2),
                Label("label", Type(_symbols.Int64))
                ));

        // branch with expression & label with non-convertable type
        TestBinding(
            Block(
                Condition(
                    Constant(true),
                    Branch("label", Constant(1))),
                Constant(2),
                Label("label", Type(_symbols.String))
                ),
                containsDiagnostics: true);

        // infinite loop
        TestBinding(
            Block(
                Label("label"),
                Condition(
                    Constant(true),
                    Branch("label")),
                Constant("3")
                ));

        // branch alone?  this is error, but check result type
        TestBinding(
            Branch("label"),
            _symbols.DoesNotReturn,
            containsDiagnostics: true);

        // branch at end of block to valid target
        TestBinding(
            Block(
                Label("label"),
                Branch("label")),
            _symbols.DoesNotReturn);
    }

    [TestMethod]
    public void TestCall()
    {
        TestBinding(Call(Constant(1).Member("ToString")), _symbols.String);
    }

    [TestMethod]
    public void TestCondition()
    {
        // whenTrue/whenFalse same types
        TestBinding(
            Condition(Constant(true), Constant(1), Constant(0)),
            _symbols.Int32);

        // whenTrue/whenFalse not same types, convertable
        TestBinding(
            Condition(Constant(true), Constant(1), Constant(1L)),
            _symbols.Int64);

        // whenTrue / whenFalse not same types, not convertable
        TestBinding(
            Condition(Constant(true), Constant(1), Constant("zero")),
            _symbols.Object,
            containsDiagnostics: true);

        // whenTrue is void (void path leads to default)
        TestBinding(
            Condition(Constant(true), Void(), Constant(2)),
            _symbols.Int32);

        // whenFalse is void (void path leads to default)
        TestBinding(
            Condition(Constant(true), Constant(1), Void()),
            _symbols.Int32);

        // both are void
        TestBinding(
            Condition(Constant(true), Void(), Void()),
            _symbols.Void);
    }

    [TestMethod]
    public void TestConstant()
    {
        TestBinding(Constant(1), _symbols.Int32);
    }

    [TestMethod]
    public void TestConstructType()
    {
        var listT = (TypeSymbol?)_symbols.GlobalNamespace.GetFirstSymbolFromPath("System.Collections.Generic.List`1");
        Assert.IsNotNull(listT);
        var listInt32 = _symbols.GetOrConstruct(listT, [_symbols.Int32]);
        Assert.IsNotNull(listInt32);

        TestBinding(
            Construct(Type(listT), [Type(_symbols.Int32)]),
            expectedReferencedSymbol: listInt32
            );
    }

    [TestMethod]
    public void TestDefault()
    {
        // default w/ type expression
        TestBinding(
            Default(Type(_symbols.Int32)),
            _symbols.Int32);

        // default w/o type expression
        TestBinding(
            Default(),
            containsDiagnostics: true);

        // default w/o type but with target type
        TestBinding(
            Variable(Type(_symbols.Int32), "x", Default()),
            _symbols.Int32);
    }

    [TestMethod]
    public void TestLabels()
    {
        // lone void label okay
        TestBinding(
            Label("x"),
            expectedResultType: _symbols.Void);

        // lone void label in block okay
        TestBinding(
            Block(Label("x")),
            expectedResultType: _symbols.Void);

        // void label with inflowing value okay (can be ignored)
        TestBinding(
            Block(
                Constant(1),
                Label("x")),
            expectedResultType: _symbols.Void);

        // can flow compatible value type into label expecting a type
        TestBinding(
            Block(
                Constant(1),
                Label("x", Type(_symbols.Int32))
                ),
            expectedResultType: _symbols.Int32);

        // can flow compatible value type into label expecting a type
        TestBinding(
            Block(
                Constant(1),
                Label("x", Type(_symbols.Int64))
                ),
            expectedResultType: _symbols.Int64);

        // cannot flow void into label expecting type
        TestBinding(
            Label("x", Type(_symbols.Int32)),
            expectedResultType: _symbols.Int32,
            containsDiagnostics: true);

        // cannot flow incompatible value type into label expecting a type
        TestBinding(
            Block(
                Constant("one"),
                Label("x", Type(_symbols.Int32))
                ),
            expectedResultType: _symbols.Int32,
            containsDiagnostics: true);

        // TODO: should be able to branch with value to void label. though probably doing something wrong.
    }


    [TestMethod]
    public void TestLambda()
    {
        // lambda with no parameters and no returns
        TestBinding(
            Lambda(Constant(1)));

        // called lambda with no parameters and no return
        TestBinding(
            Call(Lambda(Constant(1))),
            expectedResultType: _symbols.Int32);

        // called lambda with no parameters and no return
        TestBinding(
            Call(Lambda(Void())),
            expectedResultType: _symbols.Void);

        // lambda with parameter
        TestBinding(
            Lambda(
                [Parameter("x", Type(_symbols.String))],
                Name("x")));

        // call lambda with parameter
        TestBinding(
            Call(
                Lambda(
                    [Parameter("x", Type(_symbols.String))],
                    Name("x")),
                Constant("string")),
            _symbols.String);

        // lambda with return
        TestBinding(
            Call(
                Lambda(
                    Return(Constant(1)))),
            _symbols.Int32);

        // lambda with return and value
        TestBinding(
            Call(
                Lambda(
                    Block(
                        Return(Constant(1)),
                        Constant(2L)
                        ))),
            _symbols.Int64);

        // lambda with conditional true and value
        TestBinding(
            Call(
                Lambda(
                    Condition(Constant(true), Return(Constant(1)), Constant(2L))
                    )),
            _symbols.Int64);

        // lambda with conditional returns
        TestBinding(
            Call(
                Lambda(
                    Condition(Constant(true), Return(Constant(1)), Return(Constant(2L)))
                    )),
            _symbols.Int64);

        // lambda with non-convertable returns
        TestBinding(
            Call(
                Lambda(
                    Condition(Constant(true), Return(Constant(1)), Return(Constant("two")))
                    )),
            _symbols.Object);
    }

    [TestMethod]
    public void TestLoop()
    {
        // loop with no break
        TestBinding(
            Loop(Constant(1)),
            expectedResultType: _symbols.Void);

        // loop with block & no break
        TestBinding(
            Loop(Block(Constant(1))),
            expectedResultType: _symbols.Void);

        // loop with break
        TestBinding(
            Loop(Break()),
            expectedResultType: _symbols.Void);

        // loop with break in block
        TestBinding(
            Loop(Block(Break())),
            expectedResultType: _symbols.Void);

        // loop with break with value
        TestBinding(
            Loop(Break(Constant(1))),
            expectedResultType: _symbols.Int32);

        // loop with break with value in block
        TestBinding(
            Loop(Block(Break(Constant(1)))),
            expectedResultType: _symbols.Int32);

        // loop with conditional break
        TestBinding(
            Loop(
                Condition(Constant(true), Constant(1), Break())),
            expectedResultType: _symbols.Void);

        // loop with conditional break returning value
        TestBinding(
            Loop(
                Condition(Constant(true), Constant(1), Break(Constant(2)))),
            expectedResultType: _symbols.Int32);

        // loop with conditional break returning compatible values
        TestBinding(
            Loop(
                Condition(Constant(true), Constant(1), Break(Constant(2L)))),
            expectedResultType: _symbols.Int64);

        // loop with multiple compatible breaks
        TestBinding(
            Loop(
                Block(
                    Break(),
                    Break())),
            expectedResultType: _symbols.Void);

        TestBinding(
            Loop(
                Block(
                    Break(Constant(1)),
                    Break(Constant(2L)))),
            expectedResultType: _symbols.Int64);

        // loop with multiple breaks, void and value
        TestBinding(
            Loop(
                Block(
                    Break(),
                    Break(Constant(1)))),
            expectedResultType: _symbols.Int32);

        TestBinding(
            Loop(
                Block(
                    Break(Constant(1)),
                    Break())),
            expectedResultType: _symbols.Int32);

        // loop with final expression (not compatible with break)
        // does not figure in loop's type
        TestBinding(
            Loop(
                Block(
                    Break(Constant(1)),
                    Constant("string"))),
            expectedResultType: _symbols.Int32);

        // loop with continue
        TestBinding(
            Loop(Continue()),
            expectedResultType: _symbols.Void);

        // loop with continue in block
        TestBinding(
            Loop(Block(Continue())),
            expectedResultType: _symbols.Void);

        // loop with continue between other expressions
        TestBinding(
            Loop(Block(
                Constant(1),
                Continue(),
                Constant(2))),
            expectedResultType: _symbols.Void);

        // loop with conditional continue.
        TestBinding(
            Loop(Condition(Constant(true), Continue())),
            expectedResultType: _symbols.Void);

        // loop with multiple continues
        TestBinding(
            Loop(Block(
                Continue(),
                Continue())),
            expectedResultType: _symbols.Void);

        // loop with continue and break
        TestBinding(
            Loop(Condition(Constant(true), Continue(), Break())),
            expectedResultType: _symbols.Void);

        // loop with continue and break with value
        TestBinding(
            Loop(Condition(Constant(true), Continue(), Break(Constant(1)))),
            expectedResultType: _symbols.Int32);
    }

    [TestMethod]
    public void TestOperators()
    {
        // Int32
        TestBinding(Add(Constant(1), Constant(2)), _symbols.Int32);

        TestBinding(Subtract(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Multiply(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Divide(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Remainder(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Negate(Constant(1)), _symbols.Int32);

        TestBinding(Equal(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(NotEqual(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(LessThan(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(LessThanOrEqual(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(GreaterThan(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(GreaterThanOrEqual(Constant(1), Constant(2)), _symbols.Boolean);

        TestBinding(BitwiseAnd(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(BitwiseOr(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(BitwiseXor(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(BitwiseNot(Constant(1)), _symbols.Int32);

        // boolean / logical
        TestBinding(And(Constant(true), Constant(true)), _symbols.Boolean);
        TestBinding(Or(Constant(true), Constant(true)), _symbols.Boolean);
        TestBinding(Not(Constant(true)), _symbols.Boolean);

        // string
        TestBinding(Add(Constant("one"), Constant("two")), _symbols.String);
        TestBinding(Equal(Constant("one"), Constant("two")), _symbols.Boolean);
    }

    [TestMethod]
    public void TestPath()
    {
        TestBinding(Type(_symbols.Int32).Member("MaxValue"), _symbols.Int32);
    }

    [TestMethod]
    public void TestNameReference()
    {
        TestBinding(
            Name("Int32"), 
            _symbols.Type, 
            _symbols.Int32);

        TestBinding(
            Name("System"),
            _symbols.Namespace,
            _symbols.System);
    }

    [TestMethod]
    public void TestTypeReference()
    {
        TestBinding(
            Type(_symbols.Int32), 
            expectedResultType: _symbols.Type, 
            expectedReferencedSymbol: _symbols.Int32);
    }

    [TestMethod]
    public void TestVariable()
    {
        // declaration with initializer
        TestBinding(
            Variable("x", Constant(1)),
            _symbols.Int32);

        // declare with type but no initializer
        TestBinding(
            Variable(Type(_symbols.Int32), "x"),
            _symbols.Int32);

        // declare with type and initializer
        TestBinding(
            Variable(Type(_symbols.Int32), "x", Constant(1)),
            _symbols.Int32);

        // declare with type and initializer with convertable types
        TestBinding(
            Variable(Type(_symbols.Int64), "x", Constant(1)),
            _symbols.Int64);

        // declare with type and initializer with non-convertable types
        TestBinding(
            Variable(Type(_symbols.Int64), "x", Constant("one")),
            _symbols.Int64,
            containsDiagnostics: true);

        // declare with no type and no initializer
        TestBinding(
            new VariableExpression("x", null, null, null, null, null, null),
            _symbols.Object,
            containsDiagnostics: true);
    }

    [TestMethod]
    public void TestVoid()
    {
        TestBinding(Void(), _symbols.Void);
    }

    private void TestBinding(
        Expression expression, 
        TypeSymbol? expectedResultType = null, 
        Symbol? expectedReferencedSymbol = null, 
        bool containsDiagnostics = false,
        BindingScope? scope = null)
    {
        var binder = new ExpressionBinder(_symbols.GlobalNamespace);
        var bound = binder.Bind(expression, scope ?? _defaultTestScope);

        Assert.IsFalse(bound.ContainsUnknowns, "expression contains unknowns after binding");

        if (expectedResultType != null)
        {
            var actualResultType = bound.ResultType;
            Assert.IsTrue(TypeEqualityComparer.Instance.Equals(expectedResultType, actualResultType), $"result type expected: {expectedResultType.Name} actual: {actualResultType.Name}");
        }

        if (expectedReferencedSymbol != null)
        {
            var actualReferencedSymbol = bound.ReferencedSymbol;
            Assert.IsTrue(SymbolEqualityComparer.Instance.Equals(expectedReferencedSymbol, actualReferencedSymbol), $"referenced symbol expected: {expectedReferencedSymbol?.Name ?? "null"} actual: {actualReferencedSymbol?.Name ?? "null"}");
        }

        if (bound.ContainsDiagnostics && !containsDiagnostics)
        {
            var dxs = new List<Diagnostic>();
            bound.GetContainedDiagnostics(dxs);
            Assert.Fail($"Unexpected diagnostic: {dxs[0]}");
        }
        else if (!bound.ContainsDiagnostics && containsDiagnostics)
        {
            Assert.Fail("Expected diagnostics, but none found.");
        }
    }
}
