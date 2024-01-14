namespace Parkour.Services;

public abstract class CompilationLanguageService : LanguageService
{
    public CompilationLanguageService()
    {
    }

    private Compilation? _compilation;

    public Compilation GetCompilation()
    {
        if (_compilation == null)
        {
            var tmp = CreateCompilation();
            Interlocked.CompareExchange(ref _compilation, tmp, null);
        }

        return _compilation!;
    }

    protected abstract Compilation CreateCompilation();

    private CompletionService? _completion;

    public override CompletionService Completion
    {
        get
        {
            if (_completion == null)
            {
                var tmp = CreateCompletionService();
                Interlocked.CompareExchange(ref _completion, tmp, null);
            }

            return _completion;
        }
    }

    protected virtual CompletionService CreateCompletionService()
    {
        return new CompilationCompletionService(this);
    }
}
