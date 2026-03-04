#!/usr/bin/env bash
set -euo pipefail

AWS_REGION="${AWS_REGION:-eu-west-3}"
TF_STATE_BUCKET="${TF_STATE_BUCKET:-}"
TF_LOCK_TABLE="${TF_LOCK_TABLE:-black-friday-survival-tflocks}"
TF_STATE_KEY="${TF_STATE_KEY:-black-friday-survival/terraform.tfstate}"

if [[ -z "${TF_STATE_BUCKET}" ]]; then
  echo "ERROR: TF_STATE_BUCKET is required."
  echo "Example:"
  echo "  TF_STATE_BUCKET=black-friday-survival-tfstate-123456 ./scripts/bootstrap-backend.sh"
  exit 1
fi

echo "Bootstrap Terraform backend"
echo "AWS_REGION=${AWS_REGION}"
echo "TF_STATE_BUCKET=${TF_STATE_BUCKET}"
echo "TF_LOCK_TABLE=${TF_LOCK_TABLE}"
echo "TF_STATE_KEY=${TF_STATE_KEY}"

echo "Checking S3 bucket..."
if aws s3api head-bucket --bucket "${TF_STATE_BUCKET}" >/dev/null 2>&1; then
  echo "S3 bucket already exists and is accessible: ${TF_STATE_BUCKET}"
else
  echo "Creating S3 bucket: ${TF_STATE_BUCKET}"
  aws s3api create-bucket \
    --bucket "${TF_STATE_BUCKET}" \
    --region "${AWS_REGION}" \
    --create-bucket-configuration "LocationConstraint=${AWS_REGION}"
fi

echo "Enabling S3 versioning..."
aws s3api put-bucket-versioning \
  --bucket "${TF_STATE_BUCKET}" \
  --versioning-configuration Status=Enabled

echo "Enabling S3 default encryption..."
aws s3api put-bucket-encryption \
  --bucket "${TF_STATE_BUCKET}" \
  --server-side-encryption-configuration '{
    "Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]
  }'

echo "Checking DynamoDB table..."
if aws dynamodb describe-table --table-name "${TF_LOCK_TABLE}" --region "${AWS_REGION}" >/dev/null 2>&1; then
  echo "DynamoDB table already exists: ${TF_LOCK_TABLE}"
else
  echo "Creating DynamoDB table: ${TF_LOCK_TABLE}"
  aws dynamodb create-table \
    --table-name "${TF_LOCK_TABLE}" \
    --attribute-definitions AttributeName=LockID,AttributeType=S \
    --key-schema AttributeName=LockID,KeyType=HASH \
    --billing-mode PAY_PER_REQUEST \
    --region "${AWS_REGION}"
fi

echo ""
echo "Backend resources are ready."
echo "Update infra/backend.tf with:"
echo "  bucket         = \"${TF_STATE_BUCKET}\""
echo "  key            = \"${TF_STATE_KEY}\""
echo "  region         = \"${AWS_REGION}\""
echo "  dynamodb_table = \"${TF_LOCK_TABLE}\""
echo ""
echo "Then run:"
echo "  cd infra && terraform init -reconfigure"
