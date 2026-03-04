output "endpoint" {
  description = "DocumentDB cluster endpoint."
  value       = aws_docdb_cluster.this.endpoint
}

output "port" {
  description = "DocumentDB port."
  value       = aws_docdb_cluster.this.port
}

output "database_name" {
  description = "Database name used by the application."
  value       = var.database_name
}

output "master_username" {
  description = "DocumentDB master username."
  value       = var.master_username
}

output "master_password" {
  description = "DocumentDB master password."
  value       = local.effective_master_password
  sensitive   = true
}

output "connection_string" {
  description = "Mongo-compatible connection string for GrandNode2."
  value       = "mongodb://${var.master_username}:${local.effective_master_password}@${aws_docdb_cluster.this.endpoint}:${aws_docdb_cluster.this.port}/${var.database_name}?tls=true&replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false"
  sensitive   = true
}
