namespace Api.Options;

record RabbitOptions(
    string Host, 
    int Port, 
    string VirtualHost, 
    string Username, 
    string Password);

record MongoOptions(
    string Host, 
    int Port, 
    string Username, 
    string Password, 
    string? ConnectionString);

record OpenTelemetryOptions(
    string OtlpEndpoint, 
    double TraceSamplingRatio);
