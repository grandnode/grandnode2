resource "aws_elasticache_subnet_group" "redis" {
  name       = "${local.name}-redis"
  subnet_ids = aws_subnet.private[*].id
  tags       = local.tags
}

resource "aws_elasticache_replication_group" "redis" {
  replication_group_id          = "${local.name}-redis"
  description                   = "Redis for ${local.name}"
  node_type                     = var.redis_node_type
  engine                        = "redis"
  engine_version                = var.redis_engine_version
  port                          = 6379
  automatic_failover_enabled    = false
  num_cache_clusters            = 1
  subnet_group_name             = aws_elasticache_subnet_group.redis.name
  security_group_ids            = [aws_security_group.redis.id]
  parameter_group_name          = "default.redis7"
  apply_immediately             = true
  tags                          = local.tags
}
