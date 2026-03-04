variable "name" {
  description = "Name prefix for DocumentDB resources."
  type        = string
}

variable "vpc_id" {
  description = "VPC ID where DocumentDB is deployed."
  type        = string
}

variable "vpc_cidr" {
  description = "VPC CIDR allowed to reach DocumentDB."
  type        = string
}

variable "subnet_ids" {
  description = "Private subnet IDs for DocumentDB subnet group."
  type        = list(string)
}

variable "database_name" {
  description = "Application database name."
  type        = string
}

variable "master_username" {
  description = "Master username for DocumentDB."
  type        = string
}

variable "master_password" {
  description = "Optional master password. If null, a random password is generated."
  type        = string
  default     = null
  sensitive   = true
}

variable "instance_class" {
  description = "Instance class for DocumentDB instances."
  type        = string
}

variable "instance_count" {
  description = "Number of instances in DocumentDB cluster."
  type        = number
}

variable "backup_retention_period" {
  description = "Backup retention period in days."
  type        = number
}

variable "skip_final_snapshot" {
  description = "Skip final snapshot on destroy."
  type        = bool
}

variable "tags" {
  description = "Tags applied to DocumentDB resources."
  type        = map(string)
  default     = {}
}
