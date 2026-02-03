output "alb_dns_name" {
  value = aws_lb.app.dns_name
}

output "alb_url" {
  value = "http://${aws_lb.app.dns_name}"
}

output "cloudfront_domain" {
  value = aws_cloudfront_distribution.media.domain_name
}

output "media_bucket_name" {
  value = aws_s3_bucket.media.bucket
}

output "ecr_repository_url" {
  value = aws_ecr_repository.app.repository_url
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "ecs_service_name" {
  value = aws_ecs_service.app.name
}

output "redis_primary_endpoint" {
  value = aws_elasticache_replication_group.redis.primary_endpoint_address
}

output "docdb_endpoint" {
  value       = var.enable_documentdb ? aws_docdb_cluster.this[0].endpoint : ""
  description = "DocumentDB endpoint (if enabled)"
}
