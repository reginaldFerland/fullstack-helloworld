# A fullstack dotnet helloworld

This project is created to experiment with creating a dotnet core REST API, a Blazer UI, a dotnet core background service, and deploy all of them to AWS using CDK.
The dotnet projects will be deployed as docker containers. 

This hello world will host a UI with a counter and a button, clicking the button increases the counter on page, then stores it to a postgresql db. 
A second counter will be stored in a dynamoDB table, and incremented via a lambda service triggered via an SQS queue, items pushed TDB

Intended services;
- RDS (postgres) for database
- DynamoDB for worker data
- ECR for docker image repo <-- Done
- ECS (fargate) for REST API runtime and Blazer backend
- ELB (probably NLB) to expose containers to internet
- CodeDeploy to push updated images from ECR to ECS
- A VPC to contain the AWS services <-- Done
- Logging via cloudwatch <-- Done
- Lambda for background service
- SNS/SQS queue to send work to service
- IAM role used for internal auth

## Possible goals

- API gateway
- Cognito for authentication

# Infrastructure as code is defined using AWS CDK 

The `cdk.json` file tells the CDK Toolkit how to execute your app.

It uses the [.NET CLI](https://docs.microsoft.com/dotnet/articles/core/) to compile and execute your project.

## Useful commands

* `dotnet build src` compile this app
* `cdk deploy`       deploy this stack to your default AWS account/region
* `cdk diff`         compare deployed stack with current state
* `cdk synth`        emits the synthesized CloudFormation template