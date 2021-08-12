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
    ///     <c>type</c>
    ///     lexer action by calling
    ///     <see cref="Lexer.Type" />
    ///     with the assigned type.
    /// </summary>
    /// <author>Sam Harwell</author>
    /// <since>4.2</since>
    public class LexerTypeAction : ILexerAction
    {
        private readonly int type;

        /// <summary>
        ///     Constructs a new
        ///     <paramref name="type" />
        ///     action with the specified token type value.
        /// </summary>
        /// <param name="type">
        ///     The type to assign to the token using
        ///     <see cref="Lexer.Type" />
        ///     .
        /// </param>
        public LexerTypeAction(int type)
        {
            this.type = type;
        }

        /// <summary>Gets the type to assign to a token created by the lexer.</summary>
        /// <returns>The type to assign to a token created by the lexer.</returns>
        public virtual int Type => type;

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see cref="LexerActionType.Type" />
        ///     .
        /// </returns>
        public virtual LexerActionType ActionType => LexerActionType.Type;

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see langword="false" />
        ///     .
        /// </returns>
        public virtual bool IsPositionDependent => false;

        /// <summary>
        ///     <inheritDoc />
        ///     <p>
        ///         This action is implemented by calling
        ///         <see cref="Lexer.Type" />
        ///         with the
        ///         value provided by
        ///         <see cref="Type()" />
        ///         .
        ///     </p>
        /// </summary>
        public virtual void Execute([NotNull] Lexer lexer)
        {
            lexer.Type = type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, type);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is LexerTypeAction))
            {
                return false;
            }

            return type == ((LexerTypeAction) obj).type;
        }

        public override string ToString()
        {
            return $"type({type})";
        }
    }
}