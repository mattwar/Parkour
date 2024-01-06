using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parkour.Syntax
{
    public static class SyntaxFactory
    {
        public static SyntaxToken Token(string kind, string trivia, string text) =>
            new SyntaxToken(kind, trivia, text);

        public static SyntaxToken Token(string kind, string text) =>
            new SyntaxToken(kind, "", text);

        public static SyntaxList List(string kind, params SyntaxElement[] elements) =>
            new SyntaxList(kind, elements);
    }
}
