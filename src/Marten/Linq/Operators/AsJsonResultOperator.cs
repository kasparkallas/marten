﻿using System;
using System.Linq.Expressions;
using Marten.Internal;
using Marten.Linq.SqlGeneration;
using Marten.Transforms;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Clauses.StreamedData;

namespace Marten.Linq.Operators
{
    public class AsJsonResultOperator
        : SequenceTypePreservingResultOperatorBase, ISelectableOperator
    {
        public static readonly AsJsonResultOperator Flyweight = new AsJsonResultOperator(null);

        public AsJsonResultOperator(Expression parameter)
        {
            Parameter = parameter;
        }

        public Expression Parameter { get; private set; }

        public override ResultOperatorBase Clone(CloneContext cloneContext)
        {
            return new AsJsonResultOperator(Parameter);
        }

        public override void TransformExpressions(
            Func<Expression, Expression> transformation)
        {
            Parameter = transformation(Parameter);
        }

        public override StreamedSequence ExecuteInMemory<T>(StreamedSequence input)
        {
            return input;
        }

        public SelectorStatement ModifyStatement(SelectorStatement statement, IMartenSession session)
        {
            statement.ToJsonSelector();

            return statement;
        }
    }
}
