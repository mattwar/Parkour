using Parkour;
using Parkour.Semantics;
using Parkour.Reflection;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;
using static TestHelpers;

[TestClass]
public class BindingTests
{
    public BindingTests()
    {
    }

    [TestMethod]
    public void TestDeclaration_Class()
    {
        // bind it and find it
        TestBind(
            [Class("C", [])],
            expectedSymbols: ["C"]);

        // bint in namespace
        TestBind(
            [Namespace("N", [Class("C", [])])],
            expectedSymbols: ["N", "N.C"]);
    }

    [TestMethod]
    public void TestDeclaration_Class_Constructor()
    {
        // bind it and default construct it.
        TestBind(
            [
                Class("C", []),
                Symbol("C").New()
            ],
            expectedSymbols: ["C", "C.[.ctor]"],
            expectedResultType: "C");

        // bind it with explicit constructor
        TestBind(
            [
                Class("C", [Constructor()]),
                Symbol("C").New(),
            ],
            expectedSymbols: ["C", "C.[.ctor]"],
            expectedResultType: "C");

        // constructor has parameter
        TestBind([
            Class("C", [
                Constructor(
                    [Parameter("x", Int32Type)], 
                    Block())
                ]),
            Symbol("C").New([Constant(1)])
            ],
            expectedSymbols: ["C", "C.[.ctor]"],           
            expectedResultType: "C");
    }

    [TestMethod]
    public void TestDeclaration_Class_Field()
    {
        // instance field
        TestBind(
            [
                Class("C", [Field("F", Int32Type)]),
                New(Name("C")).Member("F"),
            ],
            expectedSymbols: ["C.F"],
            expectedResultType: Int32Type.FullName);

        // static field
        TestBind(
            [
                Class("C", [Field("F", Int32Type).WithModifiers(SymbolModifier.Static)]),
                Name("C").Member("F"),
            ],
            expectedSymbols: ["C.F"],
            expectedResultType: Int32Type.FullName);

        // instance field with initializer
        TestBind(
            [
                Class("C", [Field("F", Int32Type, Constant(10))]),
                New(Name("C")).Member("F"),
            ],
            expectedSymbols: ["C.F"],
            expectedResultType: Int32Type.FullName);

        // infer field type from initializer
        TestBind(
            [
                Class("C", [Field("F", fieldType: null, Constant(10))]),
                New(Name("C")).Member("F"),
            ],
            expectedSymbols: ["C.F"],
            expectedResultType: Int32Type.FullName);

        // cyclic field
        TestBind(
            [
                Class("C", [Field("F", fieldType: null, Name("F"))]),
                New(Name("C")).Member("F"),
            ],
            expectedSymbols: ["C.F"],
            expectedResultType: "CyclicDefinition");

        // no field type or initializer
        TestBind(
            [
                Class("C", [Field("F", fieldType: null, initalizer: null)]),
                New(Name("C")).Member("F"),
            ],
            expectedSymbols: ["C.F"],
            expectedResultType: "System.Object");
    }

    [TestMethod]
    public void TestDeclaration_Class_Method()
    {
        // instance void method
        TestBind(
            [
                Class("C",
                    [Method("M", [], VoidType, Block())]),
                New(Name("C")).Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: VoidType.FullName
            );

        // instance int returning method
        TestBind(
            [
                Class("C", [Method("M", [], Int32Type, Constant(1))]),
                New(Name("C")).Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: Int32Type.FullName
            );

        // instance int returning method with parameters
        TestBind(
            [
                Class("C", [Method("M", [Parameter("p", Int32Type)], Int32Type, Constant(1))]),
                New(Name("C")).Member("M").Call([Constant(1)])
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: Int32Type.FullName
            );

        // static void method
        TestBind(
            [
                Class("C", [Method("M", [], VoidType, Block()).WithModifiers(SymbolModifier.Static)]),
                Name("C").Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: VoidType.FullName
            );

        // static int returning method
        TestBind(
            [
                Class("C", [Method("M", [], Int32Type, Constant(1)).WithModifiers(SymbolModifier.Static)]),
                Name("C").Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: Int32Type.FullName
            );

        // static int returning method with parameter
        TestBind(
            [
                Class("C", [Method("M", [Parameter("p", Int32Type)], Int32Type, Constant(1)).WithModifiers(SymbolModifier.Static)]),
                Name("C").Member("M").Call(Constant(1))
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: Int32Type.FullName
            );

        // inferred return type
        TestBind(
            [
                Class("C", [Method("M", [], returnType: null, Constant(1))]),
                New(Name("C")).Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: Int32Type.FullName
            );

        // cyclic inferred return type
        TestBind(
            [
                Class("C", [Method("M", [], returnType: null, Name("M").Call())]),
                New(Name("C")).Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: SpecialSymbols.CyclicDefinition.FullName
            );

        // infinite recursion - bad, maybe warn on easy identifiable cases
        TestBind(
            [
                Class("C", [Method("M", [], Int32Type, Name("M").Call())]),
                New(Name("C")).Member("M").Call()
            ],
            expectedSymbols: ["C.M"],
            expectedResultType: Int32Type.FullName
            );
    }

    [TestMethod]
    public void TestDeclaration_Class_Property()
    {
        // instance property
        TestBind(
            [
                Class("C", [Property("P", Int32Type)]),
                New(Name("C")).Member("P")
            ],
            expectedSymbols: ["C.P"],
            expectedResultType: Int32Type.FullName
            );

        // static property
        TestBind(
            [
                Class("C", [Property("P", Int32Type).WithModifiers(SymbolModifier.Static)]),
                Name("C").Member("P")
            ],
            expectedSymbols: ["C.P"],
            expectedResultType: Int32Type.FullName
            );

        // inferred property type
        TestBind(
            [
                Class("C", [Property("P", propertyType: null, Constant(123))]),
                New(Name("C")).Member("P")
            ],
            expectedSymbols: ["C.P"],
            expectedResultType: Int32Type.FullName
            );

        // inferred cyclic definition
        TestBind(
            [
                Class("C", [Property("P", propertyType: null, Name("P"))]),
                New(Name("C")).Member("P")
            ],
            expectedSymbols: ["C.P"],
            expectedResultType: SpecialSymbols.CyclicDefinition.FullName
            );
    }

    [TestMethod]
    public void TestDeclaration_Class_Indexer()
    {
        // instance indexer
        TestBind(
            [
                Class("C", [
                    Indexer(
                        Int32Type,
                        [Parameter("index", Int32Type)],
                        Name("index"),
                        null)
                    ])
            ],
            expectedSymbols: ["C", "C.Item"]);

        // inferred element type
        TestBind(
            [
                Class("C", [
                    Indexer(
                        elementType: null,
                        [Parameter("index", Int32Type)],
                        Name("index"),
                        null)
                    ])
            ],
            expectedSymbols: ["C", "C.Item"]);
    }

    [TestMethod]
    public void TestDeclaration_Class_ReferenceToSelf()
    {
        // base types (illegal, but should bind)
        TestBind(
            [Class("C", [Name("C")], [])],
            expectedSymbols: ["C"]
            );

        // field that refers to self
        TestBind(
            [Class("C", [Field("F", Name("C"))])],
            expectedSymbols: ["C.F"]
            );
    }

    [TestMethod]
    public void TestDeclaration_Class_TypeParameters()
    {
        TestBind(
            [
                Class("C", []).WithTypeParameters([TypeParameter("T")]),
                New(Name("C").Construct([Int32Type])),
            ],
            expectedResultType: "C[System.Int32]"
            );
    }

    [TestMethod]
    public void TestDeclaration_Delegate()
    {
        TestBind(
            Delegate("D", [], VoidType),
            expectedSymbols: ["D"]
            );

        TestBind(
            Delegate("D", [Parameter("P", Int32Type)], Int32Type),
            expectedSymbols: ["D"]
            );
    }

    [TestMethod]
    public void TestDeclaration_Struct()
    {
        // instance constructor
        TestBind(
            [
                Struct("S", []),
                Symbol("S").New(),
            ],
            expectedSymbols: ["S", "S.[.ctor]"],
            expectedResultType: "S");
    }

    [TestMethod]
    public void TestDeclaration_Interface()
    {
        TestBind(
            [Interface("I", [])],
            expectedSymbols: ["I"]
            );
    }

    [TestMethod]
    public void TestDeclaration_Using()
    {
        // Members of System are now in scope
        TestBind(
            [
                Using(Symbol("System")),
                Class("C", [
                    Property("P", Name("Int32"))
                    ])
            ],
            expectedSymbols: ["C.P"]
            );

        // alias X for System is now in scope
        TestBind(
            [
                Using("X", Symbol("System")),
                Class("C", [
                    Property("P", Name("X").Member("Int32"))
                    ])
            ],
            expectedSymbols: ["C.P"]
            );
    }

    #region Expressions

    [TestMethod]
    public void TestExpression_Array()
    {
        TestBind(
            [Int32Type.Array()],
            expectedReferencedSymbol: "System.Int32[]");
    }

    [TestMethod]
    public void TestExpression_Arity()
    {
        TestBind(
            [SystemCollectionsGenericNamespace.Member("List").WithArity(1)],
            expectedReferencedSymbol: "System.Collections.Generic.List`1");
    }

    [TestMethod]
    public void TestExpression_Assign()
    {
        TestBind(
            [
                Block(
                    Variable(Int32Type, "x"),
                    Assign(Name("x"), Constant(1)))
            ],
            expectedResultType: Int32Type.FullName
            );
    }

    [TestMethod]
    public void TestExpression_Block()
    {
        // block can contain just one expression
        TestBind(
            Block(Constant(1)),
            expectedResultType: Int32Type.FullName);

        // last expression determines block type
        TestBind(
            Block(
                Constant("zero"),
                Constant(1)),
            expectedResultType: Int32Type.FullName);


        // declare in block
        TestBind(
            Block(
                Variable("x", Constant(1)),
                Name("x")),
            expectedResultType: Int32Type.FullName);
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
            expectedResultType: VoidType.FullName);

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
            expectedResultType: VoidType.FullName);

        // branch to label in outer block
        TestBind(
            Block(
                Block(Branch("label")),
                Label("label")),
            expectedResultType: VoidType.FullName);
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
            Condition(Constant(true), Block(), Constant(2)),
            expectedResultType: Int32Type.FullName);

        // whenFalse is void (void path leads to default)
        TestBind(
            Condition(Constant(true), Constant(1), Block()),
            expectedResultType: Int32Type.FullName);

        // both are void
        TestBind(
            Condition(Constant(true), Block(), Block()),
            expectedResultType: VoidType.FullName);
    }

    [TestMethod]
    public void TestExpression_Constant()
    {
        TestBind(
            Constant(1),
            expectedResultType: Int32Type.FullName);
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
            Constant(123).IsType(Int32Type),
            expectedResultType: BooleanType.FullName
            );
    }

    [TestMethod]
    public void TestExpression_Label()
    {
        // lone void label okay
        TestBind(
            Label("x"),
            expectedResultType: VoidType.FullName);

        // lone void label in block okay
        TestBind(
            Block(Label("x")),
            expectedResultType: VoidType.FullName);

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
            Call(Lambda(Block())),
            expectedResultType: VoidType.FullName);

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
            expectedResultType: VoidType.FullName);

        // loop with block & no break
        TestBind(
            Loop(Block(Constant(1))),
            expectedResultType: VoidType.FullName);

        // loop with break
        TestBind(
            Loop(Break()),
            expectedResultType: VoidType.FullName);

        // loop with break in block
        TestBind(
            Loop(Block(Break())),
            expectedResultType: VoidType.FullName);

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
            expectedResultType: VoidType.FullName);

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
            expectedResultType: VoidType.FullName);

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
            expectedResultType: VoidType.FullName);

        // loop with continue in block
        TestBind(
            Loop(Block(Continue())),
            expectedResultType: VoidType.FullName);

        // loop with continue between other expressions
        TestBind(
            Loop(Block(
                Constant(1),
                Continue(),
                Constant(2))),
            expectedResultType: VoidType.FullName);

        // loop with conditional continue.
        TestBind(
            Loop(Condition(Constant(true), Continue())),
            expectedResultType: VoidType.FullName);

        // loop with multiple continues
        TestBind(
            Loop(Block(
                Continue(),
                Continue())),
            expectedResultType: VoidType.FullName);

        // loop with continue and break
        TestBind(
            Loop(Condition(Constant(true), Continue(), Break())),
            expectedResultType: VoidType.FullName);

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
            fnValidate: elements =>
            {
                var te = elements[0].FirstDescendantOrSelf<ThisExpression>();
                Assert.IsNotNull(te);
                Assert.IsNotNull(te.ResultType);
                Assert.AreEqual("C", te.ResultType.FullName);
            });

        TestBind(
            Class("C", [
                Field("F", Int32Type),
                Method("M",[], Int32Type, This().Member("F"))
                ])
            );

        TestBind(
            Class("C", [
                Property("P", Int32Type),
                Method("M",[], Int32Type, This().Member("P"))
                ])
            );

        TestBind(
            Class("C", [
                Method("M",[], Int32Type, This().Member("M").Call())
                ])
            );
    }

    [TestMethod]
    public void TestExpression_TypeOf()
    {
        TestBind(
            TypeOf(Int32Type),
            expectedResultType: "System.Type"
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

    #endregion

    private void TestBind(
        SemanticElement element,
        string[]? expectedSymbols = null,
        string? expectedResultType = null,
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false,
        Action<ImmutableList<SemanticElement>>? fnValidate = null
        )
    {
        TestBind(
            [element],
            expectedSymbols,
            expectedResultType,
            expectedReferencedSymbol,
            containsDiagnostics,
            fnValidate
            );
    }

    private void TestBind(
        ImmutableList<SemanticElement> elements,
        string[]? expectedSymbols = null,
        string? expectedResultType = null, 
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false,
        Action<ImmutableList<SemanticElement>>? fnValidate = null
        )
    {
        var binder = new StandardBinder();
        var binding = binder.Bind(elements, ReflectionSymbols.CurrentMscorlib);
        var diagnostics = binding.Elements.GetContainedDiagnostics();
        if (diagnostics.Count > 0 && !containsDiagnostics)
        {
            Assert.Fail($"Unexpected declaration diagnostics:\n{diagnostics[0]}");
        }
        else if (containsDiagnostics && diagnostics.Count == 0)
        {
            Assert.Fail($"Unexpected missing diagnostics");
        }

        if (expectedSymbols != null)
        {
            foreach (var path in expectedSymbols)
            {
                var symbol = binding.CombinedSymbols.GetSymbol(path);
                Assert.IsNotNull(symbol, $"symbol '{path}' not found");
            }
        }

        if (binding.Elements.OfType<Expression>().LastOrDefault() is Expression expr)
        {
            if (expectedResultType != null)
                Assert.AreEqual(expectedResultType, expr.ResultType?.FullName, "result type");

            if (expectedReferencedSymbol != null)
                Assert.AreEqual(expectedReferencedSymbol, expr.ReferencedSymbol?.FullName, "referenced symbol");
        }

        if (fnValidate != null)
            fnValidate(binding.Elements);
    }
}