// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;

namespace Antlr4.Codegen.Model
{
    /**
     * Indicate field of OutputModelObject is an element to be walked by
     * OutputModelWalker.
     */
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ModelElementAttribute : Attribute
    {
    }
}