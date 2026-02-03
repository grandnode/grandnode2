resource "aws_docdb_subnet_group" "this" {
  count      = var.enable_documentdb ? 1 : 0
  name       = "${local.name}-docdb"
  subnet_ids = aws_subnet.private[*].id
  tags       = local.tags
}

resource "aws_docdb_cluster" "this" {
  count                 = var.enable_documentdb ? 1 : 0
  cluster_identifier    = "${local.name}-docdb"
  engine_version        = var.docdb_engine_version
  master_username       = var.docdb_username
  master_password       = var.docdb_password
  db_subnet_group_name  = aws_docdb_subnet_group.this[0].name
  vpc_security_group_ids = [aws_security_group.docdb.id]
  skip_final_snapshot   = true
  tags                  = local.tags
}

resource "aws_docdb_cluster_instance" "this" {
  count              = var.enable_documentdb ? 1 : 0
  identifier         = "${local.name}-docdb-1"
  cluster_identifier = aws_docdb_cluster.this[0].id
  instance_class     = var.docdb_instance_class
  tags               = local.tags
}
