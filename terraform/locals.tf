locals {
  name = "${var.project_name}-${var.environment}"
  tags = merge({
    Project     = var.project_name
    Environment = var.environment
  }, var.tags)

  redis_pubsub_connection = "${aws_elasticache_replication_group.redis.primary_endpoint_address}:6379,allowAdmin=true"
  redis_persist_url       = "${aws_elasticache_replication_group.redis.primary_endpoint_address}:6379,allowAdmin=true,defaultDatabase=1"
}
