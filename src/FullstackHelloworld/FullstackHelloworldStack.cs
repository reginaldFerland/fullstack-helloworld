using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Constructs;

namespace FullstackHelloworld
{
    public class FullstackHelloworldStack : Stack
    {
        internal FullstackHelloworldStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here

            // TODO: Create VPC
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

            // TODO: Create ECR

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

            // TODO: Create ECS Cluster

            // TODO: Create ECS Task 

            // TODO: Create ECS Service
        }
    }
}
