using System.Data;

namespace BlendInteractive.Blit.Optimizely.Data;

public record VariableRecord(int Id, string Name, string Value)
{
    public static VariableRecord FromDataReader(IDataReader reader)
        => new VariableRecord(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));

    public static Variable AsVariable(VariableRecord record) => new Variable(record.Name, record.Value);
}
