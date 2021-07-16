namespace Catalog.Settings
{
    public class MongoDbSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }

        // Stored in .NET Secret Manager
        // Password is 'mongo#pass'
        public string Password { get; set; }
    
        public string ConnectionString 
        { 
            get
            {
                return $"mongodb://{User}:{Password}@{Host}:{Port}";
            } 
        }
    }
}