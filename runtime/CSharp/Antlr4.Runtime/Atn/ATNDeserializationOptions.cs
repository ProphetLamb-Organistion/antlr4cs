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
    /// <author>Sam Harwell</author>
    public class ATNDeserializationOptions
    {
        private bool generateRuleBypassTransitions;

        private bool optimize;

        private bool verifyATN;

        static ATNDeserializationOptions()
        {
            Default = new ATNDeserializationOptions();
            Default.MakeReadOnly();
        }

        public ATNDeserializationOptions()
        {
            verifyATN = true;
            generateRuleBypassTransitions = false;
            optimize = true;
        }

        public ATNDeserializationOptions(ATNDeserializationOptions options)
        {
            verifyATN = options.verifyATN;
            generateRuleBypassTransitions = options.generateRuleBypassTransitions;
            optimize = options.optimize;
        }

        [NotNull] public static ATNDeserializationOptions Default { get; }

        public bool IsReadOnly { get; private set; }

        public bool VerifyAtn
        {
            get => verifyATN;
            set
            {
                bool verifyATN = value;
                ThrowIfReadOnly();
                this.verifyATN = verifyATN;
            }
        }

        public bool GenerateRuleBypassTransitions
        {
            get => generateRuleBypassTransitions;
            set
            {
                bool generateRuleBypassTransitions = value;
                ThrowIfReadOnly();
                this.generateRuleBypassTransitions = generateRuleBypassTransitions;
            }
        }

        public bool Optimize
        {
            get => optimize;
            set
            {
                bool optimize = value;
                ThrowIfReadOnly();
                this.optimize = optimize;
            }
        }

        public void MakeReadOnly()
        {
            IsReadOnly = true;
        }

        protected internal virtual void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("The object is read only.");
            }
        }
    }
}