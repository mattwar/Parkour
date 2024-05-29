namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Writes elements into a generalized text form. 
/// Used for debugging.
/// </summary>
public class SemanticWriter
{
    private TextWriter _writer;
    private readonly string _indentation;
    private string _currentIndentation;
    private bool _needsIndentation;
    private bool _needsSeparation;

    public SemanticWriter(TextWriter writer, string? indentation = null)
    {
        _writer = writer;
        _indentation = indentation ?? "  ";
        _currentIndentation = "";
        _needsIndentation = false;
        _needsSeparation = false;
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
        _needsSeparation = false;
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

        _needsIndentation = text.Length > 0 && IsEndOfLine(text[^1]);
        _needsSeparation = text.Length > 0 && !_needsIndentation && !IsSeparation(text[^1]);
    }

    private static bool IsEndOfLine(char ch) =>
        ch == '\r' || ch == '\n';

    private static bool IsSeparation(char ch) =>
        char.IsWhiteSpace(ch)
        || IsEndOfLine(ch);

    private void WriteSeparated(string text)
    {
        if (_needsSeparation)
        {
            Write(" ");
            _needsSeparation = false;
        }

        Write(text);
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

    private void WriteSeparated(SemanticElement semantic)
    {
        Write("");
        Write(semantic);
    }

    private void WriteCommaList<TElement>(ImmutableList<TElement> elements)
        where TElement : SemanticElement
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (i > 0)
                Write(", ");
            Write(elements[i]);
        }
    }

    private void WriteSemicolonLineSeparated<TElement>(ImmutableList<TElement> elements)
        where TElement : SemanticElement
    {
        for (int i = 0; i < elements.Count; i++)
        {
            Write(elements[i]);
            if (i < elements.Count - 1)
                WriteLine(";");
        }
    }

    private void WriteLineSeparated<TElement>(ImmutableList<TElement> elements)
        where TElement : SemanticElement
    {
        foreach (var element in elements)
        {
            WriteLine(element);
        }
    }

    private void WriteType(TypeSymbol? typeSymbol, Expression? typeExpression)
    {
        if (typeSymbol != null)
        {
            Write(typeSymbol.FullName);
        }
        else if (typeExpression != null)
        {
            Write(typeExpression);
        }
        else
        {
            Write("<unknown>");
        }
    }

    private void Write(SemanticElement semantic)
    {
        switch (semantic)
        {
            case ArityExpression arity:
                Write(arity.TypeOrMember);
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
                    WriteSemicolonLineSeparated(block.Expressions);
                });
                WriteLine();
                WriteLine("}");
                break;

            case BranchExpression branch:
                if (branch.IsBreak)
                {
                    WriteSeparated("break");
                }
                else if (branch.IsContinue)
                {
                    WriteSeparated("continue");
                }
                else if (branch.IsReturn)
                {
                    WriteSeparated("return");
                }
                else
                {
                    WriteSeparated("goto");
                    WriteSeparated(branch.LabelName);
                }

                if (branch.Expression != null)
                {
                    WriteSeparated(branch.Expression);
                }
                break;

            case CallExpression call:
                WriteSeparated(call.Expression);
                Write("(");
                WriteCommaList(call.Arguments);
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
                WriteSeparated(constant.Value switch
                {
                    string str => $"\"{str}\"",
                    object obj => obj.ToString() ?? "",
                    null => "null"
                });
                break;

            case ConstructExpression construct:
                WriteSeparated(construct.TypeOrMember);
                Write("<");
                WriteCommaList(construct.TypeArguments);
                Write(">");
                break;

            case ConvertExpression convert:
                WriteSeparated("Convert(");
                Write(convert.Expression);
                Write(", ");
                WriteType(convert.ResultType, convert.ConvertedType);
                Write(")");
                break;

            case LabelExpression label:
                WriteSeparated(label.Name);
                Write(":");
                break;

            case LambdaExpression lambda:
                if (lambda.Body is BlockExpression)
                    WriteSeparated("function ");
                Write("(");
                WriteCommaList(lambda.Parameters);
                Write(")");
                if (lambda.Body is not BlockExpression)
                    Write(" => ");
                Write(lambda.Body);
                break;

            case LoopExpression loop:
                WriteSeparated("loop");
                WriteLine();
                WriteBlockOrIndented(loop.Expression);
                break;

            case MemberExpression member:
                WriteSeparated(member.Instance);
                Write(".");
                Write(member.Name);
                break;

            case NameExpression nameRef:
                WriteSeparated(nameRef.Name);
                break;

            case SymbolExpression symbolRef:
                WriteSeparated(symbolRef.Name);
                break;

            case VariableExpression declaration:
                WriteSeparated("var");
                WriteSeparated(declaration.Name);
                if (declaration.VariableType != null)
                {
                    Write(":");
                    WriteSeparated(declaration.VariableType);
                }
                if (declaration.Initializer != null)
                {
                    WriteSeparated("=");
                    WriteSeparated(declaration.Initializer);
                }
                break;

            case NamespaceDeclaration nd:
                WriteSeparated("namespace");
                WriteSeparated(nd.Name);
                WriteLine();
                WriteIndented(() =>
                {
                    foreach (var decl in nd.Declarations)
                    {
                        WriteLine(decl);
                    }
                });
                break;

            case ClassDeclaration cd:
                WriteTypeDeclaration(cd, "class");
                break;

            case ConstructorDeclaration cd:
                WriteAccessAndModifiers(cd.Access, cd.Modifiers);
                WriteSeparated("constructor");
                WriteSeparated("(");
                WriteCommaList(cd.Parameters);
                WriteLine(")");
                WriteLine("{");
                WriteIndented(cd.Body);
                WriteLine("}");
                break;

            case DelegateDeclaration dd:
                WriteAccessAndModifiers(dd.Access, dd.Modifiers);
                WriteSeparated("delegate");
                WriteSeparated(dd.Name);
                WriteSeparated("(");
                WriteCommaList(dd.Parameters);
                Write("):");
                WriteType(dd.Symbol?.ReturnType, dd.ReturnType);
                break;

            case FieldDeclaration fd:
                WriteAccessAndModifiers(fd.Access, fd.Modifiers);
                WriteSeparated("field");
                WriteSeparated(fd.Name);
                WriteSeparated(":");
                WriteType(fd.Symbol?.Type, fd.FieldType);
                if (fd.Initializer != null)
                {
                    WriteSeparated("=");
                    WriteSeparated(fd.Initializer);
                }
                break;

            case IndexerDeclaration id:
                WriteAccessAndModifiers(id.Access, id.Modifiers);
                WriteSeparated("indexer");
                WriteSeparated(id.Name);
                Write(":");
                WriteType(id.Symbol?.ElementType, id.ElementType);
                WriteLine();
                WriteIndented(() =>
                {
                    WriteSeparated("get");
                    WriteSeparated(id.GetMethod);
                    if (id.SetMethod != null)
                    {
                        WriteSeparated("set");
                        WriteSeparated(id.SetMethod);
                    }
                });
                break;

            case InterfaceDeclaration td:
                WriteTypeDeclaration(td, "interface");
                break;

            case MethodDeclaration md:
                WriteAccessAndModifiers(md.Access, md.Modifiers);
                WriteSeparated("method");
                WriteSeparated(md.Name);
                Write("(");
                WriteCommaList(md.Parameters);
                Write("):");
                WriteType(md.Symbol?.ReturnType, md.ReturnType);
                WriteLine();
                if (md.Body != null)
                {
                    WriteLine("{");
                    WriteIndented(md.Body);
                    WriteLine("}");
                }
                else
                {
                    WriteLine(";");
                }
                break;

            case ParameterDeclaration pd:
                WriteModifiers(pd.Modifiers);
                WriteSeparated(pd.Name);
                if (pd.ParameterType != null)
                {
                    WriteSeparated(":");
                    WriteType(pd.Symbol?.Type, pd.ParameterType);
                }
                break;

            case PropertyDeclaration pd:
                WriteAccessAndModifiers(pd.Access, pd.Modifiers);
                WriteSeparated("property");
                WriteSeparated(pd.Name);
                WriteSeparated(":");
                WriteType(pd.Symbol?.Type, pd.PropertyType);
                WriteLine();
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

            case StructDeclaration sd:
                WriteTypeDeclaration(sd, "struct");
                break;

            default:
                throw new InvalidOperationException($"Unhandled semantic type '{semantic.GetType().Name}' in {nameof(SemanticWriter)}.Write");
        }
    }

    private void WriteTypeDeclaration(TypeDeclaration td, string kind)
    {
        WriteAccessAndModifiers(td.Access, td.Modifiers);
        WriteSeparated(kind);
        WriteSeparated(td.Name);
        if (td.BaseTypes.Count > 0)
        {
            WriteSeparated(":");
            WriteCommaList(td.BaseTypes);
            WriteLine();
        }
        else
        {
            WriteLine();
        }
        WriteLine("{");
        WriteIndented(() =>
        {
            WriteLineSeparated(td.Declarations);
        });
        WriteLine("}");
    }

    private void WriteAccessAndModifiers(SymbolAccess access, BitSet<SymbolModifier> modifiers)
    {
        WriteAccess(access);
        WriteModifiers(modifiers);
    }

    private void WriteAccess(SymbolAccess access)
    {
        WriteSeparated(access.ToString().ToLower());
    }

    private void WriteModifiers(BitSet<SymbolModifier> modifiers)
    {
        if (modifiers != SymbolModifier.None)
        {
            foreach (var mod in modifiers.Select(m => m.ToString().ToLower()).OrderBy(x => x))
            {
                WriteSeparated(mod);
            }
        }
    }
}
