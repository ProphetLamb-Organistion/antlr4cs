// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime
{
    /// <summary>
    ///     Provides an empty default implementation of
    ///     <see cref="IAntlrErrorListener{Symbol}" />
    ///     . The
    ///     default implementation of each method does nothing, but can be overridden as
    ///     necessary.
    /// </summary>
    /// <author>Sam Harwell</author>
    public class BaseErrorListener : IParserErrorListener
    {
        public virtual void SyntaxError([NotNull] IRecognizer recognizer, [AllowNull] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg,
            [AllowNull] RecognitionException e)
        {
        }

        public virtual void ReportAmbiguity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, bool exact, [AllowNull] BitSet ambigAlts,
            [NotNull] ATNConfigSet configs)
        {
        }

        public virtual void ReportAttemptingFullContext([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, [AllowNull] BitSet conflictingAlts,
            [NotNull] SimulatorState conflictState)
        {
        }

        public virtual void ReportContextSensitivity([NotNull] Parser recognizer, [NotNull] DFA dfa, int startIndex, int stopIndex, int prediction,
            [NotNull] SimulatorState acceptState)
        {
        }
    }
}