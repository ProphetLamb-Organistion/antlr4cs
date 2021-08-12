// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime;
using Antlr4.Tool;

namespace Antlr4.Parse
{
    /**
     * A CommonToken that can also track it's original location,
     * derived from options on the element ref like BEGIN&lt;line=34,...&gt;.
     */
    public class GrammarToken : IToken
    {
        private readonly CommonToken _token;
        public Grammar g;
        public int originalTokenIndex = -1;

        public GrammarToken(Grammar g, IToken oldToken)
        {
            this.g = g;
            _token = new CommonToken(oldToken);
        }

        public int CharPositionInLine
        {
            get
            {
                if (originalTokenIndex >= 0)
                {
                    return g.originalTokenStream.Get(originalTokenIndex).CharPositionInLine;
                }

                return _token.CharPositionInLine;
            }

            set => _token.CharPositionInLine = value;
        }

        public int Line
        {
            get
            {
                if (originalTokenIndex >= 0)
                {
                    return g.originalTokenStream.Get(originalTokenIndex).Line;
                }

                return _token.Line;
            }

            set => _token.Line = value;
        }

        public int TokenIndex
        {
            get => originalTokenIndex;

            set => _token.TokenIndex = value;
        }

        public int StartIndex
        {
            get
            {
                if (originalTokenIndex >= 0)
                {
                    return g.originalTokenStream.Get(originalTokenIndex).StartIndex;
                }

                return _token.StartIndex;
            }

            set => _token.StartIndex = value;
        }

        public int StopIndex
        {
            get
            {
                int n = _token.StopIndex - _token.StartIndex + 1;
                return StartIndex + n - 1;
            }

            set => _token.StopIndex = value;
        }

        public int Channel
        {
            get => _token.Channel;

            set => _token.Channel = value;
        }

        public ICharStream InputStream
        {
            get => _token.InputStream;

            set => _token.InputStream = value;
        }

        public string Text
        {
            get => _token.Text;

            set => _token.Text = value;
        }

        public int Type
        {
            get => _token.Type;

            set => _token.Type = value;
        }

        public override string ToString()
        {
            string channelStr = "";
            if (Channel > 0)
            {
                channelStr = ",channel=" + Channel;
            }

            string txt = Text;
            if (txt != null)
            {
                txt = txt.Replace("\n", "\\n");
                txt = txt.Replace("\r", "\\r");
                txt = txt.Replace("\t", "\\t");
            }
            else
            {
                txt = "<no text>";
            }

            return "[@" + TokenIndex + "," + StartIndex + ":" + StopIndex +
                   "='" + txt + "',<" + Type + ">" + channelStr + "," + Line + ":" + CharPositionInLine + "]";
        }
    }
}