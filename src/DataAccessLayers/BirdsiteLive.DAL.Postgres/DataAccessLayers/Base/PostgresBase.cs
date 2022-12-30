using BirdsiteLive.DAL.Postgres.Settings;
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
            _dataSource = NpgsqlDataSource.Create(settings.ConnString);
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