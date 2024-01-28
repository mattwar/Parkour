namespace Parkour.Semantics;
using Symbols;

public class SemanticWriter
{
    private TextWriter _writer;
    private readonly string _indentation;
    private string _currentIndentation;
    private bool _needsIndentation;

    public SemanticWriter(TextWriter writer, string? indentation = null)
    {
        _writer = writer;
        _indentation = indentation ?? "  ";
        _currentIndentation = "";
        _needsIndentation = false;
    }

    public SemanticWriter(string? indentation = null)
        : this(new StringWriter(), indentation)
    {
    }

    public string WriteToString(SemanticElement semantic)
    {
        Write(semantic);
        return _writer.ToString() ?? "";
    }

    private void WriteLine(string text = "")
    {
        Write(text);
        _writer.WriteLine();
        _needsIndentation = true;
    }

    private void Write(string text)
    {
        if (_needsIndentation)
            _writer.Write(_currentIndentation);

        if (text.Contains("\n") || text.Contains("\r"))
        {
            var lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (i < lines.Length - 1)
                {
                    WriteLine(line);
                }
                else
                {
                    Write(line);
                }
            }
        }
        else
        {
            _writer.Write(text);
        }

        _needsIndentation = text.Length > 0 && (text[^1] == '\r' || text[^1] == '\n');
    }

    private void WriteIndented(Action action)
    {
        var oldIndentation = _currentIndentation;
        _currentIndentation += _indentation;
        action();
        _currentIndentation = oldIndentation;
    }

    private void WriteIndented(SemanticElement semantic)
    {
        var oldIndentation = _currentIndentation;
        _currentIndentation += _indentation;
        Write(semantic);
        _currentIndentation = oldIndentation;
    }

    private void WriteBlockOrIndented(SemanticElement semantic)
    {
        if (semantic is BlockExpression block)
        {
            Write(semantic);
        }
        else
        {
            WriteIndented(semantic);
        }
    }

    private void WriteLine(SemanticElement semantic)
    {
        Write(semantic);
        WriteLine();
    }

    private void Write(SemanticElement semantic)
    {
        switch (semantic)
        {
            case ArityExpression arity:
                Write(arity.Expression);
                Write("<");
                for (int i = 0; i < arity.Arity - 1; i++)
                    Write(",");
                Write(">");
                Write(arity.Arity.ToString());
                break;

            case BlockExpression block:
                WriteLine();
                WriteLine("{");
                WriteIndented(() =>
                {
                    for (int i = 0; i < block.Expressions.Count; i++)
                    {
                        Write(block.Expressions[i]);
                        if (i < block.Expressions.Count - 1)
                            WriteLine(";");
                    }
                });
                WriteLine();
                WriteLine("}");
                break;

            case BranchExpression branch:
                if (branch.IsBreak)
                {
                    Write("break");
                }
                else if (branch.IsContinue)
                {
                    Write("continue");
                }
                else if (branch.IsReturn)
                {
                    Write("return");
                }
                else
                {
                    Write("goto ");
                    Write(branch.LabelName);
                }

                if (branch.Expression != null)
                {
                    Write(" ");
                    Write(branch.Expression);
                }
                break;

            case CallExpression call:
                Write(call.Expression);
                Write("(");
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    Write(call.Arguments[i]);
                }
                Write(")");
                break;

            case ConditionExpression condition:
                Write("if (");
                Write(condition.Test);
                WriteLine(")");
                WriteBlockOrIndented(condition.WhenTrue);
                WriteLine("else");
                WriteBlockOrIndented(condition.WhenFalse);
                WriteLine();
                break;

            case ConstantExpression constant:
                Write(constant.Value switch
                {
                    string str => $"\"{str}\"",
                    object obj => obj.ToString() ?? "",
                    null => "null"
                });
                break;

            case TypeArgumentsExpression construct:
                Write(construct.Expression);
                Write("<");
                for (int i = 0; i < construct.TypeArguments.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    Write(construct.TypeArguments[i]);
                }
                Write(">");
                break;

            case ConvertExpression convert:
                Write("Convert(");
                Write(convert.Expression);
                if (convert.ConvertedType != null)
                {
                    Write(", ");
                    Write(convert.ConvertedType);
                }
                else if (convert.ResultType != null)
                {
                    Write(", ");
                    Write(convert.ResultType.FullName);
                }
                Write(")");
                break;

            case LabelExpression label:
                Write(label.Name);
                Write(":");
                break;

            case LambdaExpression lambda:
                if (lambda.Body is BlockExpression)
                    Write("function ");
                Write("(");
                for (int i = 0; i < lambda.Parameters.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    Write(lambda.Parameters[i].Name);
                }
                Write(")");
                if (lambda.Body is not BlockExpression)
                    Write(" => ");
                Write(lambda.Body);
                break;

            case LoopExpression loop:
                Write("loop");
                WriteLine();
                WriteBlockOrIndented(loop.Body);
                break;

            case MemberExpression member:
                Write(member.Expression);
                Write(".");
                Write(member.Name);
                break;

            case NameReferenceExpression nameRef:
                Write(nameRef.Name);
                break;

            case SymbolReferenceExpression symbolRef:
                Write(symbolRef.FullName);
                break;

            case VariableExpression declaration:
                Write("var ");
                Write(declaration.Name);
                if (declaration.VariableType != null)
                {
                    Write(": ");
                    Write(declaration.VariableType);
                }
                if (declaration.Initializer != null)
                {
                    Write(" = ");
                    Write(declaration.Initializer);
                }
                break;


            case NamespaceDeclaration nd:
                Write("namespace ");
                WriteLine(nd.Name);
                WriteIndented(() =>
                {
                    foreach (var decl in nd.Declarations)
                    {
                        WriteLine(decl);
                    }
                });
                break;

            case ClassDeclaration cd:
                WriteAccessAndModifiers(cd.Access, cd.Modifiers);
                Write("class ");
                WriteLine(cd.Name);
                WriteIndented(() =>
                {
                    foreach (var decl in cd.Declarations)
                    {
                        WriteLine(decl);
                    }
                });
                break;

            case ConstructorDeclaration cd:
                WriteAccessAndModifiers(cd.Access, cd.Modifiers);
                Write("constructor ");
                Write("(");
                for (int i = 0; i < cd.Parameters.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    Write(cd.Parameters[i]);
                }
                WriteLine(")");
                WriteIndented(cd.Body);
                break;

            case MethodDeclaration md:
                WriteAccessAndModifiers(md.Access, md.Modifiers);
                Write("method ");
                Write(md.Name);
                Write("(");
                for (int i = 0; i < md.Parameters.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    Write(md.Parameters[i]);
                }
                Write("): ");
                Write(md.ReturnType);
                WriteLine();
                WriteIndented(md.Body);
                break;

            case ParameterDeclaration pd:
                Write(pd.Name);
                if (pd.ParameterType != null)
                {
                    Write(": ");
                    Write(pd.ParameterType);
                }
                break;
            case FieldDeclaration fd:
                WriteAccessAndModifiers(fd.Access, fd.Modifiers);
                Write("field ");
                Write(fd.Name);
                Write(" : ");
                Write(fd.FieldType);
                if (fd.Initializer != null)
                {
                    Write(" = ");
                    Write(fd.Initializer);
                }
                break;

            case PropertyDeclaration pd:
                WriteAccessAndModifiers(pd.Access, pd.Modifiers);
                Write("property ");
                Write(pd.Name);
                Write(" : ");
                WriteLine(pd.PropertyType);
                WriteIndented(() =>
                {
                    Write("get ");
                    Write(pd.GetMethod);
                    if (pd.SetMethod != null)
                    {
                        Write("set ");
                        Write(pd.SetMethod);
                    }
                });
                break;

            default:
                throw new InvalidOperationException($"Unhandled semantic type '{semantic.GetType().Name}' in {nameof(SemanticWriter)}.Write");
        }
    }

    private void WriteAccessAndModifiers(SymbolAccess access, SymbolModifier modifiers)
    {
        Write(access switch
        {
            SymbolAccess.Public => "public ",
            SymbolAccess.Internal => "internal ",
            SymbolAccess.Protected => "protected ",
            SymbolAccess.ProtectedAndInternal => "protectedAndInternal ",
            SymbolAccess.ProtectedOrInternal => "protectedOrInternal ",
            _ => ""
        });

        if (modifiers != SymbolModifier.None)
        {
            if ((modifiers & SymbolModifier.Static) != 0)
                Write("static ");

            if ((modifiers & SymbolModifier.Abstract) != 0)
                Write("abstract ");

            if ((modifiers & SymbolModifier.Virtual) != 0)
                Write("virtual ");

            if ((modifiers & SymbolModifier.Sealed) != 0)
                Write("sealed ");

            if ((modifiers & SymbolModifier.ReadOnly) != 0)
                Write("readonly ");

            if ((modifiers & SymbolModifier.Special) != 0)
                Write("special ");

            if ((modifiers & SymbolModifier.HideBySig) != 0)
                Write("hidden ");
        }
    }
}
