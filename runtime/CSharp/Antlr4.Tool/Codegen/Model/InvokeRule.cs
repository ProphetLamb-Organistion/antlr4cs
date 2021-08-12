// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Codegen.Model.Chunk;
using Antlr4.Codegen.Model.Decl;
using Antlr4.Misc;
using Antlr4.Runtime.Atn;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public class InvokeRule : RuleElement, LabeledOp
    {
        [ModelElement] public IList<ActionChunk> argExprsChunks;

        public string ctxName;
        public OrderedHashSet<Decl.Decl> labels = new(); // TODO: should need just 1
        public string name;

        public InvokeRule(ParserFactory factory, GrammarAST ast, GrammarAST labelAST)
            : base(factory, ast)
        {
            if (ast.atnState != null)
            {
                RuleTransition ruleTrans = (RuleTransition) ast.atnState.Transition(0);
                stateNumber = ast.atnState.stateNumber;
            }

            name = ast.Text;
            Rule r = factory.GetGrammar().GetRule(name);
            ctxName = factory.GetTarget().GetRuleFunctionContextStructName(r);

            // TODO: move to factory
            RuleFunction rf = factory.GetCurrentRuleFunction();
            if (labelAST != null)
            {
                // for x=r, define <rule-context-type> x and list_x
                string label = labelAST.Text;
                if (labelAST.Parent.Type == ANTLRParser.PLUS_ASSIGN)
                {
                    factory.DefineImplicitLabel(ast, this);
                    string listLabel = factory.GetTarget().GetListLabel(label);
                    RuleContextListDecl l = new(factory, listLabel, ctxName);
                    rf.AddContextDecl(ast.GetAltLabel(), l);
                }
                else
                {
                    RuleContextDecl d = new(factory, label, ctxName);
                    labels.Add(d);
                    rf.AddContextDecl(ast.GetAltLabel(), d);
                }
            }

            ActionAST arg = (ActionAST) ast.GetFirstChildWithType(ANTLRParser.ARG_ACTION);
            if (arg != null)
            {
                argExprsChunks = ActionTranslator.TranslateAction(factory, rf, arg.Token, arg);
            }

            // If action refs rule as rulename not label, we need to define implicit label
            if (factory.GetCurrentOuterMostAlt().ruleRefsInActions.ContainsKey(ast.Text))
            {
                string label = factory.GetTarget().GetImplicitRuleLabel(ast.Text);
                RuleContextDecl d = new(factory, label, ctxName);
                labels.Add(d);
                rf.AddContextDecl(ast.GetAltLabel(), d);
            }
        }

        public virtual IList<Decl.Decl> GetLabels()
        {
            return labels.Elements;
        }
    }
}