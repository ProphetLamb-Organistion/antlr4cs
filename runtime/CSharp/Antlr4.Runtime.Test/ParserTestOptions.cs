using Antlr4.Runtime.Tree;

namespace Antlr4.Runtime.Test
{
    internal class ParserTestOptions<TParser> : LexerTestOptions
    {
        public Factory<ITokenStream, TParser> Parser { get; set; }

        public Factory<TParser, IParseTree> ParserStartRule { get; set; }

        public bool Debug { get; set; }
    }
}