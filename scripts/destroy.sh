#!/usr/bin/env bash
set -euo pipefail

ENV="${1:-dev}"
AWS_REGION="${AWS_REGION:-eu-west-1}"

export ENV
export AWS_REGION

if [[ "${ENV}" == "prod" ]]; then
  read -r -p "You are about to destroy PROD infrastructure. Type 'yes' to continue: " CONFIRM
  if [[ "${CONFIRM}" != "yes" ]]; then
    echo "Abort."
    exit 1
  fi
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INFRA_DIR="${ROOT_DIR}/infra"

cd "${INFRA_DIR}"
terraform init

CLUSTER_NAME="$(terraform output -raw eks_cluster_name 2>/dev/null || true)"
if [[ -n "${CLUSTER_NAME}" ]]; then
  aws eks update-kubeconfig --region "${AWS_REGION}" --name "${CLUSTER_NAME}" || true
fi

helm uninstall grandnode2 -n grandnode2 || true

# TODO: Optionally uninstall AWS Load Balancer Controller if this cluster is being fully decommissioned.
# helm uninstall aws-load-balancer-controller -n kube-system || true

terraform destroy -auto-approve \
  -var="env=${ENV}" \
  -var="aws_region=${AWS_REGION}"

echo "Destroy complete for ENV=${ENV} in AWS_REGION=${AWS_REGION}."
