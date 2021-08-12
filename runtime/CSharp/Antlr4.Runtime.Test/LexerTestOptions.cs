namespace Antlr4.Runtime.Test
{
    internal class LexerTestOptions
    {
        public string TestName { get; set; }

        public Factory<ICharStream, Lexer> Lexer { get; set; }

        public string Input { get; set; }

        public string ExpectedOutput { get; set; }

        public string ExpectedErrors { get; set; }

        public bool ShowDFA { get; set; }

        internal delegate TResult Factory<TArg, TResult>(TArg argument);
    }
}