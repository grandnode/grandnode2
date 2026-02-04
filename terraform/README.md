# Terraform Infrastructure Overview

This folder provisions AWS infrastructure to run the GrandNode application on ECS Fargate, with optional DocumentDB, Redis, and an S3 + CloudFront media pipeline.

## What This Creates
- VPC with public and private subnets, Internet Gateway, NAT Gateway, and routing.
- Application Load Balancer with HTTP or HTTPS (redirect + certificate optional).
- ECS cluster, task definition, and Fargate service behind the ALB.
- ECR repository for container images.
- CloudWatch log group and ECS autoscaling policies.
- S3 bucket for media uploads with CloudFront distribution and OAC access.
- Redis (ElastiCache) for caching and pub/sub.
- Optional DocumentDB cluster and subnet group.
- Security groups for ALB, ECS tasks, Redis, and DocumentDB.
- Optional Route53 A-records for ALB and CloudFront.

## Key Inputs
Provide these variables via `terraform.tfvars` or your pipeline:
- `aws_region` (required)
- `media_bucket_name` (required)
- `mongodb_connection_string` (required)

Optional but common:
- `project_name`, `environment`
- `image_tag`, `desired_count`, `container_cpu`, `container_memory`
- `alb_enable_https`, `alb_certificate_arn`
- `route53_zone_id`, `domain_name`
- `cloudfront_acm_cert_arn`, `cloudfront_domain_aliases`
- `enable_documentdb`, `docdb_username`, `docdb_password`
- `redis_node_type`, `redis_engine_version`

See `terraform/variables.tf` for full defaults and descriptions.

## Outputs
Useful outputs include:
- `alb_dns_name`, `alb_url`
- `cloudfront_domain`, `media_bucket_name`
- `ecr_repository_url`
- `ecs_cluster_name`, `ecs_service_name`
- `redis_primary_endpoint`
- `docdb_endpoint` (when enabled)

## Notes
- HTTPS on ALB requires a valid ACM cert in the same region.
- CloudFront custom domains require an ACM cert in `us-east-1`.
- DocumentDB is disabled by default; enable with `enable_documentdb = true`.
