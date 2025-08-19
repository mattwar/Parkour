using Mono.Cecil;
using Parkour.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parkour.Cecil;

public static class CecilHelpers
{
    /// <summary>
    /// Returns the symbol name with the arity.
    /// </summary>
    public static string GetMetadataName(this IMetadataTokenProvider cecilSymbol) =>
        cecilSymbol switch
        {
            MemberReference mr => mr.Name,
            ParameterReference pr => pr.Name,
            _ => ""
        };

    /// <summary>
    /// Returns the symbol name without the arity suffix.
    /// </summary>
    public static string GetName(this IMetadataTokenProvider cecilSymbol)
    {
        var name = cecilSymbol.GetMetadataName();
        var arityStart = name.IndexOf('`');
        return arityStart >= 0 ? name.Substring(0, arityStart) : name;
    }

    /// <summary>
    /// Gets the name and the arity of the symbol.
    /// </summary>
    public static string GetNameAndArity(this IMetadataTokenProvider cecilSymbol, out int arity)
    {
        var name = cecilSymbol.GetMetadataName();
        var arityStart = name.IndexOf('`');
        var arityText = name.Substring(arityStart + 1);
        int.TryParse(arityText, out arity);
        return arityStart >= 0 ? name.Substring(0, arityStart + 1) : name;
    }

    /// <summary>
    /// Returns true if the name matches.
    /// </summary>
    public static bool MatchesName(this IMetadataTokenProvider cecilSymbol, string name, bool ignoreArity = true)
    {
        var cecilName = cecilSymbol.GetMetadataName();
        if (ignoreArity)
        {
            var arityStart = cecilName.IndexOf('`');
            if (arityStart >= 0)
            {
                return arityStart == name.Length
                    && string.Compare(cecilName, 0, name, 0, arityStart, StringComparison.Ordinal) == 0;
            }           
        }

        return cecilName.Length == name.Length
            && string.Compare(cecilName, name, StringComparison.Ordinal) == 0;
    }

    /// <summary>
    /// Finds the member of a type definition that matches the parkour symbol definition.
    /// </summary>
    public static IMemberDefinition? GetMatchingMember(MemberSymbol symbolDef, TypeDefinition td)
    {
        switch (symbolDef)
        {
            case MethodSymbol method:
                return td.Methods.FirstOrDefault(m => MatchMethod(method, m, false));
            case ConstructorSymbol constructor:
                return td.Methods.FirstOrDefault(m => MatchConstructor(constructor, m, false));
            case FieldSymbol field:
                return td.Fields.FirstOrDefault(f => MatchField(field, f, false));
            case PropertySymbol property:
                return td.Properties.FirstOrDefault(p => MatchProperty(property, p, false));
            case IndexerSymbol indexer:
                return td.Properties.FirstOrDefault(p => MatchIndexer(indexer, p, false));
            case TypeSymbol type:
                return td.NestedTypes.FirstOrDefault(n => MatchType(type, n, false));
            default:
                return null;
        }
    }

    private static bool MatchDeclaringType(MemberSymbol type, MemberReference member)
    {
        if (type.DeclaringType == null && member.DeclaringType == null)
            return true;
        return type.DeclaringType != null 
            && member.DeclaringType != null
            && MatchType(type.DeclaringType, member.DeclaringType);
    }

    private static bool MatchMethod(MethodSymbol method, MethodReference methodRef, bool matchDeclaringType = true)
    {
        if (methodRef is GenericInstanceMethod genericMethod)
        {
            if (method.TypeArguments.Count != genericMethod.GenericArguments.Count)
                return false;
            for (int i = 0; i < method.TypeArguments.Count; i++)
            {
                if (!MatchType(method.TypeArguments[i], genericMethod.GenericArguments[i]))
                    return false;
            }
        }

        return methodRef.MatchesName(method.Name)
            && method.Parameters.Count == methodRef.Parameters.Count
            && method.TypeParameters.Count == methodRef.GenericParameters.Count
            && MatchParameters(method.Parameters, methodRef.Parameters)
            && (!matchDeclaringType || MatchDeclaringType(method, methodRef));
    }

    private static bool MatchConstructor(ConstructorSymbol constructor, MethodDefinition methodDef, bool matchDeclaringType = true)
    {
        return methodDef.IsConstructor
            && methodDef.IsStatic == constructor.IsStatic
            && methodDef.Parameters.Count == constructor.Parameters.Count
            && MatchParameters(constructor.Parameters, methodDef.Parameters)
            && (!matchDeclaringType || MatchDeclaringType(constructor.ConstructedType, methodDef.DeclaringType));
    }

    private static bool MatchField(FieldSymbol field, FieldReference fieldRef, bool matchDeclaringType = true)
    {
        return field.Name == fieldRef.Name
            && MatchType(field.Type, fieldRef.FieldType, matchDeclaringType)
            && (!matchDeclaringType || MatchDeclaringType(field, fieldRef));
    }

    private static bool MatchProperty(PropertySymbol property, PropertyReference propRef, bool matchDeclaringType = true)
    {
        return property.Name == propRef.Name
            && propRef.Parameters.Count == 0
            && MatchType(property.Type, propRef.PropertyType)
            && (!matchDeclaringType || MatchDeclaringType(property, propRef));
    }

    private static bool MatchIndexer(IndexerSymbol indexer, PropertyReference propRef, bool matchDeclaringType = true)
    {
        return indexer.Name == propRef.Name
            && MatchParameters(indexer.GetMethod!.Parameters, propRef.Parameters)
            && (!matchDeclaringType || MatchDeclaringType(indexer, propRef));
    }

    private static bool MatchType(TypeSymbol type, TypeReference typeRef, bool matchDeclaringType = true)
    {
        if (typeRef is GenericParameter gp && type is TypeParameterSymbol tp)
        {
            return type.Name == tp.Name;
        }
        else if (typeRef is GenericInstanceType genericType)
        {
            if (type.TypeArguments.Count != genericType.GenericArguments.Count)
                return false;
            for (int i = 0; i < type.TypeArguments.Count; i++)
            {
                if (!MatchType(type.TypeArguments[i], genericType.GenericArguments[i]))
                    return false;
            }
        }

        return typeRef.MatchesName(type.Name)
            && type.Namespace == typeRef.Namespace
            && type.TypeParameters.Count == typeRef.GenericParameters.Count
            && (!matchDeclaringType || MatchDeclaringType(type, typeRef));
    }

    private static bool MatchParameters(IReadOnlyList<ParameterSymbol> parameters, IList<ParameterDefinition> parameterDefs)
    {
        if (parameterDefs.Count != parameterDefs.Count)
            return false;

        for (int i = 0; i < parameters.Count; i++)
        {
            if (!MatchType(parameters[i].Type, parameterDefs[i].ParameterType))
                return false;
        }

        return true;
    }


    /// <summary>
    /// Implicit interface implementations are based only on method's name and signature equivalence.
    /// </summary>
    public static bool IsImplicitInterfaceImplementation(MethodDefinition method, MethodReference interfaceMethod)
    {
        var declaringType = ((MethodReference)method).DeclaringType.Resolve();

        // check that the 'overridden' method is iface method and the iface is implemented by method.DeclaringType
        if (!IsInterface(interfaceMethod.DeclaringType) ||
            !declaringType.Interfaces.Any(i => Equals(i.InterfaceType, interfaceMethod.DeclaringType)))
        {
            return false;
        }

        // check whether the type contains some other explicit implementation of the method
        if (declaringType.Methods.SelectMany(m => m.Overrides).Any(m => Equals(m, interfaceMethod)))
        {
            // explicit implementation -> no implicit implementation possible
            return false;
        }

        // now it is enough to just match the signatures and names:
        return method.Name == interfaceMethod.Name && SignatureMatches(method, interfaceMethod);
    }

    /// <summary>
    /// True if the type is known to be an interface.
    /// </summary>
    public static bool IsInterface(TypeReference type)
    {
        return type.Resolve().IsInterface;
    }

    /// <summary>
    /// True if the two method signatures match
    /// </summary>
    public static bool SignatureMatches(MethodReference method1, MethodReference method2)
    {
        if (method1 is GenericInstanceMethod gm1 && method2 is GenericInstanceMethod gm2)
        {
            if (!SignatureMatches(gm1.ElementMethod, gm2.ElementMethod))
                return false;

            return Equals(gm1.GenericArguments, gm2.GenericArguments);
        }
        else
        {
            method1 = method1.Resolve();
            method2 = method2.Resolve();

            if (method1.GenericParameters.Count != method2.GenericParameters.Count)
                return false;

            if (method1.Parameters.Count != method2.Parameters.Count)
                return false;

            for (int i = 0; i < method1.Parameters.Count; i++)
            {
                if (!Equals(method1.Parameters[i].ParameterType, method2.Parameters[i].ParameterType))
                    return false;
            }
        }

        return true;  
    }


    /// <summary>
    /// Gets the dotted path to a symbol from the root of the global namespace.
    /// </summary>
    public static bool TryGetDottedPath(IMetadataTokenProvider cecilSymbol, [NotNullWhen(true)] out string? dottedPath)
    {
        dottedPath = GetPath(cecilSymbol);
        return dottedPath != null;

        string? GetPath(IMetadataTokenProvider cecilSymbol)
        {
            return GetConstructedPath(cecilSymbol, ImmutableList<TypeReference>.Empty);
        }

        string? GetConstructedPath(IMetadataTokenProvider cecilSymbol, ImmutableList<TypeReference> typeArguments)
        {
            if (cecilSymbol is TypeDefinition type)
            {
                var myTypeArgs = typeArguments.Count >= type.GenericParameters.Count
                    ? typeArguments.GetRange(typeArguments.Count - type.GenericParameters.Count, type.GenericParameters.Count)
                    : ImmutableList<TypeReference>.Empty;
                var remainingTypeArgs = myTypeArgs.Count > 0
                    ? typeArguments.RemoveRange(typeArguments.Count - myTypeArgs.Count, myTypeArgs.Count)
                    : typeArguments;

                if (type.DeclaringType != null)
                {
                    if (GetConstructedPath(type.DeclaringType, remainingTypeArgs) is string declaringTypePath)
                    {
                        return WithTypeArgs(declaringTypePath + "." + type.Name, myTypeArgs);
                    }
                }
                else if (type.Namespace != null)
                {
                    return WithTypeArgs(type.Namespace + "." + type.Name, myTypeArgs);
                }
                else
                {
                    return WithTypeArgs(type.Name, myTypeArgs);
                }
            }
            else if (cecilSymbol is GenericInstanceType gt)
            {
                var typeArgs = gt.GenericArguments.ToList();
                return GetConstructedPath(gt.ElementType, typeArguments);
            }
            else if (cecilSymbol is GenericInstanceMethod gm)
            {
                var typeArgs = gm.GenericArguments.ToList();
                return GetConstructedPath(gm.ElementMethod, typeArguments);
            }
            else if (cecilSymbol is MethodDefinition method)
            {
                var myTypeArgs = typeArguments.Count >= method.GenericParameters.Count
                    ? typeArguments.GetRange(typeArguments.Count - method.GenericParameters.Count, method.GenericParameters.Count)
                    : ImmutableList<TypeReference>.Empty;
                var remainingTypeArgs = myTypeArgs.Count > 0
                    ? typeArguments.RemoveRange(typeArguments.Count - myTypeArgs.Count, myTypeArgs.Count)
                    : typeArguments;

                if (method.DeclaringType != null)
                {
                    if (GetConstructedPath(method.DeclaringType, remainingTypeArgs) is string declaringTypePath)
                    {
                        return WithTypeArgs(declaringTypePath + "." + method.Name, myTypeArgs);
                    }
                }
                else
                {
                    return WithTypeArgs(method.Name, myTypeArgs);
                }
            }
            else if (cecilSymbol is IMemberDefinition member)
            {
                if (member.DeclaringType != null)
                {
                    if (GetConstructedPath(member.DeclaringType, typeArguments) is string declaringTypePath)
                    {
                        return declaringTypePath + "." + member.Name;
                    }
                }
                else
                {
                    return member.Name;
                }
            }

            return null;
        }

        string? WithTypeArgs(string? path, ImmutableList<TypeReference> typeArgs)
        {
            if (path != null && typeArgs.Count > 0)
            {
                return $"{path}[{string.Join(", ", typeArgs.Select(ta => GetPath(ta) ?? ta.Name))}]";
            }
            return path;
        }
    }


    #region Cecil object equality

    /// <summary>
    /// Returns true if the two Cecil metadata objects are the same or refer to the same definition or instantiation.
    /// </summary>
    public static bool Equals(IMetadataTokenProvider? x, IMetadataTokenProvider? y)
    {
        if (x == y)
            return true;

        if (x == null || y == null)
            return false;

        if (x is MemberReference xmr && y is MemberReference ymr)
        {
            if (x is GenericInstanceType xgt && y is GenericInstanceType ygt)
            {
                return Equals(xgt.ElementType, ygt.ElementType)
                    && Equals(xgt.GenericArguments, ygt.GenericArguments);
            }
            else if (x is GenericInstanceMethod xgm && y is GenericInstanceMethod ygm)
            {
                return Equals(xgm.ElementMethod, ygm.ElementMethod)
                    && Equals(xgm.GenericArguments, ygm.GenericArguments);
            }
            else if (x is ArrayType xat && y is ArrayType yat)
            {
                return xat.Rank == yat.Rank
                    && Equals(xat.ElementType, yat.ElementType);
            }
            else if (x is TypeReference xt && y is TypeReference yt)
            {
                xt = xt.Resolve();
                yt = yt.Resolve();

                if (xt != null && yt != null)
                {
                    if (xt.IsGenericParameter && yt.IsGenericParameter)
                    {
                        return xt.Name == yt.Name;
                    }
                    else
                    {
                        return xt.Name == yt.Name
                            && xt.Namespace == yt.Namespace
                            && xt.GenericParameters.Count == yt.GenericParameters.Count;
                    }
                }
            }
            else if (x is MethodReference xmd && y is MethodReference ymd)
            {
                return xmd.Name == ymd.Name
                    && xmd.GenericParameters.Count == ymd.GenericParameters.Count
                    && xmd.Parameters.Count == ymd.Parameters.Count
                    && Equals(xmd.Parameters, ymd.Parameters)
                    && Equals(xmd.DeclaringType, ymd.DeclaringType);
            }
            else if (x is FieldReference xf && y is FieldReference yf)
            {
                return xf.Name == yf.Name
                    && Equals(xf.FieldType, yf.FieldType)
                    && Equals(xf.DeclaringType, yf.DeclaringType);
            }
            else if (x is PropertyReference xp && y is PropertyReference yp)
            {
                return xp.Name == yp.Name
                    && Equals(xp.PropertyType, yp.PropertyType)
                    && Equals(xp.DeclaringType, yp.DeclaringType);
            }
            else if (x is EventReference xe && y is EventReference ye)
            {
                return xe.Name == ye.Name
                    && Equals(xe.EventType, ye.EventType)
                    && Equals(xe.DeclaringType, ye.DeclaringType);
            }
            else
            {
                return xmr.Name == ymr.Name
                    && Equals(xmr.DeclaringType, ymr.DeclaringType);
            }
        }
        else if (x is ParameterReference xpr && y is ParameterReference ypr)
        {
            return xpr.Name == ypr.Name
                && Equals(xpr.ParameterType, ypr.ParameterType);
        }

        return x == y;
    }

    public static bool Equals<T>(IList<T> x, IList<T> y) where T : IMetadataTokenProvider
    {
        if (x.Count != y.Count)
            return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (!Equals(x[i], y[i]))
                return false;
        }

        return true;
    }

    public static int GetHashCode(IMetadataTokenProvider symbol)
    {
        if (symbol is MemberReference mr)
        {
            if (mr is GenericInstanceType gt)
            {
                return HashCode.Combine(
                    GetHashCode(gt.ElementType),
                    GetHashCode(gt.GenericArguments)
                    );
            }
            else if (mr is GenericInstanceMethod gm)
            {
                return HashCode.Combine(
                    GetHashCode(gm.ElementMethod),
                    GetHashCode(gm.GenericArguments)
                    );
            }
            else if (mr is ArrayType at)
            {
                return GetHashCode(at.ElementType);
            }
            else if (mr is TypeReference tr)
            {
                return HashCode.Combine(tr.FullName);
            }
            else if (mr.DeclaringType != null)
            {
                return HashCode.Combine(GetHashCode(mr.DeclaringType), mr.Name);
            }
        }
        else if (symbol is ParameterReference pr)
        {
            HashCode.Combine(pr.Name.GetHashCode(), pr.ParameterType.GetHashCode());
        }

        return symbol.GetHashCode();
    }

    public static int GetHashCode<T>(IList<T> symbols) where T : IMetadataTokenProvider
    {
        int hash = 0;
        foreach (var symbol in symbols)
        {
            hash = HashCode.Combine(hash, symbol.GetHashCode());
        }
        return hash;
    }

    /// <summary>
    /// A comparer for Cecil type references.
    /// </summary>
    public static EqualityComparer<TypeReference> TypeReferenceComparer =>
        _lazyTypeReferenceComparer ??= new CecilEqualityComparer<TypeReference>();

    private static EqualityComparer<TypeReference>? _lazyTypeReferenceComparer;

    #endregion
}


/// <summary>
/// Compares Cecil symbols for equality.
/// Definitions must be same instance.
/// References must refer to same definition and have same instantiation etc.
/// </summary>
public class CecilEqualityComparer<T> : EqualityComparer<T>
    where T : IMetadataTokenProvider
{
    public CecilEqualityComparer()
    {
    }

    public static readonly CecilEqualityComparer<T> Instance = new CecilEqualityComparer<T>();

    public override bool Equals(T? x, T? y)
    {
        return CecilHelpers.Equals(x, y);
    }

    public override int GetHashCode([DisallowNull] T symbol)
    {
        return CecilHelpers.GetHashCode(symbol);
    }
}

