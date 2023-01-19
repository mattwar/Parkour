namespace Parkour.Parsers;


public sealed class OperatorsParser<TInput, TOperand>
    : Parser<TInput, TOperand>
{
    private readonly Parser<TInput, TOperand> _primaryParser;
    private readonly Parser<TInput, TOperand> _secondaryParser;
    private readonly PrefixCase[] _prefixCases;
    private readonly PostfixCase[] _postfixCases;
    private readonly InfixCase[] _infixCases;

    public OperatorsParser(
        Parser<TInput, TOperand> primaryParser,
        Parser<TInput, TOperand>? secondaryParser,
        Action<OperatorBuilder> fnBuildOperators)
    {
        _primaryParser = primaryParser;
        _secondaryParser = secondaryParser ?? primaryParser;

        var operators = new List<OperatorCase>();
        fnBuildOperators(new OperatorBuilder(0, operators));

        _prefixCases = operators.OfType<PrefixCase>().ToArray();
        _postfixCases = operators.OfType<PostfixCase>().ToArray();
        _infixCases = operators.OfType<InfixCase>().ToArray();
    }

    public struct OperatorBuilder
    {
        private readonly int _level;
        private readonly List<OperatorCase> _cases;

        internal OperatorBuilder(int level, List<OperatorCase> cases)
        {
            _level = level;
            _cases = cases;
        }

        /// <summary>
        /// Adds an infix operator at the current precedence level.
        /// </summary>
        public OperatorBuilder Infix<TOperator>(Parser<TInput, TOperator> operatorParser, Func<TOperand, TOperator, TOperand, TOperand> fnProducer)
        {
            _cases.Add(new OperatorsParser<TInput, TOperand>.InfixCase(_level, operatorParser, (left, op, right) => fnProducer(left, (TOperator)op, right)));
            return this;
        }

        /// <summary>
        /// Adds an prefix operator at the current precedence level.
        /// </summary>
        public OperatorBuilder Prefix<TOperator>(Parser<TInput, TOperator> operatorParser, Func<TOperator, TOperand, TOperand> fnProducer)
        {
            _cases.Add(new OperatorsParser<TInput, TOperand>.PrefixCase(_level, operatorParser, (op, right) => fnProducer((TOperator)op, right)));
            return this;
        }

        /// <summary>
        /// Adds an postfix operator at the current precedence level.
        /// </summary>
        public OperatorBuilder Postfix<TOperator>(Parser<TInput, TOperator> operatorParser, Func<TOperand, TOperator, TOperand> fnProducer)
        {
            _cases.Add(new OperatorsParser<TInput, TOperand>.PostfixCase(_level, operatorParser, (left, op) => fnProducer(left, (TOperator)op)));
            return this;
        }

        /// <summary>
        /// Operators added after the builde returned by this method will be at a lower precedence level.
        /// </summary>
        public OperatorBuilder Lower()
        {
            return new OperatorBuilder(_level + 1, _cases);
        }
    }

    internal abstract record OperatorCase(int Level);

    private sealed record PrefixCase(
        int Level,
        Parser<TInput> OperatorParser,
        Func<object, TOperand, TOperand> Producer) : OperatorCase(Level);

    private sealed record InfixCase(
        int Level,
        Parser<TInput> OperatorParser,
        Func<TOperand, object, TOperand, TOperand> Producer) : OperatorCase(Level);

    private sealed record PostfixCase(
        int Level,
        Parser<TInput> OperatorParser,
        Func<TOperand, object, TOperand> Producer) : OperatorCase(Level);

    private record struct OperatorInfo(object Operator, OperatorCase Case);

    public override bool Parse(ReadOnlySpan<TInput> input, out TOperand output, out ReadOnlySpan<TInput> remainingInput)
    {
        var operandStack = new Stack<TOperand>();
        var operatorStack = new Stack<OperatorInfo>();

        remainingInput = input;
        TOperand operand = default!;
        var operandParser = _primaryParser;

    nextPrefix:
        // look for prefix operators
        foreach (var prefixCase in _prefixCases)
        {
            if (prefixCase.OperatorParser.ParseAsObject(remainingInput, out var op, out remainingInput))
            {
                operatorStack.Push(new OperatorInfo(op, prefixCase));
                operandParser = _secondaryParser;
                goto nextPrefix;
            }
        }

        if (operandParser.Parse(remainingInput, out operand, out remainingInput))
        {
            operandStack.Push(operand);

        nextInfixOrPostfix:
            // look for infix operators
            foreach (var infixCase in _infixCases)
            {
                if (infixCase.OperatorParser.ParseAsObject(remainingInput, out var op, out remainingInput))
                {
                    Fold(infixCase.Level);
                    operatorStack.Push(new OperatorInfo(op, infixCase));
                    operandParser = _secondaryParser;
                    goto nextPrefix;
                }
            }
            // look for postfix operators
            foreach (var postfixCase in _postfixCases)
            {
                if (postfixCase.OperatorParser.ParseAsObject(remainingInput, out var op, out remainingInput))
                {
                    operatorStack.Push(new OperatorInfo(op, postfixCase));
                    Fold(postfixCase.Level);
                    goto nextInfixOrPostfix;
                }
            }
        }
        else 
        {
            output = default!;
            remainingInput = input;
            return false;
        }

        // fold any remaining operators
        Fold(int.MaxValue);
        output = operandStack.Pop();
        return true;

        void Fold(int currentOpLevel)
        {
            while (operatorStack!.Count > 0
                && operatorStack.Peek().Case.Level <= currentOpLevel)
            {
                var operatorInfo = operatorStack.Pop();
                switch (operatorInfo.Case)
                {
                    case PrefixCase prefixCase:
                        operandStack.Push(prefixCase.Producer(operatorInfo.Operator, operandStack.Pop()));
                        break;
                    case InfixCase infixCase:
                        var rightOperand = operandStack.Pop();
                        var leftOperand = operandStack.Pop();
                        var result = infixCase.Producer(leftOperand, operatorInfo.Operator, rightOperand);
                        operandStack.Push(result);
                        break;
                    case PostfixCase postfixCase:
                        operandStack.Push(postfixCase.Producer(operandStack.Pop(), operatorInfo.Operator));
                        break;
                }
            }
        }
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        remainingInput = input;
        var operandParser = _primaryParser;

    nextPrefix:
        // look for prefix operators
        foreach (var prefixCase in _prefixCases)
        {
            if (prefixCase.OperatorParser.Scan(remainingInput, out remainingInput))
            {
                operandParser = _secondaryParser;
                goto nextPrefix;
            }
        }

        if (operandParser.Scan(remainingInput, out remainingInput))
        {
        nextInfixOrPostfix:
            // look for infix operators
            foreach (var infixCase in _infixCases)
            {
                if (infixCase.OperatorParser.Scan(remainingInput, out remainingInput))
                {
                    operandParser = _secondaryParser;
                    goto nextPrefix;
                }
            }
            // look for postfix operators
            foreach (var postfixCase in _postfixCases)
            {
                if (postfixCase.OperatorParser.Scan(remainingInput, out remainingInput))
                {
                    goto nextInfixOrPostfix;
                }
            }
        }
        else
        {
            remainingInput = input;
            return false;
        }

        return true;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        var initialAfterMissing = afterMissing;
        fnCallback(this, input, afterMissing);

        remainingInput = input;
        var operandParser = _primaryParser;

    nextPrefix:
        // look for prefix operators
        var beforeMissing = afterMissing;
        foreach (var prefixCase in _prefixCases)
        {
            afterMissing = beforeMissing;
            if (prefixCase.OperatorParser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
            {
                operandParser = _secondaryParser;
                goto nextPrefix;
            }
        }

        if (operandParser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
        {
        nextInfixOrPostfix:
            beforeMissing = afterMissing;

            // look for infix operators
            foreach (var infixCase in _infixCases)
            {
                afterMissing = beforeMissing;
                if (infixCase.OperatorParser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
                {
                    operandParser = _secondaryParser;
                    goto nextPrefix;
                }
            }

            // look for postfix operators
            foreach (var postfixCase in _postfixCases)
            {
                afterMissing = beforeMissing;
                if (postfixCase.OperatorParser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
                {
                    goto nextInfixOrPostfix;
                }
            }
        }
        else
        {
            remainingInput = input;
            afterMissing = initialAfterMissing;
            return false;
        }

        return true;
    }
}