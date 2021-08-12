// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime
{
    /// <summary>
    ///     This class provides a default implementation of the
    ///     <see cref="IVocabulary" />
    ///     interface.
    /// </summary>
    /// <author>Sam Harwell</author>
    public class Vocabulary : IVocabulary
    {
        private static readonly string[] EmptyNames = new string[0];

        /// <summary>
        ///     Gets an empty
        ///     <see cref="IVocabulary" />
        ///     instance.
        ///     <p>
        ///         No literal or symbol names are assigned to token types, so
        ///         <see cref="GetDisplayName(int)" />
        ///         returns the numeric value for all tokens
        ///         except
        ///         <see cref="TokenConstants.Eof" />
        ///         .
        ///     </p>
        /// </summary>
        [NotNull] public static readonly Vocabulary EmptyVocabulary = new(EmptyNames, EmptyNames, EmptyNames);

        [NotNull] private readonly string[] displayNames;

        [NotNull] private readonly string[] literalNames;

        [NotNull] private readonly string[] symbolicNames;

        /// <summary>
        ///     Constructs a new instance of
        ///     <see cref="Vocabulary" />
        ///     from the specified
        ///     literal and symbolic token names.
        /// </summary>
        /// <param name="literalNames">
        ///     The literal names assigned to tokens, or
        ///     <see langword="null" />
        ///     if no literal names are assigned.
        /// </param>
        /// <param name="symbolicNames">
        ///     The symbolic names assigned to tokens, or
        ///     <see langword="null" />
        ///     if no symbolic names are assigned.
        /// </param>
        /// <seealso cref="GetLiteralName(int)" />
        /// <seealso cref="GetSymbolicName(int)" />
        public Vocabulary([AllowNull] string[] literalNames, [AllowNull] string[] symbolicNames)
            : this(literalNames, symbolicNames, null)
        {
        }

        /// <summary>
        ///     Constructs a new instance of
        ///     <see cref="Vocabulary" />
        ///     from the specified
        ///     literal, symbolic, and display token names.
        /// </summary>
        /// <param name="literalNames">
        ///     The literal names assigned to tokens, or
        ///     <see langword="null" />
        ///     if no literal names are assigned.
        /// </param>
        /// <param name="symbolicNames">
        ///     The symbolic names assigned to tokens, or
        ///     <see langword="null" />
        ///     if no symbolic names are assigned.
        /// </param>
        /// <param name="displayNames">
        ///     The display names assigned to tokens, or
        ///     <see langword="null" />
        ///     to use the values in
        ///     <paramref name="literalNames" />
        ///     and
        ///     <paramref name="symbolicNames" />
        ///     as
        ///     the source of display names, as described in
        ///     <see cref="GetDisplayName(int)" />
        ///     .
        /// </param>
        /// <seealso cref="GetLiteralName(int)" />
        /// <seealso cref="GetSymbolicName(int)" />
        /// <seealso cref="GetDisplayName(int)" />
        public Vocabulary([AllowNull] string[] literalNames, [AllowNull] string[] symbolicNames, [AllowNull] string[] displayNames)
        {
            this.literalNames = literalNames != null ? literalNames : EmptyNames;
            this.symbolicNames = symbolicNames != null ? symbolicNames : EmptyNames;
            this.displayNames = displayNames != null ? displayNames : EmptyNames;
            // See note here on -1 part: https://github.com/antlr/antlr4/pull/1146
            MaxTokenType = Math.Max(this.displayNames.Length, Math.Max(this.literalNames.Length, this.symbolicNames.Length)) - 1;
        }

        public virtual int MaxTokenType { get; }

        [return: MaybeNull]
        public virtual string GetLiteralName(int tokenType)
        {
            if (tokenType >= 0 && tokenType < literalNames.Length)
            {
                return literalNames[tokenType];
            }

            return null;
        }

        [return: MaybeNull]
        public virtual string GetSymbolicName(int tokenType)
        {
            if (tokenType >= 0 && tokenType < symbolicNames.Length)
            {
                return symbolicNames[tokenType];
            }

            if (tokenType == TokenConstants.Eof)
            {
                return "EOF";
            }

            return null;
        }

        [return: NotNull]
        public virtual string GetDisplayName(int tokenType)
        {
            if (tokenType >= 0 && tokenType < displayNames.Length)
            {
                string displayName = displayNames[tokenType];
                if (displayName != null)
                {
                    return displayName;
                }
            }

            string literalName = GetLiteralName(tokenType);
            if (literalName != null)
            {
                return literalName;
            }

            string symbolicName = GetSymbolicName(tokenType);
            if (symbolicName != null)
            {
                return symbolicName;
            }

            return tokenType.ToString();
        }

        /// <summary>
        ///     Returns a
        ///     <see cref="Vocabulary" />
        ///     instance from the specified set of token
        ///     names. This method acts as a compatibility layer for the single
        ///     <paramref name="tokenNames" />
        ///     array generated by previous releases of ANTLR.
        ///     <p>
        ///         The resulting vocabulary instance returns
        ///         <see langword="null" />
        ///         for
        ///         <see cref="GetLiteralName(int)" />
        ///         and
        ///         <see cref="GetSymbolicName(int)" />
        ///         , and the
        ///         value from
        ///         <paramref name="tokenNames" />
        ///         for the display names.
        ///     </p>
        /// </summary>
        /// <param name="tokenNames">
        ///     The token names, or
        ///     <see langword="null" />
        ///     if no token names are
        ///     available.
        /// </param>
        /// <returns>
        ///     A
        ///     <see cref="IVocabulary" />
        ///     instance which uses
        ///     <paramref name="tokenNames" />
        ///     for
        ///     the display names of tokens.
        /// </returns>
        public static IVocabulary FromTokenNames([AllowNull] string[] tokenNames)
        {
            if (tokenNames == null || tokenNames.Length == 0)
            {
                return EmptyVocabulary;
            }

            string[] literalNames = Arrays.CopyOf(tokenNames, tokenNames.Length);
            string[] symbolicNames = Arrays.CopyOf(tokenNames, tokenNames.Length);
            for (int i = 0;
                i < tokenNames.Length;
                i++)
            {
                string tokenName = tokenNames[i];
                if (tokenName == null)
                {
                    continue;
                }

                if (tokenName.Length > 0)
                {
                    char firstChar = tokenName[0];
                    if (firstChar == '\'')
                    {
                        symbolicNames[i] = null;
                        continue;
                    }

                    if (Char.IsUpper(firstChar))
                    {
                        literalNames[i] = null;
                        continue;
                    }
                }

                // wasn't a literal or symbolic name
                literalNames[i] = null;
                symbolicNames[i] = null;
            }

            return new Vocabulary(literalNames, symbolicNames, tokenNames);
        }
    }
}