namespace Parkour.Lowering;

using Semantics;
using Symbols;
using static Semantics.SemanticFactory;

public class FieldInitializerLowerer : SemanticLowerer
{
    public static readonly FieldInitializerLowerer Instance =
        new FieldInitializerLowerer();

    public override SemanticLowering Lower(
        ImmutableList<SemanticElement> elements, 
        SymbolTable symbols)
    {
        var newElements = elements.RewriteAll<SemanticElement, TypeDeclaration>(Lower);
        return new SemanticLowering(newElements);
    }

    /// <summary>
    /// Lowers the field initializers in this type declaration
    /// to assignments inside constructors.
    /// </summary>
    public TypeDeclaration Lower(TypeDeclaration td)
    {
        // all fields with initializers that are not consts
        var fields = td.Declarations
            .OfType<FieldDeclaration>()
            .Where(fd =>
                fd.Initializer != null
                && !fd.Modifiers.Contains(SymbolModifier.Constant))
            .ToList();

        var instanceFields =
            fields.Where(f => !f.Modifiers.Contains(SymbolModifier.Static))
            .ToList();

        var staticFields =
            fields.Where(f => f.Modifiers.Contains(SymbolModifier.Static))
            .ToList();

        // no fields to fix, no rewrite
        if (fields.Count == 0)
            return td;

        var instanceAssignments = instanceFields
            .Select(f => Assign(Name(f.Name), f.Initializer!))
            .ToImmutableList<Expression>();

        var staticAssignments = staticFields
            .Select(f => Assign(Name(f.Name), f.Initializer!))
            .ToImmutableList<Expression>();

        var foundStaticConstructor = false;
        var foundInstanceConstructor = false;

        var newDecls = new List<Declaration>();
        foreach (var decl in td.Declarations)
        {
            if (decl is ConstructorDeclaration cd)
            {
                if (cd.Modifiers.Contains(SymbolModifier.Static))
                {
                    var newConstructor = cd.WithBody(Block(staticAssignments.Add(cd.Body)));
                    newDecls.Add(newConstructor);
                    foundStaticConstructor = true;
                }
                else
                {
                    // TODO: only modify constructors that do not invoke other constructors on this object
                    var newConstructor = cd.WithBody(Block(instanceAssignments.Add(cd.Body)));
                    newDecls.Add(newConstructor);
                    foundInstanceConstructor = true;
                }
            }
            else if (decl is FieldDeclaration fd)
            {
                newDecls.Add(fd.WithInitializer(null));
            }
            else
            {
                newDecls.Add(decl);
            }
        }

        if (!foundStaticConstructor && staticAssignments.Count > 0)
        {
            newDecls.Add(Constructor(Block(staticAssignments)).WithModifiers(SymbolModifier.Static));
        }

        if (!foundInstanceConstructor && instanceAssignments.Count > 0)
        {
            newDecls.Add(Constructor(Block(instanceAssignments)));
        }

        return td.WithDeclarations(newDecls.ToImmutableList());
    }
}