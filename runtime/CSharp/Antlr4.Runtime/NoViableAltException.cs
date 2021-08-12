// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Atn;

namespace Antlr4.Runtime
{
    /// <summary>
    ///     Indicates that the parser could not decide which of two or more paths
    ///     to take based upon the remaining input.
    /// </summary>
    /// <remarks>
    ///     Indicates that the parser could not decide which of two or more paths
    ///     to take based upon the remaining input. It tracks the starting token
    ///     of the offending input and also knows where the parser was
    ///     in the various paths when the error. Reported by reportNoViableAlternative()
    /// </remarks>
    [Serializable]
    public class NoViableAltException : RecognitionException
    {
        private const long serialVersionUID = 5096000008992867052L;

        public NoViableAltException([NotNull] Parser recognizer)
            : this(recognizer, (ITokenStream) recognizer.InputStream, recognizer.CurrentToken, recognizer.CurrentToken, null, recognizer._ctx)
        {
        }

        public NoViableAltException([NotNull] IRecognizer recognizer, [NotNull] ITokenStream input, [NotNull] IToken startToken, [NotNull] IToken offendingToken,
            [AllowNull] ATNConfigSet deadEndConfigs, [NotNull] ParserRuleContext ctx)
            : base(recognizer, input, ctx)
        {
            // LL(1) error
            this.DeadEndConfigs = deadEndConfigs;
            this.StartToken = startToken;
            OffendingToken = offendingToken;
        }

        /// <summary>
        ///     The token object at the start index; the input stream might
        ///     not be buffering tokens so get a reference to it.
        /// </summary>
        /// <remarks>
        ///     The token object at the start index; the input stream might
        ///     not be buffering tokens so get a reference to it. (At the
        ///     time the error occurred, of course the stream needs to keep a
        ///     buffer all of the tokens but later we might not have access to those.)
        /// </remarks>
        [field: NotNull]
        public virtual IToken StartToken { get; }

        /// <summary>Which configurations did we try at input.index() that couldn't match input.LT(1)?</summary>
        [MaybeNull]
        public virtual ATNConfigSet DeadEndConfigs { get; }
    }
}