// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif


namespace Antlr4.Runtime.Atn
{
    /// <summary>
    ///     Implements the
    ///     <c>popMode</c>
    ///     lexer action by calling
    ///     <see cref="Antlr4.Runtime.Lexer.PopMode()" />
    ///     .
    ///     <p>
    ///         The
    ///         <c>popMode</c>
    ///         command does not have any parameters, so this action is
    ///         implemented as a singleton instance exposed by
    ///         <see cref="Instance" />
    ///         .
    ///     </p>
    /// </summary>
    /// <author>Sam Harwell</author>
    /// <since>4.2</since>
    public sealed class LexerPopModeAction : ILexerAction
    {
        /// <summary>Provides a singleton instance of this parameterless lexer action.</summary>
        public static readonly LexerPopModeAction Instance = new();

        /// <summary>
        ///     Constructs the singleton instance of the lexer
        ///     <c>popMode</c>
        ///     command.
        /// </summary>
        private LexerPopModeAction()
        {
        }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see cref="LexerActionType.PopMode" />
        ///     .
        /// </returns>
        public LexerActionType ActionType => LexerActionType.PopMode;

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see langword="false" />
        ///     .
        /// </returns>
        public bool IsPositionDependent => false;

        /// <summary>
        ///     <inheritDoc />
        ///     <p>
        ///         This action is implemented by calling
        ///         <see cref="Antlr4.Runtime.Lexer.PopMode()" />
        ///         .
        ///     </p>
        /// </summary>
        public void Execute([NotNull] Lexer lexer)
        {
            lexer.PopMode();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType);
        }

        public override bool Equals(object obj)
        {
            return obj == this;
        }

        public override string ToString()
        {
            return "popMode";
        }
    }
}