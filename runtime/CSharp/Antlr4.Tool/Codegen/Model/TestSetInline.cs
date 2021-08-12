// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public class TestSetInline : SrcOp
    {
        public Bitset[] bitsets;
        public int bitsetWordSize;
        public string varName;

        public TestSetInline(OutputModelFactory factory, GrammarAST ast, IntervalSet set, int wordSize)
            : base(factory, ast)
        {
            bitsetWordSize = wordSize;
            Bitset[] withZeroOffset = CreateBitsets(factory, set, wordSize, true);
            Bitset[] withoutZeroOffset = CreateBitsets(factory, set, wordSize, false);
            bitsets = withZeroOffset.Length <= withoutZeroOffset.Length ? withZeroOffset : withoutZeroOffset;
            varName = "_la";
        }

        private static Bitset[] CreateBitsets(OutputModelFactory factory,
            IntervalSet set,
            int wordSize,
            bool useZeroOffset)
        {
            IList<Bitset> bitsetList = new List<Bitset>();
            foreach (int ttype in set.ToArray())
            {
                Bitset current = bitsetList.Count > 0 ? bitsetList[bitsetList.Count - 1] : null;
                if (current == null || ttype > current.shift + wordSize - 1)
                {
                    current = new Bitset();
                    if (useZeroOffset && ttype >= 0 && ttype < wordSize - 1)
                    {
                        current.shift = 0;
                    }
                    else
                    {
                        current.shift = ttype;
                    }

                    bitsetList.Add(current);
                }

                current.ttypes.Add(factory.GetTarget().GetTokenTypeAsTargetLabel(factory.GetGrammar(), ttype));
            }

            return bitsetList.ToArray();
        }

        public sealed class Bitset
        {
            public readonly IList<string> ttypes = new List<string>();
            public int shift;
        }
    }
}