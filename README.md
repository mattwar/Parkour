# Parkour

This is a toolkit for building compilers and exploring new languages.

It comes with a variety of compiler/language components that can be used together or piecemeal, and abandoned later when you want to move beyond a prototype.

It is not currently published as a nuget package

## Parkour

This project contains the base parkour components that can be used all together or piecemeal. Plug-in alternate implementations that work with the rest by adhering to the shared interfaces.

- **Common** - common types and interfaces that enable loose coupling of models.
    - Diagnostics
    - Syntax
    - Symbols
    - Semantics
    - Settings

- **Parsers** - a parser-combinator model
    - enables quickly assembling complex parsers at runtime with strong typing

- **Syntax** - a Roslyn-like syntax tree
    - Use as is or extend to represent strongly-typed language-specific nodes
    
- **Semantics** - an 'expression tree' model for an entire compilation unit.
    - including all steps to analyze, lower and emit basic MSIL
    - Extensible to include higher level concepts and declarations

- **Symbols** - a model for representing basic types and other language symbols
    - Represents all basic MSIL types and declarations
    - Extensible to include higher level concepts and declarations

- **Runtime** - additional definitions for representing dotnet symbols

- **Projects** - a Roslyn-like document/project/solution model
    - useful for modelling relationship between projects

- **Services** - abstractions for common intellisense services
    - basic implementations for classification, completion, formatting, hovertext and more

- **Text** - a model for representing immutable change-tracked text
    - Useful for code fixes and refactorings


## Parkour.Cecil

Load symbols from assembly metadata and emit lowered parkour semantic trees into IL using the Cecil library.

## Parkour.Linq

Convert parkour sementic expressions into `System.Linq.Expression` trees at runtime, using an emitter.

## Parkour.Reflection

Load symbols from reflection metadata and emit lowered parkour semantic trees into IL using `System.Reflection`.





