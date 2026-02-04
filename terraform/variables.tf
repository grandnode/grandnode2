variable "aws_region" {
  type        = string
  description = "AWS region"
}

variable "project_name" {
  type        = string
  description = "Project name prefix"
  default     = "grandnode"
}

variable "environment" {
  type        = string
  description = "Environment name (e.g., dev, prod)"
  default     = "prod"
}

variable "vpc_cidr" {
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  type        = list(string)
  default     = ["10.0.0.0/24", "10.0.1.0/24"]
}

variable "private_subnet_cidrs" {
  type        = list(string)
  default     = ["10.0.10.0/24", "10.0.11.0/24"]
}

variable "container_port" {
  type    = number
  default = 8080
}

variable "container_cpu" {
  type    = number
  default = 512
}

variable "container_memory" {
  type    = number
  default = 1024
}

variable "desired_count" {
  type    = number
  default = 2
}

variable "ecr_repo_name" {
  type    = string
  default = "grandnode"
}

variable "image_tag" {
  type    = string
  default = "latest"
}

variable "mongodb_connection_string" {
  type        = string
  description = "MongoDB/DocumentDB connection string used by the app"
  sensitive   = true
}

variable "db_provider" {
  type        = number
  description = "DbProvider enum (MongoDB=0, CosmosDB=1, DocumentDB=2, LiteDB=3)"
  default     = 0
}

variable "redis_pubsub_enabled" {
  type    = bool
  default = true
}

variable "redis_persist_keys" {
  type    = bool
  default = true
}

variable "aspnetcore_environment" {
  type    = string
  default = "Production"
}

variable "installer_enabled" {
  type    = bool
  default = false
}

variable "health_check_path" {
  type    = string
  default = "/health/live"
}

variable "log_retention_in_days" {
  type    = number
  default = 30
}

variable "alb_enable_https" {
  type    = bool
  default = false
}

variable "alb_certificate_arn" {
  type        = string
  description = "ACM cert ARN for ALB HTTPS"
  default     = ""
}

variable "route53_zone_id" {
  type        = string
  description = "Route53 hosted zone ID (optional)"
  default     = ""
}

variable "domain_name" {
  type        = string
  description = "DNS name for ALB (optional)"
  default     = ""
}

variable "media_bucket_name" {
  type        = string
  description = "S3 bucket for uploaded media"
}

variable "cloudfront_acm_cert_arn" {
  type        = string
  description = "ACM cert ARN in us-east-1 for CloudFront (optional)"
  default     = ""
}

variable "cloudfront_domain_aliases" {
  type        = list(string)
  description = "Domain aliases for CloudFront (optional)"
  default     = []
}

variable "enable_documentdb" {
  type    = bool
  default = false
}

variable "docdb_username" {
  type      = string
  sensitive = true
  default   = ""
}

variable "docdb_password" {
  type      = string
  sensitive = true
  default   = ""
}

variable "docdb_db_name" {
  type    = string
  default = "grandnode"
}

variable "docdb_instance_class" {
  type    = string
  default = "db.t3.medium"
}

variable "docdb_engine_version" {
  type    = string
  default = "5.0.0"
}

variable "redis_node_type" {
  type    = string
  default = "cache.t3.small"
}

variable "redis_engine_version" {
  type    = string
  default = "7.1"
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "alarm_sns_topic_arn" {
  type        = string
  description = "Optional SNS topic ARN for alarm notifications"
  default     = ""
}
