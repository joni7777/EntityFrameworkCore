// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Query.PipeLine
{
    public abstract class ShapedQueryExpression : Expression
    {
        public Expression QueryExpression { get; set; }
        public ResultType ResultType { get; set; }

        public LambdaExpression ShaperExpression { get; set; }

        public override Type Type => typeof(IQueryable<>).MakeGenericType(ShaperExpression.ReturnType);

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override bool CanReduce => false;
    }

    public enum ResultType
    {
#pragma warning disable SA1602 // Enumeration items should be documented
        Enumerable,
        Single,
        SingleWithDefault
#pragma warning restore SA1602 // Enumeration items should be documented
    }

}
