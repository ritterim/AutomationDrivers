using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace AutomationDrivers.Core
{
    public static class ExceptionExtensions
    {
        public static string ToExpressionString(this LambdaExpression expression)
        {
            StringBuilder sbExpression = new StringBuilder();
            foreach (var exprParam in expression.Parameters)
            {
                sbExpression.Append(exprParam);
                if (expression.Parameters.Last() != exprParam) sbExpression.Append(",");
            }
            sbExpression.Append(" => ");

            var exprBody = expression.Body.ToString();
            exprBody = exprBody.Replace("OrElse", "||").Replace("AndAlso", "&&");

            sbExpression.Append(exprBody);

            return sbExpression.ToString();
        }
    }
}