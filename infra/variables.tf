variable "project_name" {
  description = "Project name used for tagging and naming resources."
  type        = string
  default     = "black-friday-survival"
}

variable "env" {
  description = "Deployment environment (dev, preprod, prod)."
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "preprod", "prod"], var.env)
    error_message = "env must be one of: dev, preprod, prod."
  }
}

variable "aws_region" {
  description = "AWS region where infrastructure is deployed."
  type        = string
  default     = "eu-west-1"
}

variable "vpc_cidr" {
  description = "VPC CIDR block."
  type        = string
  default     = "10.0.0.0/16"
}

variable "azs" {
  description = "Availability Zones used to spread subnets."
  type        = list(string)
  default     = ["eu-west-1a", "eu-west-1b", "eu-west-1c"]
}

variable "kubernetes_version" {
  description = "EKS Kubernetes version."
  type        = string
  default     = "1.29"
}

variable "node_instance_types" {
  description = "EC2 instance types for EKS managed node group."
  type        = list(string)
  default     = ["t3.large"]
}

variable "node_desired_size" {
  description = "Desired number of EKS worker nodes."
  type        = number
  default     = 3
}

variable "node_min_size" {
  description = "Minimum number of EKS worker nodes."
  type        = number
  default     = 3
}

variable "node_max_size" {
  description = "Maximum number of EKS worker nodes."
  type        = number
  default     = 15
}

variable "cluster_log_retention_in_days" {
  description = "CloudWatch retention period for EKS control plane logs."
  type        = number
  default     = 14
}

variable "tags" {
  description = "Additional tags to apply to resources."
  type        = map(string)
  default     = {}
}
