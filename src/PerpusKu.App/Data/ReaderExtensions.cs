using System.Data;
using Microsoft.Data.SqlClient;

namespace PerpusKu.Data;

public static class ReaderExtensions
{
    public static string Str(this SqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public static int Int(this SqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
    }

    public static int? IntN(this SqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));
    }

    public static decimal Dec(this SqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    }

    public static DateTime Date(this SqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? DateTime.MinValue : Convert.ToDateTime(r.GetValue(i));
    }

    public static DateTime? DateN(this SqlDataReader r, string col)
    {
        int i = r.GetOrdinal(col);
        return r.IsDBNull(i) ? null : Convert.ToDateTime(r.GetValue(i));
    }

    public static SqlParameter Add(this SqlCommand cmd, string name, SqlDbType type, object value)
    {
        var p = cmd.Parameters.Add(name, type);
        p.Value = value ?? DBNull.Value;
        return p;
    }

    public static string Like(string search)
    {
        search = (search ?? "").Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        return "%" + search + "%";
    }
}
