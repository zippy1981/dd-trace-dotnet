namespace Datadog.Trace.TestHelpers.FSharp

module TracingIntegrationRules =
    open ValidationTypes
    open Datadog.Trace.TestHelpers
    open SpanModelHelpers

    let isAdoNet : MockSpan -> Result<MockSpan, string> =
        propertyMatches ``type`` "sql"
        &&& tagIsOptional "db.name"
        &&& tagIsPresent "db.type"
        &&& tagMatches "component" "AdoNet"
        &&& tagMatches "span.kind" "client"

    let isAerospike : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "aerospike.command"
        &&& propertyMatches ``type`` "aerospike"
        &&& tagIsOptional "aerospike.key"
        &&& tagIsOptional "aerospike.namespace"
        &&& tagIsOptional "aerospike.setname"
        &&& tagIsOptional "aerospike.userkey"
        &&& tagMatches "component" "aerospike"
        &&& tagMatches "span.kind" "client"

    let isAspNet : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "aspnet.request"
        &&& propertyMatches ``type`` "web"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.request.headers.host"
        &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        // BUG: component tag is not set
        // &&& tagMatches "component" "aspnet"
        &&& tagMatches "span.kind" "server"

    let isAspNetMvc : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "aspnet-mvc.request"
        &&& propertyMatches ``type`` "web"
        &&& tagIsPresent "aspnet.action"
        &&& tagIsOptional "aspnet.area"
        &&& tagIsPresent "aspnet.controller"
        &&& tagIsPresent "aspnet.route"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.request.headers.host"
        &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        // BUG: component tag is not set
        // &&& tagMatches "component" "aspnet"
        &&& tagMatches "span.kind" "server"

    let isAspNetWebApi2 : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "aspnet-webapi.request"
        &&& propertyMatches ``type`` "web"
        &&& tagIsOptional "aspnet.action"
        &&& tagIsOptional "aspnet.controller"
        &&& tagIsPresent "aspnet.route"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.request.headers.host"
        // BUG: When WebApi2 throws an exception, we cannot immediately set the
        // status code because the response hasn't been written yet.
        // For ASP.NET, we register a callback to populate http.status_code
        // when the request has completed, but on OWIN there is no such mechanism.
        // What we should do is instrument OWIN and assert that that has the
        // "http.status_code" tag
        // &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        // BUG: component tag is not set
        // &&& tagMatches "component" "aspnet"
        &&& tagMatches "span.kind" "server"

    let isAspNetCore : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "aspnet_core.request"
        &&& propertyMatches ``type`` "web"
        &&& tagIsOptional "aspnet_core.endpoint"
        &&& tagIsOptional "aspnet_core.route"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.request.headers.host"
        &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        &&& tagMatches "component" "aspnet_core"
        &&& tagMatches "span.kind" "server"

    let isAspNetCoreMvc : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "aspnet_core_mvc.request"
        &&& propertyMatches ``type`` "web"
        &&& tagIsPresent "aspnet_core.action"
        &&& tagIsOptional "aspnet_core.area"
        &&& tagIsPresent "aspnet_core.controller"
        &&& tagIsOptional "aspnet_core.page"
        &&& tagMatches "component" "aspnet_core"
        &&& tagMatches "span.kind" "server"

    let isAwsSqs : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "sqs.request"
        &&& propertyMatches ``type`` "http"
        &&& tagMatches "aws.agent" "dotnet-aws-sdk"
        &&& tagIsPresent "aws.operation"
        &&& tagIsOptional "aws.region"
        &&& tagIsPresent "aws.requestId"
        &&& tagMatches "aws.service" "SQS"
        &&& tagIsOptional "aws.queue.name"
        &&& tagIsOptional "aws.queue.url"
        &&& tagMatches "component" "aws-sdk"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        &&& tagMatches "span.kind" "client"

    let isCosmosDb : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "cosmosdb.query"
        &&& propertyMatches ``type`` "sql"
        &&& tagIsOptional "cosmosdb.container"
        &&& tagIsOptional "db.name"
        &&& tagMatches "db.type" "cosmosdb"
        &&& tagIsPresent "out.host"
        &&& tagMatches "component" "CosmosDb"
        &&& tagMatches "span.kind" "client"

    let isCouchbase : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "couchbase.query"
        &&& propertyMatches ``type`` "db"
        &&& tagIsOptional "couchbase.operation.bucket"
        &&& tagIsPresent "couchbase.operation.code"
        &&& tagIsPresent "couchbase.operation.key"
        &&& tagIsOptional "out.port"
        &&& tagIsOptional "out.host"
        &&& tagMatches "component" "Couchbase"
        &&& tagMatches "span.kind" "client"

    let isElasticsearchNet : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "elasticsearch.query"
        &&& propertyMatches ``type`` "elasticsearch"
        &&& tagIsPresent "elasticsearch.action"
        &&& tagIsPresent "elasticsearch.method"
        &&& tagIsPresent "elasticsearch.url"
        &&& tagMatches "component" "elasticsearch-net"
        &&& tagMatches "span.kind" "client"

    let isGraphQL : MockSpan -> Result<MockSpan, string> =
        propertyMatchesOneOf name [| "graphql.execute"; "graphql.validate" |]
        &&& propertyMatches ``type`` "graphql"
        &&& tagIsOptional "graphql.operation.name"
        &&& tagIsOptional "graphql.operation.type"
        &&& tagIsPresent "graphql.source"
        &&& tagMatches "component" "GraphQL"
        &&& tagMatches "span.kind" "server"

    let isGrpc : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "grpc.request"
        &&& propertyMatches ``type`` "grpc"
        &&& tagIsPresent "grpc.method.kind"
        &&& tagIsPresent "grpc.method.name"
        &&& tagIsPresent "grpc.method.package"
        &&& tagIsPresent "grpc.method.path"
        &&& tagIsPresent "grpc.method.service"
        &&& tagIsPresent "grpc.status.code"
        &&& tagMatches "component" "Grpc"
        &&& tagMatchesOneOf "span.kind" [| "client"; "server" |]

    let isHttpMessageHandler : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "http.request"
        &&& propertyMatches ``type`` "http"
        &&& tagIsPresent "component"
        &&& tagIsPresent "http-client-handler-type"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        &&& tagMatches "span.kind" "client"

    let isKafka : MockSpan -> Result<MockSpan, string> =
        propertyMatchesOneOf name [|"kafka.consume"; "kafka.produce" |]
        &&& propertyMatches ``type`` "queue"
        &&& tagIsOptional "kafka.offset"
        &&& tagIsOptional "kafka.partition"
        &&& tagIsOptional "kafka.tombstone"
        &&& tagIsOptional "message.queue_time_ms"
        &&& tagMatches "component" "kafka"
        &&& tagIsPresent "span.kind"

    let isMongoDB : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "mongodb.query"
        &&& propertyMatches ``type`` "mongodb"
        &&& tagIsOptional "db.name"
        &&& tagIsOptional "mongodb.collection"
        &&& tagIsOptional "mongodb.query"
        &&& tagIsPresent "out.host"
        &&& tagIsPresent "out.port"
        &&& tagMatches "component" "MongoDb"
        &&& tagMatches "span.kind" "client"

    let isMsmq : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "msmq.command"
        &&& propertyMatches ``type`` "queue"
        &&& tagIsPresent "msmq.command"
        &&& tagIsOptional "msmq.message.transactional"
        &&& tagIsPresent "msmq.queue.path"
        &&& tagIsOptional "msmq.queue.transactional"
        &&& tagMatches "component" "msmq"
        &&& tagMatchesOneOf "span.kind" [| "client"; "consumer"; "producer" |]

    let isMySql : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "mysql.query"
        &&& propertyMatches ``type`` "sql"
        &&& tagIsPresent "db.name"
        &&& tagMatches "db.type" "mysql"
        &&& tagMatches "component" "MySql"
        &&& tagMatches "span.kind" "client"

    let isNpgsql : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "postgres.query"
        &&& propertyMatches ``type`` "sql"
        &&& tagIsPresent "db.name"
        &&& tagMatches "db.type" "postgres"
        &&& tagMatches "component" "Npgsql"
        &&& tagMatches "span.kind" "client"

    let isOracle : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "oracle.query"
        &&& propertyMatches ``type`` "sql"
        &&& tagIsPresent "db.name"
        &&& tagMatches "db.type" "oracle"
        &&& tagMatches "component" "Oracle"
        &&& tagMatches "span.kind" "client"

    let isRabbitMQ : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "amqp.command"
        &&& propertyMatches ``type`` "queue"
        &&& tagIsPresent "amqp.command"
        &&& tagIsOptional "amqp.delivery_mode"
        &&& tagIsOptional "amqp.exchange"
        &&& tagIsOptional "amqp.routing_key"
        &&& tagIsOptional "amqp.queue"
        &&& tagIsOptional "message.size"
        &&& tagMatches "component" "RabbitMQ"
        &&& tagIsPresent "span.kind"

    let ``isService Fabric`` : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "amqp.command"
        &&& propertyMatches ``type`` "redis"
        &&& tagIsPresent "service-fabric.application-id"
        &&& tagIsPresent "service-fabric.application-name"
        &&& tagIsPresent "service-fabric.partition-id"
        &&& tagIsPresent "service-fabric.node-id"
        &&& tagIsPresent "service-fabric.node-name"
        &&& tagIsPresent "service-fabric.service-name"
        &&& tagIsPresent "service-fabric.service-remoting.uri"
        &&& tagIsPresent "service-fabric.service-remoting.method-name"
        &&& tagIsOptional "service-fabric.service-remoting.method-id"
        &&& tagIsOptional "service-fabric.service-remoting.interface-id"
        &&& tagIsOptional "service-fabric.service-remoting.invocation-id"

    let ``isService Remoting (client)`` : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "service_remoting.client"
        &&& tagIsPresent "service-fabric.service-remoting.uri"
        &&& tagIsPresent "service-fabric.service-remoting.method-name"
        &&& tagIsOptional "service-fabric.service-remoting.method-id"
        &&& tagIsOptional "service-fabric.service-remoting.interface-id"
        &&& tagIsOptional "service-fabric.service-remoting.invocation-id"
        &&& tagMatches "span.kind" "client"

    let ``isService Remoting (server)`` : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "service_remoting.server"
        &&& tagIsPresent "service-fabric.service-remoting.uri"
        &&& tagIsPresent "service-fabric.service-remoting.method-name"
        &&& tagIsOptional "service-fabric.service-remoting.method-id"
        &&& tagIsOptional "service-fabric.service-remoting.interface-id"
        &&& tagIsOptional "service-fabric.service-remoting.invocation-id"
        &&& tagMatches "span.kind" "server"

    let isServiceStackRedis : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "redis.command"
        &&& propertyMatches ``type`` "redis"
        &&& tagIsPresent "redis.raw_command"
        &&& tagIsPresent "out.host"
        &&& tagIsPresent "out.port"
        &&& tagMatches "component" "ServiceStackRedis"
        &&& tagMatches "span.kind" "client"

    let isStackExchangeRedis : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "redis.command"
        &&& propertyMatches ``type`` "redis"
        &&& tagIsPresent "redis.raw_command"
        &&& tagIsPresent "out.host"
        &&& tagIsPresent "out.port"
        &&& tagMatches "component" "StackExchangeRedis"
        &&& tagMatches "span.kind" "client"

    let isSqlite : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "sqlite.query"
        &&& propertyMatches ``type`` "sql"
        &&& tagIsOptional "db.name"
        &&& tagMatches "db.type" "sqlite"
        &&& tagMatches "component" "Sqlite"
        &&& tagMatches "span.kind" "client"

    let isSqlClient : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "sql-server.query"
        &&& propertyMatches ``type`` "sql"
        &&& tagIsOptional "db.name"
        &&& tagMatches "db.type" "sql-server"
        &&& tagMatches "component" "SqlClient"
        &&& tagMatches "span.kind" "client"

    let isWcf : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "wcf.request"
        &&& propertyMatches ``type`` "web"
        &&& tagIsPresent "http.url"
        &&& tagMatches "component" "Wcf"
        &&& tagMatches "span.kind" "server"

    let isWebRequest : MockSpan -> Result<MockSpan, string> =
        propertyMatches name "http.request"
        &&& propertyMatches ``type`` "http"
        &&& tagIsPresent "http.method"
        &&& tagIsPresent "http.status_code"
        &&& tagIsPresent "http.url"
        &&& tagMatchesOneOf "component" [| "HttpMessageHandler"; "WebRequest" |]
        &&& tagMatches "span.kind" "client"