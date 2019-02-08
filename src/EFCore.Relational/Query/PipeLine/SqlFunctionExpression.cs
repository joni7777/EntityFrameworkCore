// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class SqlFunctionExpression : Expression
    {
        public SqlFunctionExpression(Expression instance, string functionName, string schema, IEnumerable<Expression> arguments, Type type)
        {
            Instance = instance;
            FunctionName = functionName;
            Schema = schema;
            Arguments = arguments.ToList();
            Type = type;
        }

        public override Type Type { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public string FunctionName { get; }
        public string Schema { get; }
        public IReadOnlyList<Expression> Arguments { get; }
        public Expression Instance { get; }
    }
}
