namespace AsyncTaskOrchestrator.RabbitMq.Settings;

public class RabbitMqSettings
{
    public int RetryCount { get; set; }
    public int RetryInterval { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }

    public ExchangeOptions Exchange { get; set; }
    public QueueOptions Queue { get; set; }

    public class ExchangeOptions
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Declare { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
    }

    public class QueueOptions
    {
        public bool Declare { get; set; }
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public string Name { get; set; }
        public bool UseDeadLetterExchange { get; set; } = true;
    }
}
