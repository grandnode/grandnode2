terraform {
  required_version = ">= 1.5.0, < 2.0.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

locals {
  common_tags = merge(
    {
      Project     = var.project_name
      Environment = var.env
      ManagedBy   = "Terraform"
    },
    var.tags
  )
}

module "vpc" {
  source = "./modules/vpc"

  name     = "${var.project_name}-${var.env}-vpc"
  vpc_cidr = var.vpc_cidr
  azs      = var.azs
  tags     = local.common_tags
}

module "eks" {
  source = "./modules/eks"

  cluster_name                  = "${var.project_name}-${var.env}-eks"
  kubernetes_version            = var.kubernetes_version
  subnet_ids                    = module.vpc.private_subnet_ids
  node_group_name               = "${var.project_name}-${var.env}-ng"
  node_instance_types           = var.node_instance_types
  node_desired_size             = var.node_desired_size
  node_min_size                 = var.node_min_size
  node_max_size                 = var.node_max_size
  cluster_log_retention_in_days = var.cluster_log_retention_in_days
  tags                          = local.common_tags
}
