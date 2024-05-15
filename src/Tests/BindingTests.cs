using Parkour;
using Parkour.Semantics;
using Parkour.Binding;
using Parkour.Reflection;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;
using static TestHelpers;

[TestClass]
public class BindingTests
{
    private readonly ReflectionSymbols _reflectionSymbols;

    public BindingTests()
    {
        _reflectionSymbols = ReflectionSymbols.CurrentMscorlib;
    }

    [TestMethod]
    public void TestDeclaration_Class()
    {
        // bind it and find it
        TestBind(
            [Class("C", [])],
            ["C"]);

        // bint in namespace
        TestBind(
            [Namespace("N", [Class("C", [])])],
            ["N", "N.C"]);
    }

    [TestMethod]
    public void TestDeclaration_Class_Constructor()
    {
        // bind it and default construct it.
        TestBindX(
            [Class("C", [])],
            ["C", "C.[.ctor]"],
            New(Name("C")),
            expectedResultType: "C");

        // bind it with explicit constructor
        TestBindX(
            [Class("C", [Constructor()])],
            ["C", "C.[.ctor]"],
            New(Name("C")),
            expectedResultType: "C");

        // constructor has parameter
        TestBindX([
            Class("C", [
                Constructor(
                    [Parameter("x", Int32Type)], 
                    Block())
                ])
            ],
            ["C", "C.[.ctor]"],
            New(Name("C"), [Constant(1)]),
            expectedResultType: "C");
    }

    [TestMethod]
    public void TestDeclaration_Class_Field()
    {
        // instance field
        TestBindX(
            [Class("C", [Field("F", Int32Type)])],
            ["C.F"],
            New(Name("C")).Member("F"),
            expectedResultType: "System.Int32");

        // static field
        TestBindX(
            [Class("C", [Field("F", Int32Type).WithModifiers(SymbolModifier.Static)])],
            ["C.F"],
            Name("C").Member("F"),
            expectedResultType: "System.Int32");
    }

    [TestMethod]
    public void TestDeclaration_Class_Method()
    {
        // instance void method
        TestBindX(
            [Class("C",
                [Method("M", [], VoidType, Block())])],
            ["C.M"],
            New(Name("C")).Member("M").Call(),
            expectedResultType: "System.Void"
            );

        // instance int returning method
        TestBindX(
            [Class("C",
                [Method("M", [], Int32Type, Constant(1))])],
            ["C.M"],
            New(Name("C")).Member("M").Call(),
            expectedResultType: "System.Int32"
            );

        // instance int returning method with parameters
        TestBindX(
            [Class("C",
                [Method("M", [Parameter("p", Int32Type)], Int32Type, Constant(1))])],
            ["C.M"],
            New(Name("C")).Member("M").Call([Constant(1)]),
            expectedResultType: "System.Int32"
            );

        // static void method
        TestBindX(
            [Class("C",
                [Method("M", [], VoidType, Block()).WithModifiers(SymbolModifier.Static)]
                )],
            ["C.M"],
            Name("C").Member("M").Call(),
            expectedResultType: "System.Void"
            );

        // static int returning method
        TestBindX(
            [Class("C",
                [Method("M", [], Int32Type, Constant(1)).WithModifiers(SymbolModifier.Static)]
                )],
            ["C.M"],
            Name("C").Member("M").Call(),
            expectedResultType: "System.Int32"
            );

        // static int returning method with parameter
        TestBindX(
            [Class("C",
                [Method("M", [Parameter("p", Int32Type)], Int32Type, Constant(1)).WithModifiers(SymbolModifier.Static)]
                )],
            ["C.M"],
            Name("C").Member("M").Call(Constant(1)),
            expectedResultType: "System.Int32"
            );
    }

    [TestMethod]
    public void TestDeclaration_Class_Property()
    {
        // instance property
        TestBindX(
            [Class("C", [
                Property("P", Int32Type)])
                ],
            ["C.P"],
            New(Name("C")).Member("P"),
            expectedResultType: "System.Int32");

        // static property
        TestBindX(
            [Class("C", [
                Property("P", SymbolAccess.Public, SymbolModifier.Static, Int32Type)])
                ],
            ["C.P"],
            Name("C").Member("P"),
            expectedResultType: "System.Int32");
    }

    [TestMethod]
    public void TestDeclaration_Class_Indexer()
    {
        TestBind(
            [Class("C",
                [
                    Indexer(
                        Symbol("System.Int32"),
                        [Parameter("index", Symbol("System.Int32"))],
                        Name("index"),
                        null)
                ]
                )],
            ["C", "C.Item"]);
    }


    [TestMethod]
    public void TestDeclaration_Class_ReferenceToSelf()
    {
        //// base types (illegal, but should find reference)
        //TestBind(
        //    [Class("C", [Name("C")], [])],
        //    ["C"]);

        // field that refers to self
        TestBind(
            Class("C", [Field("F", Name("C"))]),
            ["C.F"]);

        TestBind(
            Namespace("N",
                [Class("C", [Field("F", Name("C"))])]),
            ["N.C.F"]);
    }


    [TestMethod]
    public void TestDeclaration_Class_TypeParameters()
    {
        TestBind(
            [Class("C", []).WithTypeParameters([TypeParameter("T")])],
            New(Name("C").Construct([Int32Type])),
            expectedResultType: "C[System.Int32]"
            );
    }

    [TestMethod]
    public void TestDeclaration_Struct()
    {
        // instance constructor
        TestBindX(
            [Struct("S", [])],
            ["S", "S.[.ctor]"],
            New(Name("S")),
            expectedResultType: "S");
    }

    [TestMethod]
    public void TestDeclaration_Interface()
    {
        TestBind(
            [Interface("I", [])],
            ["I"]);
    }

    [TestMethod]
    public void TestDeclaration_Using()
    {
        TestBind([
            Using(Symbol("System")),

            Class("C", [
                Property("P", Name("Int32"))
                ])
            ],
            ["C.P"]
            );

        TestBind([
            Using("X", Symbol("System")),

            Class("C", [
                Property("P", Name("X").Member("Int32"))
                ])
            ],
            ["C.P"]
            );
    }

    #region Expressions

    [TestMethod]
    public void TestExpression_Array()
    {
        TestBind(
            Int32Type.Array(),
            expectedReferencedSymbol: "System.Int32[]");
    }

    [TestMethod]
    public void TestExpression_Arity()
    {
        TestBind(
            SystemCollectionsGenericNamespace.Member("List").WithArity(1),
            expectedReferencedSymbol: "System.Collections.Generic.List`1");
    }

    [TestMethod]
    public void TestExpression_Assign()
    {
        TestBind(
            Block(
                Variable(Int32Type, "x"),
                Assign(Name("x"), Constant(1))),
            expectedResultType: Int32Type.FullName
            );
    }

    [TestMethod]
    public void TestExpression_Block()
    {
        // block can contain just one expression
        TestBind(
            Block(Constant(1)),
            Int32Type.FullName);

        // last expression determines block type
        TestBind(
            Block(
                Constant("zero"),
                Constant(1)),
            Int32Type.FullName);


        // declare in block
        TestBind(
            Block(
                Variable("x", Constant(1)),
                Name("x")),
            Int32Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Branch()
    {
        // branch with expression to label w/o type (void)
        TestBind(
            Block(
                Branch("label", Constant(1)),
                Label("label")
                ),
            expectedResultType: SpecialSymbols.Void.FullName);

        TestBind(
            Block(
                Label("label"),
                Branch("label", Constant(1))
                ),
            expectedResultType: SpecialSymbols.DoesNotReturn.FullName);

        // branch w/o expression to label with type
        TestBind(
            Block(
                Branch("label"),
                Label("label", Int32Type)
                ),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Block(
                Label("label", Int32Type),
                Branch("label")
                ),
            expectedResultType: SpecialSymbols.DoesNotReturn.FullName);

        // branch with expression to label with convertable type
        TestBind(
            Block(
                Branch("label", Constant(1)),
                Label("label", Int64Type)
                ),
            expectedResultType: Int64Type.FullName);

        TestBind(
            Block(
                Label("label", Int64Type),
                Branch("label", Constant(1))
                ),
            expectedResultType: SpecialSymbols.DoesNotReturn.FullName);

        // branch with expression to label with non-convertable type
        TestBind(
            Block(
                Branch("label", Constant(1)),
                Label("label", StringType)
                ),
                containsDiagnostics: true);

        TestBind(
            Block(
                Label("label", StringType),
                Branch("label", Constant(1))
                ),
                containsDiagnostics: true);

        // branch alone to unknown target
        TestBind(
            Branch("label"),
            expectedResultType: SpecialSymbols.DoesNotReturn.FullName,
            containsDiagnostics: true);

        // branch in block to unknown target
        TestBind(
            Block(Branch("label")),
            expectedResultType: SpecialSymbols.DoesNotReturn.FullName,
            containsDiagnostics: true);

        // branch to outer label
        TestBind(
            Block(
                If(Constant(true), Branch("label")),
                Label("label")),
            expectedResultType: SpecialSymbols.Void.FullName);

        // branch to label in outer block
        TestBind(
            Block(
                Block(Branch("label")),
                Label("label")),
            expectedResultType: SpecialSymbols.Void.FullName);
    }

    [TestMethod]
    public void TestExpression_Call()
    {
        TestBind(
            Call(Constant(1).Member("ToString")), 
            expectedResultType: StringType.FullName);
    }

    [TestMethod]
    public void TestExpression_Condition()
    {
        // whenTrue/whenFalse same types
        TestBind(
            Condition(Constant(true), Constant(1), Constant(0)),
            expectedResultType: Int32Type.FullName);

        // whenTrue/whenFalse not same types, convertable
        TestBind(
            Condition(Constant(true), Constant(1), Constant(1L)),
            expectedResultType: Int64Type.FullName);

        // whenTrue / whenFalse not same types, not convertable
        TestBind(
            Condition(Constant(true), Constant(1), Constant("zero")),
            expectedResultType: ObjectType.FullName,
            containsDiagnostics: true);

        // whenTrue is void (void path leads to default)
        TestBind(
            Condition(Constant(true), Void(), Constant(2)),
            expectedResultType: Int32Type.FullName);

        // whenFalse is void (void path leads to default)
        TestBind(
            Condition(Constant(true), Constant(1), Void()),
            expectedResultType: Int32Type.FullName);

        // both are void
        TestBind(
            Condition(Constant(true), Void(), Void()),
            expectedResultType: SpecialSymbols.Void.FullName);
    }

    [TestMethod]
    public void TestExpression_Constant()
    {
        TestBind(
            Constant(1), 
            Int32Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Construct()
    {
        TestBind(
            ListTType.Construct([Int32Type]),
            expectedReferencedSymbol: "System.Collections.Generic.List[System.Int32]");
    }

    [TestMethod]
    public void TestExpression_Convert()
    {
        // TODO: add tests for other conversions, boxing, unboxing, downcast, upcast, user-defined

        TestBind(
            Convert(Constant(1), Int64Type),
            expectedResultType: Int64Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Default()
    {
        // default w/ type expression
        TestBind(
            Default(Int32Type),
            expectedResultType: Int32Type.FullName);

        // default w/o type expression
        TestBind(
            Default(),
            containsDiagnostics: true);

        // default w/o type but with target type
        TestBind(
            Variable(Int32Type, "x", Default()),
            expectedResultType: Int32Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Element()
    {
        TestBind(
            NewArray(Int32Type, Constant(2)).Element(Constant(0)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            NewArray(StringType, Constant(2)).Element(Constant(0)),
            expectedResultType: StringType.FullName);

        TestBind(
            New(ListInt32Type,[]).Element(Constant(0)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            New(ListStringType, []).Element(Constant(0)),
            expectedResultType: StringType.FullName);
    }

    [TestMethod]
    public void TestExpression_IsType()
    {
        TestBind(
            IsType(Name("System").Member("Int32"), Symbol("System.Int32")),
            "System.Boolean"
            );
    }

    [TestMethod]
    public void TestExpression_Label()
    {
        // lone void label okay
        TestBind(
            Label("x"),
            expectedResultType: SpecialSymbols.Void.FullName);

        // lone void label in block okay
        TestBind(
            Block(Label("x")),
            expectedResultType: SpecialSymbols.Void.FullName);

        // lone label with receiving type okay
        TestBind(
            Label("x", Int32Type),
            expectedResultType: Int32Type.FullName);

        // label in block with receiving type okay
        TestBind(
            Block(Label("x", Int32Type)),
            expectedResultType: Int32Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Lambda()
    {
        // lambda with no parameters and no returns
        TestBind(
            Lambda(Constant(1)));

        // called lambda with no parameters and no return
        TestBind(
            Call(Lambda(Constant(1))),
            expectedResultType: Int32Type.FullName);

        // called lambda with no parameters and no return
        TestBind(
            Call(Lambda(Void())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // lambda with parameter
        TestBind(
            Lambda(
                [Parameter("x", StringType)],
                Name("x")));

        // call lambda with parameter
        TestBind(
            Call(
                Lambda(
                    [Parameter("x", StringType)],
                    Name("x")),
                Constant("string")),
            expectedResultType: StringType.FullName);

        // lambda with return
        TestBind(
            Call(
                Lambda(
                    Return(Constant(1)))),
            expectedResultType: Int32Type.FullName);

        // lambda with return and value
        TestBind(
            Call(
                Lambda(
                    Block(
                        Return(Constant(1)),
                        Constant(2L)
                        ))),
            expectedResultType: Int64Type.FullName);

        // lambda with conditional true and value
        TestBind(
            Call(
                Lambda(
                    Condition(Constant(true), Return(Constant(1)), Constant(2L))
                    )),
            expectedResultType: Int64Type.FullName);

        // lambda with conditional returns
        TestBind(
            Call(
                Lambda(
                    Condition(Constant(true), Return(Constant(1)), Return(Constant(2L)))
                    )),
            expectedResultType: Int64Type.FullName);

        // lambda with non-convertable returns
        TestBind(
            Call(
                Lambda(
                    Condition(Constant(true), Return(Constant(1)), Return(Constant("two")))
                    )),
            expectedResultType: ObjectType.FullName);
    }

    [TestMethod]
    public void TestExpression_Loop()
    {
        // loop with no break
        TestBind(
            Loop(Constant(1)),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with block & no break
        TestBind(
            Loop(Block(Constant(1))),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with break
        TestBind(
            Loop(Break()),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with break in block
        TestBind(
            Loop(Block(Break())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with break with value
        TestBind(
            Loop(Break(Constant(1))),
            expectedResultType: Int32Type.FullName);

        // loop with break with value in block
        TestBind(
            Loop(Block(Break(Constant(1)))),
            expectedResultType: Int32Type.FullName);

        // loop with conditional break
        TestBind(
            Loop(
                Condition(Constant(true), Constant(1), Break())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with conditional break returning value
        TestBind(
            Loop(
                Condition(Constant(true), Constant(1), Break(Constant(2)))),
            expectedResultType: Int32Type.FullName);

        // loop with conditional break returning compatible values
        TestBind(
            Loop(
                Condition(Constant(true), Constant(1), Break(Constant(2L)))),
            expectedResultType: Int64Type.FullName);

        // loop with multiple compatible breaks
        TestBind(
            Loop(
                Block(
                    Break(),
                    Break())),
            expectedResultType: SpecialSymbols.Void.FullName);

        TestBind(
            Loop(
                Block(
                    Break(Constant(1)),
                    Break(Constant(2L)))),
            expectedResultType: Int64Type.FullName);

        // loop with multiple breaks, void and value
        TestBind(
            Loop(
                Block(
                    Break(),
                    Break(Constant(1)))),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Loop(
                Block(
                    Break(Constant(1)),
                    Break())),
            expectedResultType: Int32Type.FullName);

        // loop with final expression (not compatible with break)
        // does not figure in loop's type
        TestBind(
            Loop(
                Block(
                    Break(Constant(1)),
                    Constant("string"))),
            expectedResultType: Int32Type.FullName);

        // loop with continue
        TestBind(
            Loop(Continue()),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with continue in block
        TestBind(
            Loop(Block(Continue())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with continue between other expressions
        TestBind(
            Loop(Block(
                Constant(1),
                Continue(),
                Constant(2))),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with conditional continue.
        TestBind(
            Loop(Condition(Constant(true), Continue())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with multiple continues
        TestBind(
            Loop(Block(
                Continue(),
                Continue())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with continue and break
        TestBind(
            Loop(Condition(Constant(true), Continue(), Break())),
            expectedResultType: SpecialSymbols.Void.FullName);

        // loop with continue and break with value
        TestBind(
            Loop(Condition(Constant(true), Continue(), Break(Constant(1)))),
            expectedResultType: Int32Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Member()
    {
        TestBind(
            Int32Type.Member("MaxValue"), 
            expectedResultType: Int32Type.FullName);
    }

    [TestMethod]
    public void TestExpression_Name()
    {
        // refer to a variable in scope
        TestBind(
            Block(
                Variable("x", Constant(1)),
                Name("x")),
            expectedResultType: Int32Type.FullName);

        // refer to a namespace name in scope
        TestBind(
            Name("System"),
            expectedResultType: "Namespace",
            expectedReferencedSymbol: "System");

        // refer to a parameter in scope
        TestBind(
            Lambda([Parameter("x", Int32Type)], Name("x"))
            );
    }

    [TestMethod]
    public void TestExpression_New()
    {
        TestBind(
            New(ObjectType, []),
            expectedResultType: ObjectType.FullName
            );

        TestBind(
            New(ListTType.Construct([Int32Type])),
            expectedResultType: "System.Collections.Generic.List[System.Int32]"
            );
    }

    [TestMethod]
    public void TestExpression_NewArrayInit()
    {
        TestBind(
            NewArray(Int32Type, [Constant(1), Constant(2), Constant(3)]),
            expectedResultType: "System.Int32[]"
            );

        // infered from value types
        TestBind(
            NewArray([Constant(1), Constant(2), Constant(3)]),
            expectedResultType: "System.Int32[]"
            );

        TestBind(
            NewArray([Constant(1), Constant(2L), Constant(3)]),
            expectedResultType: "System.Int64[]"
            );

        // inferred from target type
        TestBind(
            Variable(Int32Type.Array(), "x", NewArray([Constant(1), Constant(2), Constant(3)])),
            expectedResultType: "System.Int32[]"
            );
    }

    [TestMethod]
    public void TestExpression_NewArraySize()
    {
        TestBind(
            NewArray(Int32Type, Constant(10)),
            expectedResultType: "System.Int32[]"
            );
    }

    [TestMethod]
    public void TestExpression_Operator()
    {
        // Int32
        TestBind(
            Add(Constant(1), Constant(2)), 
            expectedResultType: Int32Type.FullName);

        TestBind(
            Subtract(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Multiply(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Divide(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Remainder(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Negate(Constant(1)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            Equal(Constant(1), Constant(2)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            NotEqual(Constant(1), Constant(2)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            LessThan(Constant(1), Constant(2)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            LessThanOrEqual(Constant(1), Constant(2)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            GreaterThan(Constant(1), Constant(2)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            GreaterThanOrEqual(Constant(1), Constant(2)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            BitwiseAnd(Constant(1), Constant(2)), 
            expectedResultType: Int32Type.FullName);

        TestBind(
            BitwiseOr(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            BitwiseXor(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        TestBind(
            BitwiseNot(Constant(1)),
            expectedResultType: Int32Type.FullName);

        // boolean / logical
        TestBind(
            LogicalAnd(Constant(true), Constant(true)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            LogicalOr(Constant(true), Constant(true)),
            expectedResultType: BooleanType.FullName);

        TestBind(
            LogicalNot(Constant(true)),
            expectedResultType: BooleanType.FullName);

        // string
        TestBind(
            Equal(Constant("one"), Constant("two")),
            expectedResultType: BooleanType.FullName);

        TestBind(
            Add(Constant("one"), Constant("two")),
            expectedResultType: StringType.FullName);
    }

    [TestMethod]
    public void TestExpression_Symbol()
    {
        TestBind(
            Symbol("System.Int32"),
            expectedResultType: "System.Type",
            expectedReferencedSymbol: "System.Int32");

        TestBind(
            Symbol("System"),
            expectedResultType: "Namespace",
            expectedReferencedSymbol: "System");

        TestBind(
            Symbol("System.Collections.Generic"),
            expectedResultType: "Namespace");
    }

    [TestMethod]
    public void TestExpression_This()
    {
        TestBind(
            This(),
            containsDiagnostics: true);

        TestBind(
            Class("C", [
                Method("M",[], Name("C"), This())
                ]),
            (Declaration decl) =>
            {
                var te = decl.FirstDescendantOrSelf<ThisExpression>();
                Assert.IsNotNull(te);
                Assert.IsNotNull(te.ResultType);
                Assert.AreEqual("C", te.ResultType.FullName);
            });
    }

    [TestMethod]
    public void TestExpression_TypeOf()
    {
        TestBind(
            TypeOf(Symbol("System.Int32")),
            "System.Type"
            );
    }

    [TestMethod]
    public void TestExpression_Variable()
    {
        // declaration with initializer
        TestBind(
            Variable("x", Constant(1)),
            expectedResultType: Int32Type.FullName);

        // declare with type but no initializer
        TestBind(
            Variable(Int32Type, "x"),
            expectedResultType: Int32Type.FullName);

        // declare with type and initializer
        TestBind(
            Variable(Int32Type, "x", Constant(1)),
            expectedResultType: Int32Type.FullName);

        // declare with type and initializer with convertable types
        TestBind(
            Variable(Int64Type, "x", Constant(1)),
            expectedResultType: Int64Type.FullName);

        // declare with type and initializer with non-convertable types
        TestBind(
            Variable(Int64Type, "x", Constant("one")),
            expectedResultType: Int64Type.FullName,
            containsDiagnostics: true);

        // declare with no type and no initializer
        TestBind(
            new VariableExpression("x", null, null, null, null, null, null),
            expectedResultType: ObjectType.FullName,
            containsDiagnostics: true);
    }

    [TestMethod]
    public void TestExpression_Void()
    {
        TestBind(
            Void(),
            expectedResultType: SpecialSymbols.Void.FullName);
    }

#if false


#endif

    #endregion

    private void TestBind(
        Declaration declaration, 
        string[]? expectedSymbols = null)
    {
        TestBind([declaration], expectedSymbols);
    }

    private void TestBind(
        Declaration declaration,
        Action<Declaration> fnValidateDecl)
    {
        TestBindX(
            declarations: [declaration], 
            fnValidateDecls: decls => fnValidateDecl(decls[0]));
    }

    private void TestBind(
        Declaration[] declarations, 
        string[]? expectedSymbols = null)
    {
        TestBindX(declarations, expectedSymbols, null, null, null);
    }

    private void TestBind(
        Declaration[] declarations,
        Action<ImmutableList<Declaration>> fnValidateDecls)
    {
        TestBindX(declarations, fnValidateDecls: fnValidateDecls);
    }

    private void TestBind(
        Expression expression, 
        string? expectedResultType = null, 
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false)
    {
        TestBindX(null, null, expression, expectedResultType, expectedReferencedSymbol, containsDiagnostics);
    }

    private void TestBind(
        Expression expression,
        Action<Expression> fnValidateExpr)
    {
        TestBindX(expression: expression, fnValidateExpr: fnValidateExpr);
    }

    private void TestBind(
        Declaration[] declarations,
        Expression expression,
        string? expectedResultType = null,
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false)
    {
        TestBindX(declarations, null, expression, expectedResultType, expectedReferencedSymbol, containsDiagnostics);
    }

    private void TestBindX(
        Declaration[]? declarations = null,
        string[]? expectedSymbols = null,
        Expression? expression = null,
        string? expectedResultType = null, 
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false,
        Action<ImmutableList<Declaration>>? fnValidateDecls = null,
        Action<Expression>? fnValidateExpr = null)
    {
        var binder = new StandardSemanticBinder();

        SymbolTable bindSymbols = _reflectionSymbols;

        DeclarationBinding? declBinding = null;
        if (declarations != null)
        {
            declBinding = binder.BindDeclarations(declarations.ToImmutableList(), _reflectionSymbols);

            if (declBinding.Diagnostics.Count > 0 && !containsDiagnostics)
            {
                Assert.Fail($"Unexpected declaration diagnostics:\n{declBinding.Diagnostics[0]}");
            }

            if (expectedSymbols != null)
            {
                foreach (var path in expectedSymbols)
                {
                    var symbol = declBinding.Symbols.GetSymbol(path);
                    Assert.IsNotNull(symbol, $"symbol '{path}' not found");
                }
            }

            bindSymbols = declBinding.Symbols;
        }

        ExpressionBinding? exprBinding = null;
        if (expression != null)
        {
            exprBinding = binder.BindExpression(expression, bindSymbols);

            if (exprBinding.Diagnostics.Count > 0 && !containsDiagnostics)
            {
                Assert.Fail($"Unexpected expression diagnostics:\n{exprBinding.Diagnostics[0]}");
            }

            if (expectedResultType != null)
            {
                Assert.IsNotNull(exprBinding.Expression.ResultType);
                Assert.AreEqual(expectedResultType, exprBinding.Expression.ResultType.FullName);
            }

            if (expectedReferencedSymbol != null)
            {
                Assert.IsNotNull(exprBinding.Expression.ReferencedSymbol);
                Assert.AreEqual(expectedReferencedSymbol, exprBinding.Expression.ReferencedSymbol.FullName);
            }
        }

        if (declBinding != null && fnValidateDecls != null)
            fnValidateDecls(declBinding.Declarations);

        if (exprBinding != null && fnValidateExpr != null)
            fnValidateExpr(exprBinding.Expression);
    }
}