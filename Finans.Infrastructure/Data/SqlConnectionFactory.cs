using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Finans.Infrastructure.Data
{
    /// <summary>
    /// Neden var?
    /// - Dapper için bağlantı açma işini tek yerde toplar.
    /// - Connection string tekrarını ve dağınıklığı engeller.
    /// </summary>
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            var cs = _configuration.GetConnectionString("FinansDb");
            return new SqlConnection(cs);
        }
    }
}