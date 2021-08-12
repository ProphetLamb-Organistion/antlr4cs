// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif



namespace Antlr4.Runtime
{
    /// <summary>
    ///     This implementation of
    ///     <see cref="IAntlrErrorListener{Symbol}" />
    ///     dispatches all calls to a
    ///     collection of delegate listeners. This reduces the effort required to support multiple
    ///     listeners.
    /// </summary>
    /// <author>Sam Harwell</author>
    public class ProxyErrorListener<Symbol> : IAntlrErrorListener<Symbol>
    {
        private readonly IEnumerable<IAntlrErrorListener<Symbol>> delegates;

        public ProxyErrorListener(IEnumerable<IAntlrErrorListener<Symbol>> delegates)
        {
            if (delegates == null)
            {
                throw new ArgumentNullException("delegates");
            }

            this.delegates = delegates;
        }

        protected internal virtual IEnumerable<IAntlrErrorListener<Symbol>> Delegates => delegates;

        public virtual void SyntaxError([NotNull] IRecognizer recognizer, [AllowNull] Symbol offendingSymbol, int line, int charPositionInLine, [NotNull] string msg,
            [AllowNull] RecognitionException e)
        {
            foreach (IAntlrErrorListener<Symbol> listener in delegates)
            {
                listener.SyntaxError(recognizer, offendingSymbol, line, charPositionInLine, msg, e);
            }
        }
    }
}