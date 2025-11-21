using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Parkour.Syntax;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract record SyntaxElement(Diagnostic? Diagnostic)
    : ISyntaxElement, ISourceLocation
{
    /// <summary>
    /// The text displayed in the debugger for this element.
    /// </summary>
    private string DebugText => $"{GetType().Name}: {ToString(0, 20)}";

    /// <summary>
    /// Either the parent <see cref="SyntaxNode"/> or containing <see cref="SyntaxTree"/>
    /// </summary>
    private object? _parent;

    /// <summary>
    /// The starting character offset within the parent's total text length.
    /// </summary>
    private int _offsetInParent;

    /// <summary>
    /// The child index within the parent's set of children.
    /// </summary>
    private int _indexInParent;

    /// <summary>
    /// The kind of this syntax element.
    /// </summary>
    public virtual string Kind => this.GetType().Name;

    /// <summary>
    /// The starting character position of this element within the source text.
    /// </summary>
    private int _start = -1;

    /// <summary>
    /// The text position this element including starting trivia.
    /// </summary>
    public int Start
    {
        get
        {
            if (_start == -1 && this.Parent != null)
            {
                // note: this is recursive (thus dangerous) but only happens while element is unfrozen.
                // Assignment to tree will freeze.
                return this.Parent.Start + _offsetInParent;
            }

            return _start == -1 ? 0 : _start;
        }
    }

    /// <summary>
    /// The position of the first non-trivia character of this element within the source text.
    /// </summary>
    public virtual int TextStart =>
        this.Start + (GetFirstToken() is SyntaxToken token ? token.Trivia.Length : 0);

    /// <summary>
    /// The character length of the starting trivia associated with this element.
    /// </summary>
    public int TriviaLength => TextStart - Start;

    /// <summary>
    /// The character length of the element less the starting trivia length
    /// </summary>
    public int TextLength => Length - TriviaLength;

    /// <summary>
    /// The character position just after the last character of this element.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// The character length of the entire element (including trivia).
    /// </summary>
    public virtual int Length =>
        GetChildInfo().Length;

    /// <summary>
    /// True if this element was considered missing when parsed.
    /// </summary>
    public bool IsMissing => Diagnostic != null && TextLength == 0;

    /// <summary>
    /// The parent element, or null when this the the root of the tree.
    /// </summary>
    public SyntaxNode? Parent => _parent as SyntaxNode;

    /// <summary>
    /// The number of child elements this element contains.
    /// </summary>
    public virtual int ChildCount =>
        GetChildInfo().Children.Count;

    /// <summary>
    /// Returns the child element at the specified index.
    /// </summary>
    public virtual SyntaxElement? GetChild(int index) =>
        GetChildInfo().Children[index];

    private ChildInfo GetChildInfo()
    {
        if (!_instanceToChildInfoMap.TryGetValue(this, out var info))
        {
            info = _instanceToChildInfoMap.GetOrAdd(this, _me => new ChildInfo(GetChildAccessors(), _me));
        }
        return info;
    }

    private class ChildInfo
    {
        public int Length { get; }
        public IReadOnlyList<SyntaxElement?> Children { get; }

        public ChildInfo(IReadOnlyList<Func<object, SyntaxElement?>> accessors, SyntaxElement element)
        {
            var children = new List<SyntaxElement?>();
            var length = 0;
            foreach (var acc in accessors)
            {
                var child = acc(element);
                children.Add(child);
                if (child != null)
                {
                    if (element is SyntaxNode node)
                    {
                        child.SetParent(node, length, children.Count);
                    }

                    length += child.Length;
                }
            }
            this.Children = children.AsReadOnly();
            this.Length = length;
        }
    }

    private static readonly ConditionalWeakTable<SyntaxElement, ChildInfo> _instanceToChildInfoMap =
        new ConditionalWeakTable<SyntaxElement, ChildInfo>();


    private static readonly ConditionalWeakTable<Type, IReadOnlyList<Func<object, SyntaxElement?>>> _typeToAccessorsMap =
        new ConditionalWeakTable<Type, IReadOnlyList<Func<object, SyntaxElement?>>>();

    private IReadOnlyList<Func<object, SyntaxElement?>> GetChildAccessors()
    {
        var type = this.GetType();

        if (!_typeToAccessorsMap.TryGetValue(type, out var accessors))
        {
            accessors = _typeToAccessorsMap.GetOrAdd(type, _type => CreateAccessors(_type));
        }
        return accessors;

        static IReadOnlyList<Func<object, SyntaxElement?>> CreateAccessors(Type type)
        {
            var accessors = new List<Func<object, SyntaxElement?>>();

            // get primary constructor..
            var primaryConstructor = type.GetConstructors()
                .First(c => !c.IsStatic && c.GetParameters().Length > 0);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p);

            foreach (var param in primaryConstructor.GetParameters())
            {
                if (param.ParameterType.IsAssignableTo(typeof(SyntaxElement))
                    && props.TryGetValue(param.Name!, out var prop))
                {
                    // Consider: RefEmit this for speed?
                    accessors.Add(obj => prop.GetValue(obj) as SyntaxElement);
                }
            }

            return accessors.AsReadOnly();
        }
    }

    /// <summary>
    /// The count of text characters from the start of the parent element.
    /// </summary>
    public int OffsetInParent => _offsetInParent;

    /// <summary>
    /// The index of this element within the parent node's collection.
    /// </summary>
    public int IndexInParent => _indexInParent;

    /// <summary>
    /// The root element of the syntax tree.
    /// </summary>
    public SyntaxElement? Root
    {
        get
        {
            var elem = this;           
            while (elem != null && elem.Parent != null)
            {
                elem = elem.Parent;
            }

            return elem;
        }
    }

    /// <summary>
    /// The <see cref="SyntaxTree"/> containing this <see cref="SyntaxElement"/>
    /// </summary>
    public SyntaxTree? Tree
    {
        get
        {
            var elem = this;
            while (elem != null && elem._parent != null)
            {
                if (elem._parent is SyntaxTree tree)
                    return tree;
                elem = elem.Parent;
            }

            return null;
        }
    }

    /// <summary>
    /// Assigns the parent node and other facts to this element.
    /// </summary>
    internal virtual void SetParent(SyntaxNode parent, int offsetInParent, int indexInParent)
    {
        if (_start == -1)
        {
            _parent = parent;
            _offsetInParent = offsetInParent;
            _indexInParent = indexInParent;
        }
        else
        {
            throw new InvalidOperationException($"The element already has a parent");
        }
    }

    /// <summary>
    /// Assigns the tree to this element.
    /// This element becomes the root of the tree.
    /// </summary>
    internal void SetTree(SyntaxTree tree)
    {
        if (_start == -1)
        {
            _parent = tree;
            _offsetInParent = 0;
            _indexInParent = 0;
            _start = 0;

            // walk element in parent-first lexical order and freeze each element in 
            // the current configuration
            SyntaxElement.WalkElements(this, fnBefore: element => element.Freeze());
        }
        else
        {
            throw new InvalidOperationException($"The element already the root of a tree");
        }
    }

    /// <summary>
    /// True if the tree is frozen.
    /// </summary>
    internal bool IsFrozen => _start >= 0;

    /// <summary>
    /// Freeze the element in the current configuration
    /// </summary>
    internal void Freeze()
    {
        // fix starting position..
        // This appears recursive, but parent should already be frozen.
        _start = (this.Parent?.Start ?? 0) + _offsetInParent;
    }

    /// <summary>
    /// Returns the text associated with this element and all its child elements.
    /// </summary>
    public override string ToString()
    {
        return ToString(0, Length);
    }

    /// <summary>
    /// Returns a range of the text that <see cref="ToString()"/> would return.
    /// </summary>
    public string ToString(int start, int length)
    {
        var builder = new StringBuilder();

        SyntaxToken? token;
        for (token = this.GetFirstToken(); token != null; token = token!.GetNextToken())
        {
            if (token.Start > start + length)
            {
                // token is after the range
                break;
            }
            else if (token.End < start)
            {
                // token is before the range
                continue;
            }
            else
            {
                // token overlaps the range
                AppendTextInRange(token.Start, token.Trivia);
                AppendTextInRange(token.TextStart, token.Text);

                void AppendTextInRange(int textStart, string text)
                {
                    if (text.Length > 0 && (textStart + TextLength > start || textStart < start + length))
                    {
                        var appendStart = Math.Min(Math.Max(start, textStart), Math.Max(start + length, textStart + text.Length)) - textStart;
                        var appendEnd = Math.Max(Math.Min(start, textStart), Math.Min(start + length, textStart + text.Length)) - textStart;
                        var appendLength = appendEnd - appendStart;
                        if (appendLength == text.Length)
                        {
                            builder.Append(text);
                        }
                        else
                        {
                            builder.Append(text, appendStart, appendLength);
                        }
                    }
                }
            }
        }

        return builder.ToString();
    }

    #region Navigation

    /// <summary>
    /// The depth of this node below the root.
    /// </summary>
    public int Depth
    {
        get
        {
            int depth = 0;

            for (var element = this; element.Parent != null; element = element.Parent)
            {
                depth++;
            }

            return depth;
        }
    }

    /// <summary>
    /// Gets the common ancestor between two elements a and b.
    /// </summary>
    public static SyntaxNode? GetCommonAncestor(SyntaxElement a, SyntaxElement b)
    {
        if (a == null || b == null)
            return null;

        var elemA = a;
        var elemB = b;

        var depthA = elemA.Depth;
        var depthB = elemB.Depth;

        while (elemA != null && depthA > depthB && depthA > 0)
        {
            elemA = elemA.Parent;
            depthA--;
        }

        while (elemB != null && depthB > depthA && depthB > 0)
        {
            elemB = elemB.Parent;
            depthB--;
        }

        if (elemA != null && elemB != null && depthA > 0 && elemA.Parent == elemB.Parent)
        {
            return elemA.Parent;
        }

        return null;
    }

    /// <summary>
    /// Gets the child node index for the subtree that the descendant is part of
    /// </summary>
    public int GetDescendantIndex(SyntaxElement descendant)
    {
        if (descendant == null)
            return -1;

        if (descendant.Parent != this)
        {
            var depth = this.Depth;
            var descendantDepth = descendant.Depth;

            if (depth <= descendantDepth)
                return -1;

            while (descendantDepth > depth + 1)
            {
                descendant = descendant.Parent!;
                descendantDepth--;
            }
        }

        if (descendant.Parent == this)
        {
            return descendant.IndexInParent;
        }
        else
        {
            return -1;
        }
    }

    /// <summary>
    /// Returns true if this element is the ancestor of the specified element.
    /// </summary>
    public bool IsAncestorOf(SyntaxElement element)
    {
        var elem = element;

        while (elem != null)
        {
            if (elem.Parent == this)
                return true;

            elem = elem.Parent;
        }

        return false;
    }

    /// <summary>
    /// Returns true if this element is the descendant of the specified element.
    /// </summary>
    public bool IsDescendantOf(SyntaxElement element)
    {
        return element.IsAncestorOf(this);
    }

    /// <summary>
    /// Gets the first ancestor of this element that matches the specified type and predicate.
    /// </summary>
    public TElement? GetFirstAncestor<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        for (SyntaxElement? elem = this.Parent; elem != null; elem = elem.Parent)
        {
            if (elem is TElement te && (predicate == null || predicate(te)))
            {
                return te;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the first ancestor of this element that matches the specified type and predicate.
    /// </summary>
    public SyntaxNode? GetFirstAncestor(Func<SyntaxNode, bool>? predicate = null) =>
        GetFirstAncestor<SyntaxNode>(predicate);

    /// <summary>
    /// Gets the first ancestor of this element (including itself) that matches the specified type and predicate.
    /// </summary>
    public TElement? GetFirstAncestorOrSelf<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        for (var elem = this; elem != null; elem = elem.Parent)
        {
            if (elem is TElement te && (predicate == null || predicate(te)))
            {
                return te;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the first ancestor of this element (including itself) that matches the specified type and predicate.
    /// </summary>
    public SyntaxElement? GetFirstAncestorOrSelf(Func<SyntaxElement, bool>? predicate = null) =>
        GetFirstAncestorOrSelf<SyntaxElement>(predicate);


    /// <summary>
    /// Gets the all ancestors of this element (including itself) that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<TElement> GetAncestors<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        List<TElement>? list = null;

        for (SyntaxElement? elem = this.Parent; elem != null; elem = elem.Parent)
        {
            if (elem is TElement e && (predicate == null || predicate(e)))
            {
                if (list == null)
                {
                    list = new List<TElement>();
                }

                list.Add(e);
            }
        }

        return list != null ? list.AsReadOnly() : Array.Empty<TElement>();
    }

    /// <summary>
    /// Gets the all ancestors of this element (including itself) that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<SyntaxNode> GetAncestors(Func<SyntaxNode, bool>? predicate = null)=>
        GetAncestors<SyntaxNode>(predicate);

    /// <summary>
    /// Gets the all ancestors of this element (including itself) that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<TElement> GetAncestorsOrSelf<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        List<TElement>? list = null;

        for (var elem = this; elem != null; elem = elem.Parent)
        {
            if (elem is TElement e && (predicate == null || predicate(e)))
            {
                if (list == null)
                {
                    list = new List<TElement>();
                }

                list.Add(e);
            }
        }

        return list != null ? list.AsReadOnly() : Array.Empty<TElement>();
    }

    /// <summary>
    /// Gets the all ancestors of this element (including itself) that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<SyntaxElement> GetAncestorsOrSelf(Func<SyntaxElement, bool>? predicate = null) =>
        GetAncestorsOrSelf<SyntaxElement>(predicate);

    /// <summary>
    /// Gets the first descendant of this element that matches the specified type and predicate.
    /// </summary>
    public TElement? GetFirstDescendant<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        return GetFirstDescendant(this, predicate, includeSelf: false);
    }

    /// <summary>
    /// Gets the first descendant of this element that matches the specified type and predicate.
    /// </summary>
    public SyntaxElement? GetFirstDescendant(Func<SyntaxElement, bool>? predicate = null) =>
        GetFirstDescendant<SyntaxElement>(predicate);

    /// <summary>
    /// Gets the first descendant of this element (including itself) that matches the specified type and predicate.
    /// </summary>
    public TElement? GetFirstDescendantOrSelf<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        return GetFirstDescendant(this, predicate, includeSelf: true);
    }

    /// <summary>
    /// Gets the first descendant of this element (including itself) that matches the specified type and predicate.
    /// </summary>
    public SyntaxElement? GetFirstDescendantOrSelf(Func<SyntaxElement, bool>? predicate = null) =>
        GetFirstDescendantOrSelf<SyntaxElement>(predicate);

    /// <summary>
    /// Gets the first descendant or self that is of the type TElement and matches the predicate.
    /// </summary>
    private static TElement? GetFirstDescendant<TElement>(SyntaxElement element, Func<TElement, bool>? predicate, bool includeSelf)
        where TElement : SyntaxElement
    {
        if (includeSelf && element is TElement telem && (predicate == null || predicate(telem)))
        {
            return telem;
        }

        var root = element;
        var childIndex = 0;

        while (element != null)
        {
            if (childIndex < element.ChildCount && childIndex >= 0)
            {
                // walk down
                var child = element.GetChild(childIndex);
                if (child != null)
                {
                    element = child;
                    childIndex = 0;

                    if (element is TElement telem2 && (predicate == null || predicate(telem2)))
                    {
                        return telem2;
                    }
                }
                else
                {
                    childIndex++;
                }
            }
            else if (element == root)
            {
                break;
            }
            else
            {
                // walk up
                childIndex = element.IndexInParent + 1;
                element = element.Parent!;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all descendants of this element that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<TElement> GetDescendants<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        return GetDescendants(this, predicate, includeSelf: false);
    }

    /// <summary>
    /// Gets all descendants of this element that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<SyntaxElement> GetDescendants(Func<SyntaxElement, bool>? predicate = null) =>
        GetDescendants<SyntaxElement>(predicate);

    /// <summary>
    /// Gets all descendants of this element (including itself) that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<TElement> GetDescendantsOrSelf<TElement>(Func<TElement, bool>? predicate = null)
        where TElement : SyntaxElement
    {
        return GetDescendants(this, predicate, includeSelf: true);
    }

    /// <summary>
    /// Gets all descendants of this element (including itself) that match the specified type and predicate.
    /// </summary>
    public IReadOnlyList<SyntaxElement> GetDescendantsOrSelf(Func<SyntaxElement, bool>? predicate = null) =>
        GetDescendantsOrSelf<SyntaxElement>(predicate);

    /// <summary>
    /// Gets the descendants of the specified element that match the specified type and predicate.
    /// </summary>
    private static IReadOnlyList<TElement> GetDescendants<TElement>(
        SyntaxElement element,
        Func<TElement, bool>? predicate,
        bool includeSelf)
        where TElement : SyntaxElement
    {
        List<TElement>? list = null;

        if (includeSelf && element is TElement telem && (predicate == null || predicate(telem)))
        {
            list = list ?? new List<TElement>();
            list.Add(telem);
        }

        var root = element;
        var childIndex = 0;

        while (element != null)
        {
            if (childIndex < element.ChildCount && childIndex >= 0)
            {
                // walk down
                var child = element.GetChild(childIndex);
                if (child != null)
                {
                    element = child;
                    childIndex = 0;

                    if (element is TElement telem2 && (predicate == null || predicate(telem2)))
                    {
                        list = list ?? new List<TElement>();
                        list.Add(telem2);
                    }
                }
                else
                {
                    childIndex++;
                }
            }
            else if (element == root)
            {
                break;
            }
            else
            {
                // walk up
                childIndex = element.IndexInParent + 1;
                element = element.Parent!;
            }
        }

        return list != null ? list.AsReadOnly() : Array.Empty<TElement>();
    }

    /// <summary>
    /// Gets all the tokens contained by this <see cref="SyntaxElement"/> in lexical order.
    /// </summary>
    public IReadOnlyList<SyntaxToken> GetTokens(bool includeZeroLengthTokens = false)
    {
        var tokens = new List<SyntaxToken>();

        SyntaxToken? token = null;
        while ((token = GetNextToken(this, token, includeZeroLengthTokens)) != null)
        {
            tokens.Add(token);
        }

        return tokens.AsReadOnly();
    }

    /// <summary>
    /// Invokes the action for each token contained by this <see cref="SyntaxElement"/>
    /// </summary>
    public void WalkTokens(Action<SyntaxToken> action)
    {
        WalkTokens(this.Start, this.End, action);
    }

    /// <summary>
    /// Invokes the action for each token contained by this <see cref="SyntaxElement"/>
    /// between the <see cref="p:start"/> and <see cref="p:end"/> text position.
    /// </summary>
    public void WalkTokens(int start, int end, Action<SyntaxToken> action)
    {
        start = Math.Max(start, this.Start);
        end = Math.Min(end, this.End);

        if (start < end)
        {
            for (var token = this.GetTokenAt(start);
                token != null && token.Start < end;
                token = GetNextToken(this, token, includeZeroLengthTokens: false))
            {
                action(token);
            }
        }
    }

    /// <summary>
    /// Invokes the action for the element and its descendants, in lexical order, top down.
    /// </summary>
    /// <param name="action">The action that is invoked for each <see cref="SyntaxElement"/></param>
    public void WalkElements(Action<SyntaxElement> action)
    {
        WalkElements(this, action);
    }

    /// <summary>
    /// Walks this element and its descendants in lexical order, invoking the actions for each <see cref="SyntaxElement"/> including the root element.
    /// </summary>
    /// <param name="root">The root element of the walk. The walk includes this element and any descendant elements.</param>
    /// <param name="fnBefore">An optional function that is invoked for each element before any child elements are visited.</param>
    /// <param name="fnAfter">An optional function that is invoked for each element after any child elements have been visited.</param>
    /// <param name="fnDescend">An optional function that determines whether the children of an element are visited.</param>
    public static void WalkElements(
        SyntaxElement root,
        Action<SyntaxElement>? fnBefore = null,
        Action<SyntaxElement>? fnAfter = null,
        Func<SyntaxElement, bool>? fnDescend = null)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        var node = root;
        var childIndex = 0;

        // the root before walking children
        fnBefore?.Invoke(root);

        while (node != null)
        {
            if (childIndex < node.ChildCount && childIndex >= 0 && (fnDescend == null || fnDescend(node)))
            {
                // walk down
                var child = node.GetChild(childIndex);
                if (child != null)
                {
                    node = child;
                    childIndex = 0;

                    // before walking children
                    fnBefore?.Invoke(node);
                }
                else
                {
                    childIndex++;
                }
            }
            else
            {
                // after walking children
                fnAfter?.Invoke(node);

                // stop if we are done with root node
                if (node == root)
                    break;

                // walk up
                childIndex = node.IndexInParent + 1;
                node = node.Parent;
            }
        }
    }

    /// <summary>
    /// Walks this node and its descendants in lexical order, invoking the actions for each <see cref="SyntaxElement"/> including the root node.
    /// </summary>
    /// <param name="root">The root node of the walk. The walk includes this node and any descendant nodes.</param>
    /// <param name="fnBefore">An optional function that is invoked for each node before any child nodes are visited.</param>
    /// <param name="fnAfter">An optional function that is invoked for each node after any child nodes have been visited.</param>
    /// <param name="fnDescend">An optional function that determines whether the child nodes of an node are visited.</param>
    public static void WalkNodes(
        SyntaxNode root,
        Action<SyntaxNode>? fnBefore = null,
        Action<SyntaxNode>? fnAfter = null,
        Func<SyntaxNode, bool>? fnDescend = null)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        var node = root;
        var childIndex = 0;

        // the root before walking children
        fnBefore?.Invoke(root);

        while (node != null)
        {
            if (childIndex < node.ChildCount && childIndex >= 0 && (fnDescend == null || fnDescend(node)))
            {
                // walk down
                var child = node.GetChild(childIndex) as SyntaxNode;
                if (child != null)
                {
                    node = child;
                    childIndex = 0;

                    // before walking children
                    fnBefore?.Invoke(node);
                }
                else
                {
                    childIndex++;
                }
            }
            else
            {
                // after walking children
                fnAfter?.Invoke(node);

                // stop if we are done with root node
                if (node == root)
                    break;

                // walk up
                childIndex = node.IndexInParent + 1;
                node = node.Parent;
            }
        }
    }

    /// <summary>
    /// Gets the next <see cref="SyntaxElement"/> sibling of this element or null if there is no next sibling.
    /// </summary>
    public SyntaxElement? GetNextSibling(bool includeZeroWidthElements = false)
    {
        if (this.Parent != null)
        {
            for (int i = this.IndexInParent + 1, n = this.Parent.ChildCount; i < n && i >= 0; i++)
            {
                var sibling = this.Parent.GetChild(i);
                if (sibling != null && (includeZeroWidthElements || sibling.Length > 0))
                    return sibling;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the previous <see cref="SyntaxElement"/> sibling of this element or null if there is no previous sibling.
    /// </summary>
    public SyntaxElement? GetPreviousSibling(bool includeZeroWidthElements = false)
    {
        if (this.Parent != null)
        {
            for (int i = this.IndexInParent - 1; i >= 0; i--)
            {
                var sibling = this.Parent.GetChild(i);
                if (sibling != null && (includeZeroWidthElements || sibling.Length > 0))
                    return sibling;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the first descendant token of this <see cref="SyntaxElement"/> in lexical order.
    /// </summary>
    public SyntaxToken? GetFirstToken(bool includeZeroLengthTokens = false)
    {
        return GetNextToken(this, null, includeZeroLengthTokens);
    }

    /// <summary>
    /// Gets the last descendant token of this <see cref="SyntaxElement"/> in lexical order.
    /// </summary>
    public SyntaxToken? GetLastToken(bool includeZeroLengthTokens = false)
    {
        return GetPreviousToken(this, null, includeZeroLengthTokens);
    }

    /// <summary>
    /// Get the next token within the subtree of the root starting from the specified token.
    /// </summary>
    protected static SyntaxToken? GetNextToken(SyntaxElement? root, SyntaxToken? token, bool includeZeroLengthTokens)
    {
        var node = token != null ? token.Parent : root;
        var childIndex = token != null ? token.IndexInParent + 1 : 0;

        while (node != null)
        {
            if (childIndex < node.ChildCount && childIndex >= 0)
            {
                var child = node.GetChild(childIndex);
                if (child != null)
                {
                    node = child;
                    childIndex = 0;

                    if (node is SyntaxToken t && (includeZeroLengthTokens || t.Length > 0))
                    {
                        return t;
                    }
                }
                else
                {
                    childIndex++;
                }
            }
            else if (node == root)
            {
                return null;
            }
            else
            {
                childIndex = node.IndexInParent + 1;
                node = node.Parent;
            }
        }

        return null;
    }

    protected static SyntaxToken? GetPreviousToken(SyntaxElement? root, SyntaxToken? token, bool includeZeroLengthTokens)
    {
        var node = token != null ? token.Parent : root;

        var childIndex = token != null 
            ? token.IndexInParent - 1 
            : (root != null ? root.ChildCount - 1 : 0);

        while (node != null)
        {
            if (childIndex < node.ChildCount && childIndex >= 0)
            {
                var child = node.GetChild(childIndex);
                if (child != null)
                {
                    node = child;
                    childIndex = node.ChildCount - 1;

                    if (node is SyntaxToken t && (includeZeroLengthTokens || t.Length > 0))
                    {
                        return t;
                    }
                }
                else
                {
                    childIndex--;
                }
            }
            else if (node == root)
            {
                return null;
            }
            else
            {
                childIndex = node.IndexInParent - 1;
                node = node.Parent;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the token at the specified position in the source text.
    /// If the position is within trivia, it will find the next token after the trivia.
    /// If the position is past the end of the tree, it will return the last token.
    /// </summary>
    public SyntaxToken? GetTokenAt(int position)
    {
        var element = this;

        if (this is SyntaxToken)
        {
            if (this.Start <= position && position < this.End)
                return (SyntaxToken)this;
        }
        else
        {
            element = this.Root;
        }

        if (position >= element?.Length)
        {
            return element.GetLastToken(includeZeroLengthTokens: true);
        }

    // drill down until we find the token that covers this position.
    retry:
        if (element != null && element.ChildCount > 0)
        {
            for (int i = 0, n = element.ChildCount; i < n; i++)
            {
                var child = element.GetChild(i);
                if (child != null)
                {
                    if (child.Start <= position && position < child.End)
                    {
                        if (child is SyntaxToken childToken)
                            return childToken;

                        element = child;
                        goto retry;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the node that spans the specified range in the source text.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="length"></param>
    public SyntaxNode? GetNodeAt(int position, int length)
    {
        var token = GetTokenAt(position);
        var parent = token?.Parent;

        while (parent != null && parent.End < position + length)
        {
            parent = parent.Parent;
        }

        return parent;
    }
    #endregion

    #region ISourceLocation
    ISourceDocument ISourceLocation.Document => 
        this.Tree?.Document!;  // will have tree when tree constructed

    int ISourceLocation.Start => 
        this.TextStart;

    int ISourceLocation.Length => 
        this.TextLength;
    #endregion
}