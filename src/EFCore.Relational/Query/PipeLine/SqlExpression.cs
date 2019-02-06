// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class SqlExpression : Expression
    {
        public SqlExpression(Expression expression,
            RelationalTypeMapping typeMapping)
        {
            Expression = expression;
            TypeMapping = typeMapping;
            IsCondition = false;
        }

        public SqlExpression(Expression expression, bool condition)
        {
            Expression = expression;
            IsCondition = condition;
            TypeMapping = null;
        }

        public RelationalTypeMapping TypeMapping { get; }

        public Expression Expression { get; }
        public bool IsCondition { get; }
        public override Type Type => Expression.Type;
        public override ExpressionType NodeType => ExpressionType.Extension;
    }
}
