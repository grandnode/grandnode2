/* Copyright 2010-2015 MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Grand.Data.Tests.MongoDb;

public static class CoreTestConfiguration
{
    public static void TearDown()
    {
        if (__cluster.IsValueCreated)
        {
            // TODO: DropDatabase
            //DropDatabase();
            __cluster.Value.Dispose();
            __cluster = new Lazy<ICluster>(CreateCluster, true);
        }
    }

    #region static

    // static fields
    private static Lazy<ICluster> __cluster = new(CreateCluster, true);

    // static properties
    public static ICluster Cluster => __cluster.Value;

    public static ConnectionString ConnectionString { get; } = GetConnectionString();

    public static DatabaseNamespace DatabaseNamespace { get; } = GetDatabaseNamespace();

    public static MessageEncoderSettings MessageEncoderSettings { get; } = new();

    public static TraceSource TraceSource { get; private set; }

    // static methods
    public static ClusterBuilder ConfigureCluster()
    {
        return ConfigureCluster(new ClusterBuilder());
    }

    public static ClusterBuilder ConfigureCluster(ClusterBuilder builder)
    {
        var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
        if (serverSelectionTimeoutString == null) serverSelectionTimeoutString = "30000";

        builder = builder
            .ConfigureWithConnectionString(ConnectionString)
            .ConfigureCluster(c =>
                c.With(serverSelectionTimeout: TimeSpan.FromMilliseconds(int.Parse(serverSelectionTimeoutString))));

        if (ConnectionString.Tls.HasValue && ConnectionString.Tls.Value)
        {
            var certificateFilename = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_FILE");
            if (certificateFilename != null)
                builder.ConfigureSsl(ssl =>
                {
                    var password = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_PASS");
                    X509Certificate cert;
                    if (password == null)
                        cert = new X509Certificate2(certificateFilename);
                    else
                        cert = new X509Certificate2(certificateFilename, password);
                    return ssl.With(
                        clientCertificates: new[] { cert });
                });
        }

        return ConfigureLogging(builder);
    }

    public static ClusterBuilder ConfigureLogging(ClusterBuilder builder)
    {
        var environmentVariable = Environment.GetEnvironmentVariable("MONGO_LOGGING");
        if (environmentVariable == null) return builder;

        SourceLevels defaultLevel;
        if (!Enum.TryParse(environmentVariable, true, out defaultLevel)) return builder;

        TraceSource = new TraceSource("mongodb-tests", defaultLevel);
        TraceSource.Listeners.Clear();
        return builder.TraceWith(TraceSource);
    }

    public static ICluster CreateCluster()
    {
        var hasWritableServer = 0;
        var builder = ConfigureCluster();
        var cluster = builder.BuildCluster();
        cluster.DescriptionChanged += (o, e) =>
        {
            var anyWritableServer = e.NewClusterDescription.Servers.Any(
                description => description.Type.IsWritable());
            if (TraceSource != null)
            {
                TraceSource.TraceEvent(TraceEventType.Information, 0,
                    "CreateCluster: DescriptionChanged event handler called.");
                TraceSource.TraceEvent(TraceEventType.Information, 0,
                    $"CreateCluster: anyWritableServer = {anyWritableServer}.");
                TraceSource.TraceEvent(TraceEventType.Information, 0,
                    $"CreateCluster: new description: {e.NewClusterDescription}.");
            }

            Interlocked.Exchange(ref hasWritableServer, anyWritableServer ? 1 : 0);
        };
        if (TraceSource != null)
            TraceSource.TraceEvent(TraceEventType.Information, 0, "CreateCluster: initializing cluster.");
        cluster.Initialize();

        // wait until the cluster has connected to a writable server
        SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref hasWritableServer, 0, 0) != 0,
            TimeSpan.FromSeconds(30));
        if (Interlocked.CompareExchange(ref hasWritableServer, 0, 0) == 0)
        {
            var message =
                $"Test cluster has no writable server. Client view of the cluster is {cluster.Description}.";
            throw new Exception(message);
        }

        if (TraceSource != null)
            TraceSource.TraceEvent(TraceEventType.Information, 0, "CreateCluster: writable server found.");

        return cluster;
    }

    public static CollectionNamespace GetCollectionNamespaceForTestFixture()
    {
        var testFixtureType = GetTestFixtureTypeFromCallStack();
        var collectionName = TruncateCollectionNameIfTooLong(DatabaseNamespace, testFixtureType.Name);
        return new CollectionNamespace(DatabaseNamespace, collectionName);
    }

    public static CollectionNamespace GetCollectionNamespaceForTestMethod()
    {
        var testMethodInfo = GetTestMethodInfoFromCallStack();
        var collectionName = TruncateCollectionNameIfTooLong(DatabaseNamespace,
            testMethodInfo.DeclaringType.Name + "-" + testMethodInfo.Name);
        return new CollectionNamespace(DatabaseNamespace, collectionName);
    }

    private static ConnectionString GetConnectionString()
    {
        return new ConnectionString(Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost");
    }

    private static DatabaseNamespace GetDatabaseNamespace()
    {
        if (!string.IsNullOrEmpty(ConnectionString.DatabaseName))
            return new DatabaseNamespace(ConnectionString.DatabaseName);

        var timestamp = DateTime.Now.ToString("MMddHHmm");
        return new DatabaseNamespace("Tests" + timestamp);
    }

    public static DatabaseNamespace GetDatabaseNamespaceForTestFixture()
    {
        var testFixtureType = GetTestFixtureTypeFromCallStack();
        var databaseName = TruncateDatabaseNameIfTooLong(DatabaseNamespace.DatabaseName + "-" + testFixtureType.Name);
        if (databaseName.Length >= 64) databaseName = databaseName.Substring(0, 63);
        return new DatabaseNamespace(databaseName);
    }


    private static Type GetTestFixtureTypeFromCallStack()
    {
        var stackTrace = new StackTrace();
        for (var index = 0; index < stackTrace.FrameCount; index++)
        {
            var frame = stackTrace.GetFrame(index);
            var methodInfo = frame.GetMethod();
            var declaringType = methodInfo.DeclaringType;
            var testFixtureAttribute = declaringType.GetCustomAttribute<TestFixtureAttribute>(false);
            if (testFixtureAttribute != null) return declaringType;
        }

        throw new Exception("No [TestFixture] found on the call stack.");
    }

    private static MethodInfo GetTestMethodInfoFromCallStack()
    {
        var stackTrace = new StackTrace();
        for (var index = 0; index < stackTrace.FrameCount; index++)
        {
            var frame = stackTrace.GetFrame(index);
            var methodInfo = frame.GetMethod() as MethodInfo;
            if (methodInfo != null)
            {
                var testAttribute = methodInfo.GetCustomAttribute<TestAttribute>(false);
                if (testAttribute != null) return methodInfo;
                var testCaseAttribute = methodInfo.GetCustomAttribute<TestCaseAttribute>(false);
                if (testCaseAttribute != null) return methodInfo;
            }
        }

        throw new Exception("No [TestFixture] found on the call stack.");
    }

    private static string TruncateCollectionNameIfTooLong(DatabaseNamespace databaseNamespace, string collectionName)
    {
        var fullNameLength = databaseNamespace.DatabaseName.Length + 1 + collectionName.Length;
        if (fullNameLength < 123) return collectionName;

        var maxCollectionNameLength = 123 - (databaseNamespace.DatabaseName.Length + 1);
        return collectionName.Substring(0, maxCollectionNameLength);
    }

    private static string TruncateDatabaseNameIfTooLong(string databaseName)
    {
        if (databaseName.Length < 64) return databaseName;

        return databaseName.Substring(0, 63);
    }

    #endregion
}