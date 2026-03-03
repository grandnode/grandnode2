terraform {
  backend "s3" {
    bucket         = "black-friday-survival-tfstate-123456"
    key            = "black-friday-survival/terraform.tfstate"
    region         = "eu-west-3"
    dynamodb_table = "black-friday-survival-tflocks"
    encrypt        = true
  }
}
