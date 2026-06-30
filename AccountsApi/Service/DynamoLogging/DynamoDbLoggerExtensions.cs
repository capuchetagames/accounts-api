using Amazon.DynamoDBv2;

namespace DynamoDb.Logging;

public static class DynamoDbLoggerExtensions
{
    public static ILoggingBuilder AddDynamoDbLogger(this ILoggingBuilder builder, IAmazonDynamoDB client, string tableName, LogLevel minLevel = LogLevel.Warning)
    {
        builder.AddProvider(new DynamoDbLoggerProvider(client, tableName, minLevel));

        return builder;
    }
}