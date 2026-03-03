# Black Friday Survival - Infra Deployment Guide

This guide lists everything to prepare **before** running:

- `./scripts/deploy.sh`
- `./scripts/destroy.sh`

## 1) Required Tools

Install and verify these tools are available in your terminal:

- `terraform` (1.x)
- `aws` (AWS CLI v2)
- `kubectl`
- `helm`

Quick checks:

```bash
terraform version
aws --version
kubectl version --client
helm version
```

## 2) AWS Credentials and Permissions

Configure AWS credentials in your shell/profile:

```bash
aws configure
aws sts get-caller-identity
```

Your IAM user/role must be able to manage at least:

- VPC, Subnets, Route Tables, NAT, Internet Gateway
- EKS Cluster + Managed Node Groups
- IAM Roles/Policies/Attachments
- CloudWatch Log Groups
- S3 + DynamoDB (for Terraform remote state backend)
- ECR pull access for worker nodes (already attached by Terraform)

## 3) Configure Terraform Remote State (Mandatory)

Edit `infra/backend.tf` and replace placeholders:

- `bucket = "REPLACE_ME_TF_STATE_BUCKET"`
- `dynamodb_table = "REPLACE_ME_TF_LOCKS"`
- `key` (optional naming/path)
- `region` (default is `eu-west-1` for infra deployment; backend can stay in another region)

The S3 bucket and DynamoDB table must already exist before `terraform init`.

You can create them automatically with:

```bash
chmod +x scripts/bootstrap-backend.sh
TF_STATE_BUCKET=black-friday-survival-tfstate-123456 ./scripts/bootstrap-backend.sh
```

Optional overrides:

- `AWS_REGION` (default `eu-west-1`)
- `TF_LOCK_TABLE` (default `black-friday-survival-tflocks`)
- `TF_STATE_KEY` (default `black-friday-survival/terraform.tfstate`)

## 4) Review Environment Variables for App Deployment

`deploy.sh` expects these optional env vars (defaults exist, but you should set real values):

- `AWS_REGION` (default: `eu-west-1`)
- `IMAGE_REPOSITORY` (your ECR or DockerHub image)
- `IMAGE_TAG`
- `BUILD_IMAGE` (`true` to build+push before deploy, default `false`)
- `DOCKERFILE_PATH` (default `<repo>/Dockerfile`)
- `BUILD_CONTEXT` (default repo root)
- `ASPNETCORE_ENVIRONMENT`
- `DB_CONNECTION_STRING`

Example:

```bash
export AWS_REGION=eu-west-1
export IMAGE_REPOSITORY=123456789012.dkr.ecr.eu-west-1.amazonaws.com/grandnode2
export IMAGE_TAG=latest
export BUILD_IMAGE=true
export ASPNETCORE_ENVIRONMENT=Production
export DB_CONNECTION_STRING='Server=...;Database=...;User Id=...;Password=...;'
```

## 5) Check Helm Chart Placeholders

Review:

- `k8s/grandnode2/values.yaml`

Important placeholders:

- `image.repository`
- `ingress.host` (`grandnode2.example.com` by default)
- `env.DB_CONNECTION_STRING` (if you do not override with `--set`/env)

## 6) AWS Load Balancer Controller Prerequisite

`deploy.sh` installs the chart, but you still need proper IAM setup for production:

- Create IRSA role/policy for `aws-load-balancer-controller`
- Ensure the service account can assume that role

If IRSA is not configured, ALB reconciliation may fail.

## 7) Run Deployment

From repo root:

```bash
chmod +x scripts/deploy.sh scripts/destroy.sh
./scripts/deploy.sh dev
```

Supported environments:

- `dev` (default)
- `preprod`
- `prod`

## 8) Post-Deploy Checks

```bash
kubectl get nodes
kubectl get pods -n grandnode2
kubectl get ingress -n grandnode2
```

Get ALB DNS from ingress output and test the app URL.

## 9) Destroy

```bash
./scripts/destroy.sh dev
```

For `prod`, the script asks for confirmation.



export AWS_REGION=eu-west-1
export IMAGE_REPOSITORY=622333992348.dkr.ecr.eu-west-1.amazonaws.com/grandnode2
export IMAGE_TAG=test-1
export BUILD_IMAGE=true
./scripts/deploy.sh dev
