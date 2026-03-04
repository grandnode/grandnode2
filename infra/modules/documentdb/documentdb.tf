resource "random_password" "master" {
  count = var.master_password == null ? 1 : 0

  length  = 24
  special = false
}

locals {
  effective_master_password = var.master_password != null ? var.master_password : random_password.master[0].result
}

resource "aws_security_group" "documentdb" {
  name        = "${var.name}-sg"
  description = "Security group for DocumentDB"
  vpc_id      = var.vpc_id

  ingress {
    description = "Allow Mongo traffic from VPC"
    from_port   = 27017
    to_port     = 27017
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }

  egress {
    description = "Allow all egress"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.tags, {
    Name = "${var.name}-sg"
  })
}

resource "aws_docdb_subnet_group" "this" {
  name       = "${var.name}-subnet-group"
  subnet_ids = var.subnet_ids

  tags = merge(var.tags, {
    Name = "${var.name}-subnet-group"
  })
}

resource "aws_docdb_cluster" "this" {
  cluster_identifier      = var.name
  engine                  = "docdb"
  master_username         = var.master_username
  master_password         = local.effective_master_password
  db_subnet_group_name    = aws_docdb_subnet_group.this.name
  vpc_security_group_ids  = [aws_security_group.documentdb.id]
  backup_retention_period = var.backup_retention_period
  skip_final_snapshot     = var.skip_final_snapshot
  storage_encrypted       = true
  port                    = 27017

  tags = merge(var.tags, {
    Name = var.name
  })
}

resource "aws_docdb_cluster_instance" "this" {
  count = var.instance_count

  identifier         = "${var.name}-${count.index + 1}"
  cluster_identifier = aws_docdb_cluster.this.id
  instance_class     = var.instance_class
  engine             = aws_docdb_cluster.this.engine

  tags = merge(var.tags, {
    Name = "${var.name}-${count.index + 1}"
  })
}
