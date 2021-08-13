// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Antlr4.Misc;
using Antlr4.Parse;
using Antlr4.Runtime;
using Antlr4.Semantics;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Analysis
{
    /**
     * Remove left-recursive rule refs, add precedence args to recursive rule refs.
     * Rewrite rule so we can create ATN.
     * 
     * MODIFIES grammar AST in place.
     */
    public class LeftRecursiveRuleTransformer
    {
        public static readonly string PRECEDENCE_OPTION_NAME = "p";
        public static readonly string TOKENINDEX_OPTION_NAME = "tokenIndex";

        public GrammarRootAST ast;
        public Grammar g;
        public ICollection<Rule> rules;
        public AntlrTool tool;

        public LeftRecursiveRuleTransformer(GrammarRootAST ast, ICollection<Rule> rules, Grammar g)
        {
            this.ast = ast;
            this.rules = rules;
            this.g = g;
            tool = g.tool;
        }

        public virtual void TranslateLeftRecursiveRules()
        {
            string language = g.GetOptionString("language");
            // translate all recursive rules
            IList<string> leftRecursiveRuleNames = new List<string>();
            foreach (Rule r in rules)
            {
                if (!Grammar.IsTokenName(r.name))
                {
                    if (LeftRecursiveRuleAnalyzer.HasImmediateRecursiveRuleRefs(r.ast, r.name))
                    {
                        bool fitsPattern = TranslateLeftRecursiveRule(ast, (LeftRecursiveRule) r, language);
                        if (fitsPattern)
                        {
                            leftRecursiveRuleNames.Add(r.name);
                        }
                    }
                }
            }

            // update all refs to recursive rules to have [0] argument
            foreach (GrammarAST r in ast.GetNodesWithType(ANTLRParser.RULE_REF))
            {
                if (r.Parent.Type == ANTLRParser.RULE)
                {
                    continue; // must be rule def
                }

                if (((GrammarASTWithOptions) r).GetOptionString(PRECEDENCE_OPTION_NAME) != null)
                {
                    continue; // already has arg; must be in rewritten rule
                }

                if (leftRecursiveRuleNames.Contains(r.Text))
                {
                    // found ref to recursive rule not already rewritten with arg
                    ((GrammarASTWithOptions) r).SetOption(PRECEDENCE_OPTION_NAME, (GrammarAST) new GrammarASTAdaptor().Create(ANTLRParser.INT, "0"));
                }
            }
        }

        /**
         * Return true if successful
         */
        public virtual bool TranslateLeftRecursiveRule(GrammarRootAST ast,
            LeftRecursiveRule r,
            string language)
        {
            //tool.log("grammar", ruleAST.toStringTree());
            GrammarAST prevRuleAST = r.ast;
            string ruleName = prevRuleAST.GetChild(0).Text;
            LeftRecursiveRuleAnalyzer leftRecursiveRuleWalker =
                new(prevRuleAST, tool, ruleName, language);
            bool isLeftRec;
            try
            {
                //System.Console.WriteLine("TESTING ---------------\n" +
                //                   leftRecursiveRuleWalker.Text(ruleAST));
                isLeftRec = leftRecursiveRuleWalker.rec_rule();
            }
            catch (RecognitionException)
            {
                isLeftRec = false; // didn't match; oh well
            }

            if (!isLeftRec)
            {
                return false;
            }

            // replace old rule's AST; first create text of altered rule
            GrammarAST RULES = (GrammarAST) ast.GetFirstChildWithType(ANTLRParser.RULES);
            string newRuleText = leftRecursiveRuleWalker.GetArtificialOpPrecRule();
            //System.Console.WriteLine("created: " + newRuleText);
            // now parse within the context of the grammar that originally created
            // the AST we are transforming. This could be an imported grammar so
            // we cannot just reference this.g because the role might come from
            // the imported grammar and not the root grammar (this.g)
            RuleAST t = ParseArtificialRule(prevRuleAST.g, newRuleText);

            // reuse the name token from the original AST since it refers to the proper source location in the original grammar
            ((GrammarAST) t.GetChild(0)).Token = ((GrammarAST) prevRuleAST.GetChild(0)).Token;

            // update grammar AST and set rule's AST.
            RULES.SetChild(prevRuleAST.ChildIndex, t);
            r.ast = t;

            // Reduce sets in newly created rule tree
            GrammarTransformPipeline transform = new(g, g.tool);
            transform.ReduceBlocksToSets(r.ast);
            transform.ExpandParameterizedLoops(r.ast);

            // Rerun semantic checks on the new rule
            RuleCollector ruleCollector = new(g);
            ruleCollector.Visit(t, "rule");
            BasicSemanticChecks basics = new(g, ruleCollector);
            // disable the assoc element option checks because they are already
            // handled for the pre-transformed rule.
            basics.checkAssocElementOption = false;
            basics.Visit(t, "rule");

            // track recursive alt info for codegen
            r.recPrimaryAlts = new List<LeftRecursiveRuleAltInfo>();
            foreach (LeftRecursiveRuleAltInfo altInfo in leftRecursiveRuleWalker.prefixAndOtherAlts)
            {
                r.recPrimaryAlts.Add(altInfo);
            }

            if (r.recPrimaryAlts.Count == 0)
            {
                tool.errMgr.GrammarError(ErrorType.NO_NON_LR_ALTS, g.fileName, ((GrammarAST) r.ast.GetChild(0)).Token, r.name);
            }

            r.recOpAlts = new OrderedHashMap<int, LeftRecursiveRuleAltInfo>();
            foreach (KeyValuePair<int, LeftRecursiveRuleAltInfo> pair in leftRecursiveRuleWalker.binaryAlts)
            {
                r.recOpAlts[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<int, LeftRecursiveRuleAltInfo> pair in leftRecursiveRuleWalker.ternaryAlts)
            {
                r.recOpAlts[pair.Key] = pair.Value;
            }

            foreach (KeyValuePair<int, LeftRecursiveRuleAltInfo> pair in leftRecursiveRuleWalker.suffixAlts)
            {
                r.recOpAlts[pair.Key] = pair.Value;
            }

            // walk alt info records and set their altAST to point to appropriate ALT subtree
            // from freshly created AST
            SetAltASTPointers(r, t);

            // update Rule to just one alt and add prec alt
            ActionAST arg = (ActionAST) r.ast.GetFirstChildWithType(ANTLRParser.ARG_ACTION);
            if (arg != null)
            {
                r.args = ScopeParser.ParseTypedArgList(arg, arg.Text, g);
                r.args.type = AttributeDict.DictType.ARG;
                r.args.ast = arg;
                arg.resolver = r.alt[1]; // todo: isn't this Rule or something?
            }

            // define labels on recursive rule refs we delete; they don't point to nodes of course
            // these are so $label in action translation works
            foreach (Tuple<GrammarAST, string> pair in leftRecursiveRuleWalker.leftRecursiveRuleRefLabels)
            {
                GrammarAST labelNode = pair.Item1;
                GrammarAST labelOpNode = (GrammarAST) labelNode.Parent;
                GrammarAST elementNode = (GrammarAST) labelOpNode.GetChild(1);
                LabelElementPair lp = new(g, labelNode, elementNode, labelOpNode.Type);
                r.alt[1].labelDefs.Map(labelNode.Text, lp);
            }

            // copy to rule from walker
            r.leftRecursiveRuleRefLabels = leftRecursiveRuleWalker.leftRecursiveRuleRefLabels;

            tool.Log("grammar", "added: " + t.ToStringTree());
            return true;
        }

        public virtual RuleAST ParseArtificialRule(Grammar g, string ruleText)
        {
            ANTLRLexer lexer = new ANTLRLexer(new ANTLRStringStream(ruleText));
            GrammarASTAdaptor adaptor = new(lexer.CharStream);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            lexer.tokens = tokens;
            ToolANTLRParser p = new(tokens, tool);
            p.TreeAdaptor = adaptor;
            IToken ruleStart = null;
            try
            {
                AstParserRuleReturnScope<GrammarAST, IToken> r = p.rule();
                RuleAST tree = (RuleAST) r.Tree;
                ruleStart = r.Start;
                GrammarTransformPipeline.SetGrammarPtr(g, tree);
                GrammarTransformPipeline.AugmentTokensWithOriginalPosition(g, tree);
                return tree;
            }
            catch (Exception e)
            {
                tool.errMgr.ToolError(ErrorType.INTERNAL_ERROR,
                    e,
                    ruleStart,
                    "error parsing rule created during left-recursion detection: " + ruleText);
            }

            return null;
        }

        /**
         * <pre>
         *     (RULE e int _p (returns int v)
         *     (BLOCK
         *     (ALT
         *     (BLOCK
         *     (ALT INT {$v = $INT.int;})
         *     (ALT '(' (= x e) ')' {$v = $x.v;})
         *     (ALT ID))
         *     (* (BLOCK
         *     (OPTIONS ...)
         *     (ALT {7 &gt;= $_p}? '*' (= b e) {$v = $a.v * $b.v;})
         *     (ALT {6 &gt;= $_p}? '+' (= b e) {$v = $a.v + $b.v;})
         *     (ALT {3 &gt;= $_p}? '++') (ALT {2 &gt;= $_p}? '--'))))))
         * </pre>
         */
        public virtual void SetAltASTPointers(LeftRecursiveRule r, RuleAST t)
        {
            //System.Console.WriteLine("RULE: " + t.ToStringTree());
            BlockAST ruleBlk = (BlockAST) t.GetFirstChildWithType(ANTLRParser.BLOCK);
            AltAST mainAlt = (AltAST) ruleBlk.GetChild(0);
            BlockAST primaryBlk = (BlockAST) mainAlt.GetChild(0);
            BlockAST opsBlk = (BlockAST) mainAlt.GetChild(1).GetChild(0); // (* BLOCK ...)
            for (int i = 0;
                i < r.recPrimaryAlts.Count;
                i++)
            {
                LeftRecursiveRuleAltInfo altInfo = r.recPrimaryAlts[i];
                altInfo.altAST = (AltAST) primaryBlk.GetChild(i);
                altInfo.altAST.leftRecursiveAltInfo = altInfo;
                altInfo.originalAltAST.leftRecursiveAltInfo = altInfo;
                //altInfo.originalAltAST.Parent = altInfo.altAST.Parent;
                //System.Console.WriteLine(altInfo.altAST.ToStringTree());
            }

            for (int i = 0;
                i < r.recOpAlts.Count;
                i++)
            {
                LeftRecursiveRuleAltInfo altInfo = r.recOpAlts.GetElement(i);
                altInfo.altAST = (AltAST) opsBlk.GetChild(i);
                altInfo.altAST.leftRecursiveAltInfo = altInfo;
                altInfo.originalAltAST.leftRecursiveAltInfo = altInfo;
                //altInfo.originalAltAST.Parent = altInfo.altAST.Parent;
                //System.Console.WriteLine(altInfo.altAST.ToStringTree());
            }
        }
    }
}