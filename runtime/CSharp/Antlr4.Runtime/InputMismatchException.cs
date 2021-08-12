// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif


namespace Antlr4.Runtime
{
    /// <summary>
    ///     This signifies any kind of mismatched input exceptions such as
    ///     when the current input does not match the expected token.
    /// </summary>
    [Serializable]
    public class InputMismatchException : RecognitionException
    {
        private const long serialVersionUID = 1532568338707443067L;

        public InputMismatchException([NotNull] Parser recognizer)
            : base(recognizer, (ITokenStream) recognizer.InputStream, recognizer._ctx)
        {
            OffendingToken = recognizer.CurrentToken;
        }
    }
}