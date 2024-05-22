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

    public override ParseResult<TOperand> Parse(ReadOnlySpan<TInput> input)
    {
        var operandStack = new Stack<TOperand>();
        var operatorStack = new Stack<OperatorInfo>();

        var remainingInput = input;
        var operandParser = _primaryParser;

    nextPrefix:
        // look for prefix operators
        foreach (var prefixCase in _prefixCases)
        {
            var prefixResult = prefixCase.OperatorParser.ParseAsObject(remainingInput);
            if (prefixResult.Success)
            {
                remainingInput = remainingInput.Slice(prefixResult.Length);
                operatorStack.Push(new OperatorInfo(prefixResult.Output, prefixCase));
                operandParser = _secondaryParser;
                goto nextPrefix;
            }
        }

        var operandResult = operandParser.Parse(remainingInput);
        if (operandResult.Success)
        {
            remainingInput = remainingInput.Slice(operandResult.Length);
            operandStack.Push(operandResult.Output);

        nextInfixOrPostfix:
            // look for infix operators
            foreach (var infixCase in _infixCases)
            {
                var infixResult = infixCase.OperatorParser.ParseAsObject(remainingInput);
                if (infixResult.Success)
                {
                    remainingInput = remainingInput.Slice(infixResult.Length);
                    Fold(infixCase.Level);
                    operatorStack.Push(new OperatorInfo(infixResult.Output, infixCase));
                    operandParser = _secondaryParser;
                    goto nextPrefix;
                }
            }
            // look for postfix operators
            foreach (var postfixCase in _postfixCases)
            {
                var postfixResult = postfixCase.OperatorParser.ParseAsObject(remainingInput);
                if (postfixResult.Success)
                {
                    remainingInput = remainingInput.Slice(postfixResult.Length);
                    operatorStack.Push(new OperatorInfo(postfixResult.Output, postfixCase));
                    Fold(postfixCase.Level);
                    goto nextInfixOrPostfix;
                }
            }
        }
        else 
        {
            return default;
        }

        // fold any remaining operators
        Fold(int.MaxValue);
        var output = operandStack.Pop();
        return new ParseResult<TOperand>(true, input.Length - remainingInput.Length, output);

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

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var remainingInput = input;
        var operandParser = _primaryParser;

    nextPrefix:
        // look for prefix operators
        foreach (var prefixCase in _prefixCases)
        {
            var prefixResult = prefixCase.OperatorParser.Scan(remainingInput);
            if (prefixResult.Success)
            {
                remainingInput = remainingInput.Slice(prefixResult.Length);
                operandParser = _secondaryParser;
                goto nextPrefix;
            }
        }

        var operandResult = operandParser.Scan(remainingInput);
        if (operandResult.Success)
        {
            remainingInput = remainingInput.Slice(operandResult.Length);
        
        nextInfixOrPostfix:
            // look for infix operators
            foreach (var infixCase in _infixCases)
            {
                var infixResult = infixCase.OperatorParser.Scan(remainingInput);
                if (infixResult.Success)
                {
                    remainingInput = remainingInput.Slice(infixResult.Length);
                    operandParser = _secondaryParser;
                    goto nextPrefix;
                }
            }
            // look for postfix operators
            foreach (var postfixCase in _postfixCases)
            {
                var prefixResult = postfixCase.OperatorParser.Scan(remainingInput);
                if (prefixResult.Success)
                {
                    remainingInput = remainingInput.Slice(prefixResult.Length);
                    goto nextInfixOrPostfix;
                }
            }
        }
        else
        {
            return default;
        }

        return new ScanResult(true, input.Length - remainingInput.Length);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var remainingInput = input;
        var operandParser = _primaryParser;

    nextPrefix:
        // look for prefix operators
        foreach (var prefixCase in _prefixCases)
        {
            var prefixResult = prefixCase.OperatorParser.Search(remainingInput, afterMissing, fnCallback);
            if (prefixResult.Success)
            {
                remainingInput = remainingInput.Slice(prefixResult.Length);
                afterMissing = prefixResult.AfterMissing;
                operandParser = _secondaryParser;
                goto nextPrefix;
            }
        }

        var operandResult = operandParser.Search(remainingInput, afterMissing, fnCallback);
        if (operandResult.Success)
        {
            remainingInput = remainingInput.Slice(operandResult.Length);
            afterMissing = operandResult.AfterMissing;

        nextInfixOrPostfix:
            // look for infix operators
            foreach (var infixCase in _infixCases)
            {
                var infixResult = infixCase.OperatorParser.Search(remainingInput, afterMissing, fnCallback);
                if (infixResult.Success)
                {
                    remainingInput = remainingInput.Slice(infixResult.Length);
                    afterMissing = infixResult.AfterMissing;
                    operandParser = _secondaryParser;
                    goto nextPrefix;
                }
            }

            // look for postfix operators
            foreach (var postfixCase in _postfixCases)
            {
                var postfixResult = postfixCase.OperatorParser.Search(remainingInput, afterMissing, fnCallback);
                if (postfixResult.Success)
                {
                    remainingInput = remainingInput.Slice(postfixResult.Length);
                    afterMissing = postfixResult.AfterMissing;
                    goto nextInfixOrPostfix;
                }
            }
        }
        else
        {
            return default;
        }

        return new SearchResult(true, input.Length - remainingInput.Length, afterMissing);
    }
}