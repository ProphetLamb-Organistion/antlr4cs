// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Reflection;
using System.Text;
using Antlr4.Codegen.Model;
using Antlr4.Runtime;
using Antlr4.Runtime.Utility;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen
{
    /** */
    public abstract class AbstractTarget
    {
        protected readonly CodeGenerator gen;
        private readonly string language;

        /**
         * For pure strings of Java 16-bit Unicode char, how can we display
         * it in the target language as a literal.  Useful for dumping
         * predicates and such that may refer to chars that need to be escaped
         * when represented as strings.  Also, templates need to be escaped so
         * that the target language can hold them as a string.
         * <p>
         *     I have defined (via the constructor) the set of typical escapes,
         *     but your {@link Target} subclass is free to alter the translated chars
         *     or add more definitions.  This is non-static so each target can have
         *     a different set in memory at same time.
         * </p>
         */
        protected string[] targetCharValueEscape = new string[255];

        private TemplateGroup templates;

        protected AbstractTarget(CodeGenerator gen, string language)
        {
            targetCharValueEscape['\n'] = "\\n";
            targetCharValueEscape['\r'] = "\\r";
            targetCharValueEscape['\t'] = "\\t";
            targetCharValueEscape['\b'] = "\\b";
            targetCharValueEscape['\f'] = "\\f";
            targetCharValueEscape['\\'] = "\\\\";
            targetCharValueEscape['\''] = "\\'";
            targetCharValueEscape['"'] = "\\\"";
            this.gen = gen;
            this.language = language;
        }

        public virtual CodeGenerator GetCodeGenerator()
        {
            return gen;
        }

        public virtual string GetLanguage()
        {
            return language;
        }

        [return: NotNull]
        public virtual TemplateGroup GetTemplates()
        {
            if (templates == null)
            {
                templates = LoadTemplates();
            }

            return templates;
        }

        protected internal virtual void GenFile(Grammar g, Template outputFileST, string fileName)
        {
            GetCodeGenerator().Write(outputFileST, fileName);
        }

        /**
         * Get a meaningful name for a token type useful during code generation.
         * Literals without associated names are converted to the string equivalent
         * of their integer values. Used to generate x==ID and x==34 type comparisons
         * etc...  Essentially we are looking for the most obvious way to refer
         * to a token type in the generated code.
         */
        public virtual string GetTokenTypeAsTargetLabel(Grammar g, int ttype)
        {
            string name = g.GetTokenName(ttype);
            // If name is not valid, return the token type instead
            if (Grammar.INVALID_TOKEN_NAME.Equals(name))
            {
                return ttype.ToString();
            }

            return name;
        }

        public virtual string[] GetTokenTypesAsTargetLabels(Grammar g, int[] ttypes)
        {
            string[] labels = new string[ttypes.Length];
            for (int i = 0;
                i < ttypes.Length;
                i++)
            {
                labels[i] = GetTokenTypeAsTargetLabel(g, ttypes[i]);
            }

            return labels;
        }

        /**
         * Given a random string of Java unicode chars, return a new string with
         * optionally appropriate quote characters for target language and possibly
         * with some escaped characters.  For example, if the incoming string has
         * actual newline characters, the output of this method would convert them
         * to the two char sequence \n for Java, C, C++, ...  The new string has
         * double-quotes around it as well.  Example String in memory:
         * 
         * a"[newlinechar]b'c[carriagereturnchar]d[tab]e\f
         * 
         * would be converted to the valid Java s:
         * 
         * "a\"\nb'c\rd\te\\f"
         * 
         * or
         * 
         * a\"\nb'c\rd\te\\f
         * 
         * depending on the quoted arg.
         */
        public virtual string GetTargetStringLiteralFromString(string s, bool quoted)
        {
            if (s == null)
            {
                return null;
            }

            StringBuilder buf = new();
            if (quoted)
            {
                buf.Append('"');
            }

            for (int i = 0;
                i < s.Length;
                i++)
            {
                int c = s[i];
                if (c != '\'' && // don't escape single quotes in strings for java
                    c < targetCharValueEscape.Length &&
                    targetCharValueEscape[c] != null)
                {
                    buf.Append(targetCharValueEscape[c]);
                }
                else
                {
                    buf.Append((char) c);
                }
            }

            if (quoted)
            {
                buf.Append('"');
            }

            return buf.ToString();
        }

        public virtual string GetTargetStringLiteralFromString(string s)
        {
            return GetTargetStringLiteralFromString(s, true);
        }

        /**
         * Convert from an ANTLR string literal found in a grammar file to an
         * equivalent string literal in the target language.
         */
        public abstract string GetTargetStringLiteralFromANTLRStringLiteral(
            CodeGenerator generator,
            string literal, bool addQuotes);

        /**
         * Assume 16-bit char
         */
        public abstract string EncodeIntAsCharEscape(int v);

        public virtual string GetLoopLabel(GrammarAST ast)
        {
            return "loop" + ast.Token.TokenIndex;
        }

        public virtual string GetLoopCounter(GrammarAST ast)
        {
            return "cnt" + ast.Token.TokenIndex;
        }

        public virtual string GetListLabel(string label)
        {
            Template st = GetTemplates().GetInstanceOf("ListLabelName");
            st.Add("label", label);
            return st.Render();
        }

        public virtual string GetRuleFunctionContextStructName(Rule r)
        {
            if (r.g.IsLexer())
            {
                return GetTemplates().GetInstanceOf("LexerRuleContext").Render();
            }

            string baseName = r.GetBaseContext();
            return Utils.Capitalize(baseName) + GetTemplates().GetInstanceOf("RuleContextNameSuffix").Render();
        }

        public virtual string GetAltLabelContextStructName(string label)
        {
            return Utils.Capitalize(label) + GetTemplates().GetInstanceOf("RuleContextNameSuffix").Render();
        }

        /**
         * If we know which actual function, we can provide the actual ctx type.
         * This will contain implicit labels etc...  From outside, though, we
         * see only ParserRuleContext unless there are externally visible stuff
         * like args, locals, explicit labels, etc...
         */
        public virtual string GetRuleFunctionContextStructName(RuleFunction function)
        {
            Rule r = function.rule;
            if (r.g.IsLexer())
            {
                return GetTemplates().GetInstanceOf("LexerRuleContext").Render();
            }

            string baseName = r.GetBaseContext();
            return Utils.Capitalize(baseName) + GetTemplates().GetInstanceOf("RuleContextNameSuffix").Render();
        }

        // should be same for all refs to same token like ctx.ID within single rule function
        // for literals like 'while', we gen _s<ttype>
        public virtual string GetImplicitTokenLabel(string tokenName)
        {
            Template st = GetTemplates().GetInstanceOf("ImplicitTokenLabel");
            int ttype = GetCodeGenerator().g.GetTokenType(tokenName);
            if (tokenName.StartsWith("'"))
            {
                return "s" + ttype;
            }

            string text = GetTokenTypeAsTargetLabel(GetCodeGenerator().g, ttype);
            st.Add("tokenName", text);
            return st.Render();
        }

        // x=(A|B)
        public virtual string GetImplicitSetLabel(string id)
        {
            Template st = GetTemplates().GetInstanceOf("ImplicitSetLabel");
            st.Add("id", id);
            return st.Render();
        }

        public virtual string GetImplicitRuleLabel(string ruleName)
        {
            Template st = GetTemplates().GetInstanceOf("ImplicitRuleLabel");
            st.Add("ruleName", ruleName);
            return st.Render();
        }

        public virtual string GetElementListName(string name)
        {
            Template st = GetTemplates().GetInstanceOf("ElementListName");
            st.Add("elemName", GetElementName(name));
            return st.Render();
        }

        public virtual string GetElementName(string name)
        {
            if (".".Equals(name))
            {
                return "_wild";
            }

            if (GetCodeGenerator().g.GetRule(name) != null)
            {
                return name;
            }

            int ttype = GetCodeGenerator().g.GetTokenType(name);
            if (ttype == TokenConstants.InvalidType)
            {
                return name;
            }

            return GetTokenTypeAsTargetLabel(GetCodeGenerator().g, ttype);
        }

        /**
         * Generate TParser.java and TLexer.java from T.g4 if combined, else
         * just use T.java as output regardless of type.
         */
        public virtual string GetRecognizerFileName(bool header)
        {
            Template extST = GetTemplates().GetInstanceOf("codeFileExtension");
            string recognizerName = gen.g.GetRecognizerName();
            return recognizerName + extST.Render();
        }

        /**
         * A given grammar T, return the listener name such as
         * TListener.java, if we're using the Java target.
         */
        public virtual string GetListenerFileName(bool header)
        {
            Debug.Assert(gen.g.name != null);
            Template extST = GetTemplates().GetInstanceOf("codeFileExtension");
            string listenerName = gen.g.name + "Listener";
            return listenerName + extST.Render();
        }

        /**
         * A given grammar T, return the visitor name such as
         * TVisitor.java, if we're using the Java target.
         */
        public virtual string GetVisitorFileName(bool header)
        {
            Debug.Assert(gen.g.name != null);
            Template extST = GetTemplates().GetInstanceOf("codeFileExtension");
            string listenerName = gen.g.name + "Visitor";
            return listenerName + extST.Render();
        }

        /**
         * A given grammar T, return a blank listener implementation
         * such as TBaseListener.java, if we're using the Java target.
         */
        public virtual string GetBaseListenerFileName(bool header)
        {
            Debug.Assert(gen.g.name != null);
            Template extST = GetTemplates().GetInstanceOf("codeFileExtension");
            string listenerName = gen.g.name + "BaseListener";
            return listenerName + extST.Render();
        }

        /**
         * A given grammar T, return a blank listener implementation
         * such as TBaseListener.java, if we're using the Java target.
         */
        public virtual string GetBaseVisitorFileName(bool header)
        {
            Debug.Assert(gen.g.name != null);
            Template extST = GetTemplates().GetInstanceOf("codeFileExtension");
            string listenerName = gen.g.name + "BaseVisitor";
            return listenerName + extST.Render();
        }

        /**
         * Gets the maximum number of 16-bit unsigned integers that can be encoded
         * in a single segment of the serialized ATN.
         * 
         * @see SerializedATN#getSegments
         * 
         * @return the serialized ATN segment limit
         */
        public virtual int GetSerializedATNSegmentLimit()
        {
            return Int32.MaxValue;
        }

        /**
         * How many bits should be used to do inline token type tests? Java assumes
         * a 64-bit word for bitsets.  Must be a valid wordsize for your target like
         * 8, 16, 32, 64, etc...
         * 
         * @since 4.5
         */
        public virtual int GetInlineTestSetWordSize()
        {
            return 64;
        }

        public virtual bool GrammarSymbolCausesIssueInGeneratedCode(GrammarAST idNode)
        {
            switch (idNode.Parent.Type)
            {
                case ANTLRParser.ASSIGN:
                    switch (idNode.Parent.Parent.Type)
                    {
                        case ANTLRParser.ELEMENT_OPTIONS:
                        case ANTLRParser.OPTIONS:
                            return false;
                    }

                    break;

                case ANTLRParser.AT:
                case ANTLRParser.ELEMENT_OPTIONS:
                    return false;

                case ANTLRParser.LEXER_ACTION_CALL:
                    if (idNode.ChildIndex == 0)
                    {
                        // first child is the command name which is part of the ANTLR language
                        return false;
                    }

                    // arguments to the command should be checked
                    break;
            }

            return VisibleGrammarSymbolCausesIssueInGeneratedCode(idNode);
        }

        protected abstract bool VisibleGrammarSymbolCausesIssueInGeneratedCode(GrammarAST idNode);

        [return: NotNull]
        protected virtual TemplateGroup LoadTemplates()
        {
            string codeBaseLocation = new Uri(typeof(AntlrTool).GetTypeInfo().Assembly.Location).LocalPath;
            string baseDirectory = Path.GetDirectoryName(codeBaseLocation);
            TemplateGroup result = new TemplateGroupFile(
                Path.Combine(
                    baseDirectory,
                    Path.Combine(CodeGenerator.TEMPLATE_ROOT, GetLanguage(), GetLanguage() + TemplateGroup.GroupFileExtension)),
                Encoding.UTF8);
            result.RegisterRenderer(typeof(int), new NumberRenderer());
            result.RegisterRenderer(typeof(string), new StringRenderer());
            result.Listener = new ErrorListener(this);

            return result;
        }

        /**
         * @since 4.3
         */
        public virtual bool WantsBaseListener()
        {
            return true;
        }

        /**
         * @since 4.3
         */
        public virtual bool WantsBaseVisitor()
        {
            return true;
        }

        /**
         * @since 4.3
         */
        public virtual bool SupportsOverloadedMethods()
        {
            return true;
        }

        /**
         * @since 4.6
         */
        public virtual bool NeedsHeader()
        {
            // Override in targets that need header files.
            return false;
        }

        protected class ErrorListener : ITemplateErrorListener
        {
            private readonly AbstractTarget target;

            public ErrorListener(AbstractTarget target)
            {
                this.target = target;
            }

            public virtual void CompiletimeError(TemplateMessage msg)
            {
                ReportError(msg);
            }

            public virtual void RuntimeError(TemplateMessage msg)
            {
                ReportError(msg);
            }

            public virtual void IOError(TemplateMessage msg)
            {
                ReportError(msg);
            }

            public virtual void InternalError(TemplateMessage msg)
            {
                ReportError(msg);
            }

            protected virtual void ReportError(TemplateMessage msg)
            {
                target.GetCodeGenerator().tool.errMgr.ToolError(ErrorType.STRING_TEMPLATE_WARNING, msg.Cause, msg.ToString());
            }
        }
    }
}