using Npgsql;

namespace RinhaAPI.Services;

public class NpgsqlService
{
    public NpgsqlDataSource dataSource;
    public NpgsqlService(IConfiguration config)
    {

        dataSource = NpgsqlDataSource.Create(@$"User ID=admin;Password=admin;Host={config["HOST"]};Port=5432;Database=mydb;Pooling=true;MinPoolSize=1;MaxPoolSize={config["MaxPoolSize"]};");
    }
}
