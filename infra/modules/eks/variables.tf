variable "cluster_name" {
  description = "Name of the EKS cluster."
  type        = string
}

variable "kubernetes_version" {
  description = "Kubernetes version for EKS cluster."
  type        = string
}

variable "subnet_ids" {
  description = "Private subnet IDs used by EKS cluster and node group."
  type        = list(string)
}

variable "node_group_name" {
  description = "Name of the managed node group."
  type        = string
}

variable "node_instance_types" {
  description = "Instance types for worker nodes."
  type        = list(string)
}

variable "node_desired_size" {
  description = "Desired node count."
  type        = number
}

variable "node_min_size" {
  description = "Minimum node count."
  type        = number
}

variable "node_max_size" {
  description = "Maximum node count."
  type        = number
}

variable "cluster_log_retention_in_days" {
  description = "Retention for EKS control plane logs in CloudWatch."
  type        = number
  default     = 14
}

variable "tags" {
  description = "Tags applied to all EKS resources."
  type        = map(string)
  default     = {}
}
