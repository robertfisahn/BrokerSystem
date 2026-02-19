using Dapper;
using System.Data;

namespace BrokerSystem.Api.Infrastructure;

/// <summary>
/// Custom Dapper type handler to bridge the gap between .NET <see cref="DateOnly"/> 
/// and database date formats (standardizing on DateTime or strings depending on the provider).
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter p, DateOnly v)
    {
        p.Value = v.ToDateTime(TimeOnly.MinValue);
        p.DbType = DbType.Date;
    }

    public override DateOnly Parse(object v) 
    {
        if (v == null || v == DBNull.Value) return default;
        
        if (v is DateTime dt)
            return DateOnly.FromDateTime(dt);
            
        if (v is string s && DateTime.TryParse(s, out var dtOut))
            return DateOnly.FromDateTime(dtOut);

        return DateOnly.FromDateTime(Convert.ToDateTime(v));
    }
}
