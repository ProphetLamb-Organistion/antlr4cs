// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Reflection;
using System.Text;
using Antlr4.Codegen;
using Antlr4.Misc;
using Antlr4.Parse;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Utility;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Analysis
{
    /**
     * Using a tree walker on the rules, determine if a rule is directly left-recursive and if it follows
     * our pattern.
     */
    public class LeftRecursiveRuleAnalyzer : LeftRecursiveRuleWalker
    {
        public enum ASSOC
        {
            left,
            right
        }

        /**
         * Tokens from which rule AST comes from
         */
        public readonly ITokenStream tokenStream;

        public IDictionary<int, ASSOC> altAssociativity = new Dictionary<int, ASSOC>();
        public LinkedHashMap<int, LeftRecursiveRuleAltInfo> binaryAlts = new();

        [NotNull] public TemplateGroup codegenTemplates;

        public string language;

        /**
         * Pointer to ID node of ^(= ID element)
         */
        public IList<Tuple<GrammarAST, string>> leftRecursiveRuleRefLabels =
            new List<Tuple<GrammarAST, string>>();

        public IList<LeftRecursiveRuleAltInfo> prefixAndOtherAlts = new List<LeftRecursiveRuleAltInfo>();

        [NotNull] public TemplateGroup recRuleTemplates;

        public GrammarAST retvals;
        public string ruleName;
        public LinkedHashMap<int, LeftRecursiveRuleAltInfo> suffixAlts = new();
        public LinkedHashMap<int, LeftRecursiveRuleAltInfo> ternaryAlts = new();

        public AntlrTool tool;

        public LeftRecursiveRuleAnalyzer(GrammarAST ruleAST,
            AntlrTool tool, string ruleName, string language)
            : base(new CommonTreeNodeStream(new GrammarASTAdaptor(ruleAST.Token.InputStream), ruleAST))
        {
            this.tool = tool;
            this.ruleName = ruleName;
            this.language = language;
            tokenStream = ruleAST.g.tokenStream;
            if (tokenStream == null)
            {
                throw new InvalidOperationException("grammar must have a token stream");
            }

            LoadPrecRuleTemplates();
        }

        public virtual void LoadPrecRuleTemplates()
        {
            string codeBaseLocation = new Uri(typeof(AntlrTool).GetTypeInfo().Assembly.Location).LocalPath;
            string baseDirectory = Path.GetDirectoryName(codeBaseLocation);
            string templateGroupFile = Path.Combine("Tool", "Templates", "LeftRecursiveRules.stg");
            recRuleTemplates = new TemplateGroupFile(
                Path.Combine(baseDirectory, templateGroupFile),
                Encoding.UTF8);
            if (!recRuleTemplates.IsDefined("recRule"))
            {
                tool.errMgr.ToolError(ErrorType.MISSING_CODE_GEN_TEMPLATES, "LeftRecursiveRules");
            }

            // use codegen to get correct language templates; that's it though
            CodeGenerator gen = new(tool, null, language);
            TemplateGroup templates = gen.GetTemplates();
            if (templates == null)
            {
                // this class will still operate using Java templates
                templates = new CodeGenerator(tool, null, "Java").GetTemplates();
                Debug.Assert(templates != null);
            }

            codegenTemplates = templates;
        }

        public override void SetReturnValues(GrammarAST t)
        {
            retvals = t;
        }

        public override void SetAltAssoc(AltAST t, int alt)
        {
            ASSOC assoc = ASSOC.left;
            if (t.GetOptions() != null)
            {
                string a = t.GetOptionString("assoc");
                if (a != null)
                {
                    if (a.Equals(ASSOC.right.ToString()))
                    {
                        assoc = ASSOC.right;
                    }
                    else if (a.Equals(ASSOC.left.ToString()))
                    {
                        assoc = ASSOC.left;
                    }
                    else
                    {
                        tool.errMgr.GrammarError(ErrorType.ILLEGAL_OPTION_VALUE, t.g.fileName, t.GetOptionAST("assoc").Token, "assoc", assoc);
                    }
                }
            }

            if (altAssociativity.ContainsKey(alt) && altAssociativity[alt] != assoc)
            {
                tool.errMgr.ToolError(ErrorType.INTERNAL_ERROR, "all operators of alt " + alt + " of left-recursive rule must have same associativity");
            }

            altAssociativity[alt] = assoc;

            //		System.out.println("setAltAssoc: op " + alt + ": " + t.getText()+", assoc="+assoc);
        }

        public override void BinaryAlt(AltAST originalAltTree, int alt)
        {
            AltAST altTree = (AltAST) originalAltTree.DupTree();
            string altLabel = altTree.altLabel != null ? altTree.altLabel.Text : null;

            string label = null;
            bool isListLabel = false;
            GrammarAST lrlabel = StripLeftRecursion(altTree);
            if (lrlabel != null)
            {
                label = lrlabel.Text;
                isListLabel = lrlabel.Parent.Type == PLUS_ASSIGN;
                leftRecursiveRuleRefLabels.Add(Tuple.Create(lrlabel, altLabel));
            }

            StripAltLabel(altTree);

            // rewrite e to be e_[rec_arg]
            int nextPrec = NextPrecedence(alt);
            altTree = AddPrecedenceArgToRules(altTree, nextPrec);

            StripAltLabel(altTree);
            string altText = Text(altTree);
            altText = altText.Trim();
            LeftRecursiveRuleAltInfo a =
                new(alt, altText, label, altLabel, isListLabel, originalAltTree);
            a.nextPrec = nextPrec;
            binaryAlts[alt] = a;
            //System.out.println("binaryAlt " + alt + ": " + altText + ", rewrite=" + rewriteText);
        }

        public override void PrefixAlt(AltAST originalAltTree, int alt)
        {
            AltAST altTree = (AltAST) originalAltTree.DupTree();
            StripAltLabel(altTree);

            int nextPrec = Precedence(alt);
            // rewrite e to be e_[prec]
            altTree = AddPrecedenceArgToRules(altTree, nextPrec);
            string altText = Text(altTree);
            altText = altText.Trim();
            string altLabel = altTree.altLabel != null ? altTree.altLabel.Text : null;
            LeftRecursiveRuleAltInfo a =
                new(alt, altText, null, altLabel, false, originalAltTree);
            a.nextPrec = nextPrec;
            prefixAndOtherAlts.Add(a);
            //System.out.println("prefixAlt " + alt + ": " + altText + ", rewrite=" + rewriteText);
        }

        public override void SuffixAlt(AltAST originalAltTree, int alt)
        {
            AltAST altTree = (AltAST) originalAltTree.DupTree();
            string altLabel = altTree.altLabel != null ? altTree.altLabel.Text : null;

            string label = null;
            bool isListLabel = false;
            GrammarAST lrlabel = StripLeftRecursion(altTree);
            if (lrlabel != null)
            {
                label = lrlabel.Text;
                isListLabel = lrlabel.Parent.Type == PLUS_ASSIGN;
                leftRecursiveRuleRefLabels.Add(Tuple.Create(lrlabel, altLabel));
            }

            StripAltLabel(altTree);
            string altText = Text(altTree);
            altText = altText.Trim();
            LeftRecursiveRuleAltInfo a =
                new(alt, altText, label, altLabel, isListLabel, originalAltTree);
            suffixAlts[alt] = a;
            //		System.out.println("suffixAlt " + alt + ": " + altText + ", rewrite=" + rewriteText);
        }

        public override void OtherAlt(AltAST originalAltTree, int alt)
        {
            AltAST altTree = (AltAST) originalAltTree.DupTree();
            StripAltLabel(altTree);
            string altText = Text(altTree);
            string altLabel = altTree.altLabel != null ? altTree.altLabel.Text : null;
            LeftRecursiveRuleAltInfo a =
                new(alt, altText, null, altLabel, false, originalAltTree);
            // We keep other alts with prefix alts since they are all added to the start of the generated rule, and
            // we want to retain any prior ordering between them
            prefixAndOtherAlts.Add(a);
            //		System.out.println("otherAlt " + alt + ": " + altText);
        }

        // --------- get transformed rules ----------------

        public virtual string GetArtificialOpPrecRule()
        {
            Template ruleST = recRuleTemplates.GetInstanceOf("recRule");
            ruleST.Add("ruleName", ruleName);
            Template ruleArgST = codegenTemplates.GetInstanceOf("recRuleArg");
            ruleST.Add("argName", ruleArgST);
            Template setResultST = codegenTemplates.GetInstanceOf("recRuleSetResultAction");
            ruleST.Add("setResultAction", setResultST);
            ruleST.Add("userRetvals", retvals);

            var opPrecRuleAlts = new LinkedHashMap<int, LeftRecursiveRuleAltInfo>();
            foreach (KeyValuePair<int, LeftRecursiveRuleAltInfo> pair in binaryAlts)
            {
                opPrecRuleAlts[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<int, LeftRecursiveRuleAltInfo> pair in ternaryAlts)
            {
                opPrecRuleAlts[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<int, LeftRecursiveRuleAltInfo> pair in suffixAlts)
            {
                opPrecRuleAlts[pair.Key] = pair.Value;
            }

            foreach (int alt in opPrecRuleAlts.Keys)
            {
                LeftRecursiveRuleAltInfo altInfo = opPrecRuleAlts[alt];
                Template altST = recRuleTemplates.GetInstanceOf("recRuleAlt");
                Template predST = codegenTemplates.GetInstanceOf("recRuleAltPredicate");
                predST.Add("opPrec", Precedence(alt));
                predST.Add("ruleName", ruleName);
                altST.Add("pred", predST);
                altST.Add("alt", altInfo);
                altST.Add("precOption", LeftRecursiveRuleTransformer.PRECEDENCE_OPTION_NAME);
                altST.Add("opPrec", Precedence(alt));
                ruleST.Add("opAlts", altST);
            }

            ruleST.Add("primaryAlts", prefixAndOtherAlts);

            tool.Log("left-recursion", ruleST.Render());

            return ruleST.Render();
        }

        public virtual AltAST AddPrecedenceArgToRules(AltAST t, int prec)
        {
            if (t == null)
            {
                return null;
            }

            // get all top-level rule refs from ALT
            IList<GrammarAST> outerAltRuleRefs = t.GetNodesWithTypePreorderDFS(IntervalSet.Of(RULE_REF));
            foreach (GrammarAST x in outerAltRuleRefs)
            {
                RuleRefAST rref = (RuleRefAST) x;
                bool recursive = rref.Text.Equals(ruleName);
                bool rightmost = rref == outerAltRuleRefs[outerAltRuleRefs.Count - 1];
                if (recursive && rightmost)
                {
                    GrammarAST dummyValueNode = new(new CommonToken(ANTLRParser.INT, "" + prec));
                    rref.SetOption(LeftRecursiveRuleTransformer.PRECEDENCE_OPTION_NAME, dummyValueNode);
                }
            }

            return t;
        }

        /**
         * Match (RULE RULE_REF (BLOCK (ALT .*) (ALT RULE_REF[self] .*) (ALT .*)))
         * Match (RULE RULE_REF (BLOCK (ALT .*) (ALT (ASSIGN ID RULE_REF[self]) .*) (ALT .*)))
         */
        public static bool HasImmediateRecursiveRuleRefs(GrammarAST t, string ruleName)
        {
            if (t == null)
            {
                return false;
            }

            GrammarAST blk = (GrammarAST) t.GetFirstChildWithType(BLOCK);
            if (blk == null)
            {
                return false;
            }

            int n = blk.Children.Count;
            for (int i = 0;
                i < n;
                i++)
            {
                GrammarAST alt = (GrammarAST) blk.Children[i];
                ITree first = alt.GetChild(0);
                if (first == null)
                {
                    continue;
                }

                if (first.Type == ELEMENT_OPTIONS)
                {
                    first = alt.GetChild(1);
                    if (first == null)
                    {
                        continue;
                    }
                }

                if (first.Type == RULE_REF && first.Text.Equals(ruleName))
                {
                    return true;
                }

                ITree rref = first.GetChild(1);
                if (rref != null && rref.Type == RULE_REF && rref.Text.Equals(ruleName))
                {
                    return true;
                }
            }

            return false;
        }

        // TODO: this strips the tree properly, but since text()
        // uses the start of stop token index and gets text from that
        // ineffectively ignores this routine.
        public virtual GrammarAST StripLeftRecursion(GrammarAST altAST)
        {
            GrammarAST lrlabel = null;
            GrammarAST first = (GrammarAST) altAST.GetChild(0);
            int leftRecurRuleIndex = 0;
            if (first.Type == ELEMENT_OPTIONS)
            {
                first = (GrammarAST) altAST.GetChild(1);
                leftRecurRuleIndex = 1;
            }

            ITree rref = first.GetChild(1); // if label=rule
            if (first.Type == RULE_REF && first.Text.Equals(ruleName) ||
                rref != null && rref.Type == RULE_REF && rref.Text.Equals(ruleName))
            {
                if (first.Type == ASSIGN || first.Type == PLUS_ASSIGN)
                {
                    lrlabel = (GrammarAST) first.GetChild(0);
                }

                // remove rule ref (first child unless options present)
                altAST.DeleteChild(leftRecurRuleIndex);
                // reset index so it prints properly (sets token range of
                // ALT to start to right of left recur rule we deleted)
                GrammarAST newFirstChild = (GrammarAST) altAST.GetChild(leftRecurRuleIndex);
                altAST.TokenStartIndex = newFirstChild.TokenStartIndex;
            }

            return lrlabel;
        }

        /**
         * Strip last 2 tokens if → label; alter indexes in altAST
         */
        public virtual void StripAltLabel(GrammarAST altAST)
        {
            int start = altAST.TokenStartIndex;
            int stop = altAST.TokenStopIndex;
            // find =>
            for (int i = stop;
                i >= start;
                i--)
            {
                if (tokenStream.Get(i).Type == POUND)
                {
                    altAST.TokenStopIndex = i - 1;
                    return;
                }
            }
        }

        public virtual string Text(GrammarAST t)
        {
            if (t == null)
            {
                return "";
            }

            int tokenStartIndex = t.TokenStartIndex;
            int tokenStopIndex = t.TokenStopIndex;

            // ignore tokens from existing option subtrees like:
            //    (ELEMENT_OPTIONS (= assoc right))
            //
            // element options are added back according to the values in the map
            // returned by getOptions().
            IntervalSet ignore = new();
            IList<GrammarAST> optionsSubTrees = t.GetNodesWithType(ELEMENT_OPTIONS);
            foreach (GrammarAST sub in optionsSubTrees)
            {
                ignore.Add(sub.TokenStartIndex, sub.TokenStopIndex);
            }

            // Individual labels appear as RULE_REF or TOKEN_REF tokens in the tree,
            // but do not support the ELEMENT_OPTIONS syntax. Make sure to not try
            // and add the tokenIndex option when writing these tokens.
            IntervalSet noOptions = new();
            IList<GrammarAST> labeledSubTrees = t.GetNodesWithType(new IntervalSet(ASSIGN, PLUS_ASSIGN));
            foreach (GrammarAST sub in labeledSubTrees)
            {
                noOptions.Add(sub.GetChild(0).TokenStartIndex);
            }

            StringBuilder buf = new();
            int i = tokenStartIndex;
            while (i <= tokenStopIndex)
            {
                if (ignore.Contains(i))
                {
                    i++;
                    continue;
                }

                IToken tok = tokenStream.Get(i);

                // Compute/hold any element options
                StringBuilder elementOptions = new();
                if (!noOptions.Contains(i))
                {
                    GrammarAST node = t.GetNodeWithTokenIndex(tok.TokenIndex);
                    if (node != null &&
                        (tok.Type == TOKEN_REF ||
                         tok.Type == STRING_LITERAL ||
                         tok.Type == RULE_REF))
                    {
                        elementOptions.Append("tokenIndex=").Append(tok.TokenIndex);
                    }

                    if (node is GrammarASTWithOptions)
                    {
                        GrammarASTWithOptions o = (GrammarASTWithOptions) node;
                        foreach (KeyValuePair<string, GrammarAST> entry in o.GetOptions())
                        {
                            if (elementOptions.Length > 0)
                            {
                                elementOptions.Append(',');
                            }

                            elementOptions.Append(entry.Key);
                            elementOptions.Append('=');
                            elementOptions.Append(entry.Value.Text);
                        }
                    }
                }

                buf.Append(tok.Text); // add actual text of the current token to the rewritten alternative
                i++; // move to the next token

                // Are there args on a rule?
                if (tok.Type == RULE_REF && i <= tokenStopIndex && tokenStream.Get(i).Type == ARG_ACTION)
                {
                    buf.Append('[' + tokenStream.Get(i).Text + ']');
                    i++;
                }

                // now that we have the actual element, we can add the options.
                if (elementOptions.Length > 0)
                {
                    buf.Append('<').Append(elementOptions).Append('>');
                }
            }

            return buf.ToString();
        }

        public virtual int Precedence(int alt)
        {
            return numAlts - alt + 1;
        }

        // Assumes left assoc
        public virtual int NextPrecedence(int alt)
        {
            int p = Precedence(alt);
            ASSOC assoc;
            if (altAssociativity.TryGetValue(alt, out assoc) && assoc == ASSOC.right)
            {
                return p;
            }

            return p + 1;
        }

        public override string ToString()
        {
            return "PrecRuleOperatorCollector{" +
                   "binaryAlts=" + binaryAlts +
                   ", ternaryAlts=" + ternaryAlts +
                   ", suffixAlts=" + suffixAlts +
                   ", prefixAndOtherAlts=" + prefixAndOtherAlts +
                   '}';
        }
    }
}