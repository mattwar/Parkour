namespace Parkour.Expressions;
using Symbols;

public class ExpressionWriter
{
    private TextWriter _writer;
    private readonly string _indentation;
    private string _currentIndentation;
    private bool _needsIndentation;

    public ExpressionWriter(TextWriter writer, string? indentation = null)
    {
        _writer = writer;
        _indentation = indentation ?? "  ";
        _currentIndentation = "";
        _needsIndentation = false;
    }

    public ExpressionWriter(string? indentation = null)
        : this(new StringWriter(), indentation)
    {
    }

    public string WriteExpression(Expression expression)
    {
        Write(expression);
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

    private void WriteIndented(Expression expression)
    {
        var oldIndentation = _currentIndentation;
        _currentIndentation += _indentation;
        Write(expression);
        _currentIndentation = oldIndentation;
    }

    private void WriteBlockOrIndented(Expression expression)
    {
        if (expression is BlockExpression block)
        {
            Write(expression);
        }
        else
        {
            WriteIndented(expression);
        }
    }

    private void Write(Expression expression)
    {
        switch (expression)
        {
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
                    Write(branch.TargetName);
                }

                if (branch.Expression != null)
                {
                    Write(" ");
                    Write(branch.Expression);
                }
                break;

            case CallExpression call:
                if (call.CalledSymbol is FunctionSymbol fn)
                {
                    if (call.Expression is PathExpression path)
                    {
                        Write(path.Expression);
                        Write(".");
                    }
                    else if (call.Expression is ReferenceExpression rex)
                    {
                        Write(call.CalledSymbol.Name);
                    }
                    else
                    {
                        Write(call.Expression);
                    }
                }
                else
                {
                    Write(call.Expression);
                }
                Write("(");
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    WriteExpression(call.Arguments[i]);
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

            case ConvertExpression convert:
                Write("Convert(");
                Write(convert.Expression);
                Write(", ");
                Write(convert.ConvertedType.Name);
                Write(")");
                break;

            case FunctionExpression function:
                if (function.Body is BlockExpression)
                    Write("function ");
                Write("(");
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    if (i > 0)
                        Write(", ");
                    Write(function.Parameters[i].Name);
                }
                Write(")");
                if (function.Body is not BlockExpression)
                    Write(" => ");
                Write(function.Body);
                break;

            case DeclarationExpression declaration:
                Write("var ");
                Write(declaration.Name);
                Write(" = ");
                Write(declaration.Initializer);
                break;

            case PathExpression path:
                Write(path.Expression);
                Write(".");
                Write(path.Reference);
                break;

            case ReferenceExpression rex:
                Write(rex.Name);
                break;

            case WhileExpression whilst:
                Write("while (");
                Write(whilst.Test);
                Write(")");
                WriteLine();
                WriteBlockOrIndented(whilst.Body);
                break;

            default:
                throw new InvalidOperationException($"Unhandled expression kind '{expression.GetType().Name}' in {nameof(ExpressionWriter)}.Write");
        }
    }
}
