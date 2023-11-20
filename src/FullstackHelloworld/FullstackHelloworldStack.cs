using Amazon.CDK;
using Amazon.CDK.AWS.CodeDeploy;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.CloudAssembly.Schema;
using Amazon.CDK.Pipelines;
using Constructs;
using System.Collections.Generic;

namespace FullstackHelloworld
{
    public class FullstackHelloworldStack : Stack
    {
        internal FullstackHelloworldStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here
            var vpc = new Vpc(this, "VPC", new VpcProps
            {
                IpAddresses = IpAddresses.Cidr("10.0.0.0/16"),
                ReservedAzs = 2,
                SubnetConfiguration = new[]
                {
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        Name = "public",
                        SubnetType = SubnetType.PUBLIC
                    },
                    new SubnetConfiguration {
                        CidrMask = 24,
                        Name = "private",
                        SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    },
                    new SubnetConfiguration {
                        CidrMask = 20,
                        Name = "Isolated",
                        SubnetType = SubnetType.PRIVATE_ISOLATED,
                    },
                } 
            });

            var ecr = new Repository(this, "ecr", new RepositoryProps
            {
                // These two are only useful for sandbox/local type environments
                AutoDeleteImages = true,
                RemovalPolicy = RemovalPolicy.DESTROY, 

                // My settings
                LifecycleRules = new[]
                {
                    new LifecycleRule
                    {
                        MaxImageCount = 5,
                    }
                },

                // Security best practices
                ImageScanOnPush = true,
                Encryption = RepositoryEncryption.KMS,
                ImageTagMutability = TagMutability.IMMUTABLE,
            });

            var ecsCluster = new Cluster(this, "ecs", new ClusterProps
            {
                EnableFargateCapacityProviders = true,
                Vpc = vpc,
                // Best practices
                ContainerInsights = true,

                //DefaultCloudMapNamespace = namespace - Namespace not in mvp but maybe a future research
            });

            var taskDefinition = new FargateTaskDefinition(this, "taskDefinition", new FargateTaskDefinitionProps
            {
                // My settings
                Family = "FullStackHelloworld",

                // TODO: Give container an IAM role to make calls with

                // Define a platform to ensure expected behavior
                RuntimePlatform = new RuntimePlatform
                {
                    OperatingSystemFamily = OperatingSystemFamily.LINUX,
                    CpuArchitecture = CpuArchitecture.X86_64
                },

                // Resource allocation
                Cpu = 512,
                MemoryLimitMiB = 1024,
            });

            // IMPORTANT: taskDefinition requires a container be associated to it to be valid
            //var image = ContainerImage.FromRegistry("reginaldferland/dotnet-helloworld");
            var docker = new Amazon.CDK.AWS.Ecr.Assets.DockerImageAsset(this, "dockerImage", new DockerImageAssetProps
            {
                Directory = "./src/",
                File = "FullStackHelloworld-api/Dockerfile",
            });
            var image = ContainerImage.FromDockerImageAsset(docker);

            // Actively trying to figure out how CDK fits into ci/cd pipeline, could build docker image here or build deployment trigger on ecr
            // DockerImageAsset could be used to build from local dockerfile
            
            taskDefinition.AddContainer("image", new ContainerDefinitionOptions
            {
                Image = image,

                // Docker configuration
                Environment = new Dictionary<string, string>
                {
                    {"COMPlus_EnableDiagnostics", "0"}, // This is required for a dotnet app to run in a read only container
                    //{"ASPNETCORE_HTTPS_PORTS", "8443" },
                    {"ASPNETCORE_HTTP_PORTS", "8080" },
                },
                PortMappings = new[]
                {
                    new PortMapping
                    {
                        ContainerPort = 8080,
                        Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP,
                        AppProtocol = AppProtocol.Http,
                        Name = "http"
                    },
                    new PortMapping
                    {
                        ContainerPort = 8443,
                        Protocol = Amazon.CDK.AWS.ECS.Protocol.TCP,
                        AppProtocol = AppProtocol.Http,
                        Name = "https"
                    }
                },
                
                // ECS Configs
                Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    // Outside of a sandbox/local env this probably should be longer
                    LogRetention = Amazon.CDK.AWS.Logs.RetentionDays.ONE_DAY,
                    Mode = AwsLogDriverMode.NON_BLOCKING,
                    StreamPrefix = "taskPrefix",
                }),
                HealthCheck = new Amazon.CDK.AWS.ECS.HealthCheck
                {   
                    Command = new[] { "CMD-SHELL", "curl -f http://localhost:8080/health/ready || exit 1" },
                    Interval = Duration.Seconds(30),
                    Timeout = Duration.Seconds(5),
                    Retries = 3,
                    StartPeriod = Duration.Seconds(15),
                },

                // Security Best Practices
                ReadonlyRootFilesystem = true,
                //User = "root", // this shouldn't be root
            });

            // TODO: Use ACM to add SSL to Task 

            var ecsService = new NetworkLoadBalancedFargateService(this, "service", new NetworkLoadBalancedFargateServiceProps
            {
                Cluster = ecsCluster,
                DesiredCount = 1,
                TaskDefinition = taskDefinition,
                TaskSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },

                // ECS Configs
                //CircuitBreaker = new DeploymentCircuitBreaker
                //{
                //    Rollback = true
                //},
                DeploymentController = new DeploymentController
                {
                    Type = DeploymentControllerType.ECS // TODO: EXTERNAL to manage via github?
                },
                //DeploymentAlarms = new DeploymentAlarmConfig
                //{
                //    Behavior = AlarmBehavior.ROLLBACK_ON_ALARM,
                //    AlarmNames = new[] {""}   // TODO: Determine which alarms I want monitored
                //},

                // Testing props
                AssignPublicIp = true, // False when using a gateway or LB
                PublicLoadBalancer = true, // False when using an API gateway
                // CloudMapOptions = Service Connect not in MVP, may research later
                //ServiceConnectConfiguration = new ServiceConnectProps
                //{

                //},

                // Security Best Practices
                PlatformVersion = FargatePlatformVersion.LATEST,
                
            });

            // Probably could limit to load balancer once I figure out if targetGroup, LB, or listener 
            ecsService.Service.Connections.AllowFromAnyIpv4(Port.AllTraffic());

            (ecsService.TargetGroup.Node.DefaultChild as CfnTargetGroup).Port = 8080;

            ecsService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
            {
                Enabled = true,
                Path = "/health/ready",
                Interval = Duration.Seconds(30),
                Timeout = Duration.Seconds(15),
                HealthyThresholdCount = 3,
                UnhealthyThresholdCount = 2,
                HealthyHttpCodes = "200-499", // TODO: Determine what range is valid, currently thinking 200-299
                Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP
            });

            var greenGroup = new NetworkTargetGroup(this, "greenGroup", new NetworkTargetGroupProps
            {
                Port = 8080, 
                Vpc = vpc,
                TargetType = TargetType.IP,
            });

            //var codeDeploy = new EcsDeploymentGroup(this, "deploymentGroup", new EcsDeploymentGroupProps
            //{
            //    Service = ecsService.Service,
            //    BlueGreenDeploymentConfig = new EcsBlueGreenDeploymentConfig
            //    {
            //        BlueTargetGroup = ecsService.TargetGroup,
            //        GreenTargetGroup = greenGroup,
            //        Listener = ecsService.Listener,
            //    },
            //    DeploymentConfig = EcsDeploymentConfig.ALL_AT_ONCE,
            //    AutoRollback = new AutoRollbackConfig
            //    {
            //        DeploymentInAlarm = false, // Can't be true without defining alarms
            //        FailedDeployment = true,
            //        StoppedDeployment = true,
            //    },
            //});

            // TODO: configure CodeDeploy to update ECS service to use latest ECR image
        }
    }
}
