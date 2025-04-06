using BlendInteractive.Datastore;
using Microsoft.Data.SqlClient;

namespace BlendInteractive.Blit.Optimizely.Data;

public class DatastoreFactory : AbstractDatastoreFactory<Datastore>
{
    public DatastoreFactory(string connectionString) : base(connectionString)
    {
    }

    public override string SqlResourcesPrefix => "BlendInteractive.Blit.Optimizely.Data.Migrations";

    protected override string GetVersionProcedureName => "BlitVersion";

    protected override int CurrentVersion => 1;

    protected override Datastore GetDatastore(SqlConnection conn, SqlTransaction? trans)
        => new Datastore(conn, trans);
}
