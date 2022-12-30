using BirdsiteLive.DAL.Postgres.Settings;
using BirdsiteLive.DAL.Models;
using Npgsql;

namespace BirdsiteLive.DAL.Postgres.DataAccessLayers.Base
{
    public class PostgresBase
    {
        protected readonly PostgresSettings _settings;
        protected NpgsqlDataSource _dataSource;

        #region Ctor
        protected PostgresBase(PostgresSettings settings)
        {
            _settings = settings;
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnString);
            _dataSource = dataSourceBuilder.Build();
        }
        #endregion

        protected NpgsqlDataSource DataSource
        {
            get
            {
                return _dataSource;
            }
        }
        protected NpgsqlConnection Connection
        {
            get
            {
                return _dataSource.CreateConnection();
            }
        }
    }
}