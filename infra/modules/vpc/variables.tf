variable "name" {
  description = "Name prefix for VPC resources."
  type        = string
}

variable "vpc_cidr" {
  description = "CIDR block for the VPC."
  type        = string
}

variable "azs" {
  description = "Availability Zones used for public/private subnets."
  type        = list(string)
}

variable "tags" {
  description = "Tags applied to all VPC resources."
  type        = map(string)
  default     = {}
}
