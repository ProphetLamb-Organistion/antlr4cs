// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime
{
    [Serializable]
    public class LexerNoViableAltException : RecognitionException
    {
        private const long serialVersionUID = -730999203913001726L;

        /// <summary>Matching attempted at what input index?</summary>
        private readonly int startIndex;

        public LexerNoViableAltException([AllowNull] Lexer lexer, [NotNull] ICharStream input, int startIndex, [AllowNull] ATNConfigSet deadEndConfigs)
            : base(lexer, input)
        {
            this.startIndex = startIndex;
            this.DeadEndConfigs = deadEndConfigs;
        }

        public virtual int StartIndex => startIndex;

        /// <summary>Which configurations did we try at input.index() that couldn't match input.LA(1)?</summary>
        [MaybeNull]
        public virtual ATNConfigSet DeadEndConfigs { get; }

        public override IIntStream InputStream => (ICharStream) base.InputStream;

        public override string ToString()
        {
            string symbol = String.Empty;
            if (startIndex >= 0 && startIndex < ((ICharStream) InputStream).Size)
            {
                symbol = ((ICharStream) InputStream).GetText(Interval.Of(startIndex, startIndex));
                symbol = Utils.EscapeWhitespace(symbol, false);
            }

            return $"{typeof(LexerNoViableAltException).Name}('{symbol}')";
        }
    }
}