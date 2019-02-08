// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class SqlFragmentExpression : Expression
    {
        public SqlFragmentExpression(string sql, Type type)
        {
            Sql = sql;
            Type = type;
        }

        public override Type Type { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public string Sql { get; }
    }
}
