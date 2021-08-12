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
    ///     <c>pushMode</c>
    ///     lexer action by calling
    ///     <see cref="Antlr4.Runtime.Lexer.PushMode(int)" />
    ///     with the assigned mode.
    /// </summary>
    /// <author>Sam Harwell</author>
    /// <since>4.2</since>
    public sealed class LexerPushModeAction : ILexerAction
    {
        /// <summary>
        ///     Constructs a new
        ///     <c>pushMode</c>
        ///     action with the specified mode value.
        /// </summary>
        /// <param name="mode">
        ///     The mode value to pass to
        ///     <see cref="Antlr4.Runtime.Lexer.PushMode(int)" />
        ///     .
        /// </param>
        public LexerPushModeAction(int mode)
        {
            this.Mode = mode;
        }

        /// <summary>Get the lexer mode this action should transition the lexer to.</summary>
        /// <returns>
        ///     The lexer mode for this
        ///     <c>pushMode</c>
        ///     command.
        /// </returns>
        public int Mode { get; }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see cref="LexerActionType.PushMode" />
        ///     .
        /// </returns>
        public LexerActionType ActionType => LexerActionType.PushMode;

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
        ///         <see cref="Antlr4.Runtime.Lexer.PushMode(int)" />
        ///         with the
        ///         value provided by
        ///         <see cref="Mode()" />
        ///         .
        ///     </p>
        /// </summary>
        public void Execute([NotNull] Lexer lexer)
        {
            lexer.PushMode(Mode);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, Mode);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is LexerPushModeAction))
            {
                return false;
            }

            return Mode == ((LexerPushModeAction) obj).Mode;
        }

        public override string ToString()
        {
            return $"pushMode({Mode})";
        }
    }
}