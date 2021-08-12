// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Analysis;
using Antlr4.Misc;
using Antlr4.Parse;
using Antlr4.Runtime.Utility;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

using Utils = Antlr4.Misc.Utils;

namespace Antlr4.Semantics
{
    public class RuleCollector : GrammarTreeVisitor
    {
        public IDictionary<string, string> altLabelToRuleName = new Dictionary<string, string>();
        public ErrorManager errMgr;

        /**
         * which grammar are we checking
         */
        public Grammar g;

        // stuff to collect. this is the output
        public OrderedHashMap<string, Rule> rules = new();
        public MultiMap<string, GrammarAST> ruleToAltLabels = new();

        public RuleCollector(Grammar g)
        {
            this.g = g;
            errMgr = g.tool.errMgr;
        }

        public override ErrorManager GetErrorManager()
        {
            return errMgr;
        }

        public virtual void Process(GrammarAST ast)
        {
            VisitGrammar(ast);
        }

        public override void DiscoverRule(RuleAST rule, GrammarAST ID,
            IList<GrammarAST> modifiers, ActionAST arg,
            ActionAST returns, GrammarAST thrws,
            GrammarAST options, ActionAST locals,
            IList<GrammarAST> actions,
            GrammarAST block)
        {
            int numAlts = block.ChildCount;
            Rule r;
            if (LeftRecursiveRuleAnalyzer.HasImmediateRecursiveRuleRefs(rule, ID.Text))
            {
                r = new LeftRecursiveRule(g, ID.Text, rule);
            }
            else
            {
                r = new Rule(g, ID.Text, rule, numAlts);
            }

            rules[r.name] = r;

            if (arg != null)
            {
                r.args = ScopeParser.ParseTypedArgList(arg, arg.Text, g);
                r.args.type = AttributeDict.DictType.ARG;
                r.args.ast = arg;
                arg.resolver = r.alt[currentOuterAltNumber];
            }

            if (returns != null)
            {
                r.retvals = ScopeParser.ParseTypedArgList(returns, returns.Text, g);
                r.retvals.type = AttributeDict.DictType.RET;
                r.retvals.ast = returns;
            }

            if (locals != null)
            {
                r.locals = ScopeParser.ParseTypedArgList(locals, locals.Text, g);
                r.locals.type = AttributeDict.DictType.LOCAL;
                r.locals.ast = locals;
            }

            foreach (GrammarAST a in actions)
            {
                // a = ^(AT ID ACTION)
                ActionAST action = (ActionAST) a.GetChild(1);
                r.namedActions[a.GetChild(0).Text] = action;
                action.resolver = r;
            }
        }

        public override void DiscoverOuterAlt(AltAST alt)
        {
            if (alt.altLabel != null)
            {
                ruleToAltLabels.Map(currentRuleName, alt.altLabel);
                string altLabel = alt.altLabel.Text;
                altLabelToRuleName[Utils.Capitalize(altLabel)] = currentRuleName;
                altLabelToRuleName[Utils.Decapitalize(altLabel)] = currentRuleName;
            }
        }

        public override void DiscoverLexerRule(RuleAST rule, GrammarAST ID, IList<GrammarAST> modifiers,
            GrammarAST block)
        {
            int numAlts = block.ChildCount;
            Rule r = new(g, ID.Text, rule, numAlts);
            r.mode = currentModeName;
            if (modifiers.Count > 0)
            {
                r.modifiers = modifiers;
            }

            rules[r.name] = r;
        }
    }
}