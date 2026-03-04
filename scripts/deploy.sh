#!/usr/bin/env bash
set -euo pipefail

ENV="${1:-dev}"
AWS_REGION="${AWS_REGION:-eu-west-1}"

export ENV
export AWS_REGION

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INFRA_DIR="${ROOT_DIR}/infra"
BUILD_IMAGE="${BUILD_IMAGE:-false}"
IMAGE_REPOSITORY="${IMAGE_REPOSITORY:-my-ecr-or-dockerhub/grandnode2}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
DOCKERFILE_PATH="${DOCKERFILE_PATH:-${ROOT_DIR}/Dockerfile}"
BUILD_CONTEXT="${BUILD_CONTEXT:-${ROOT_DIR}}"

echo "Starting deployment for ENV=${ENV} in AWS_REGION=${AWS_REGION}"

cd "${INFRA_DIR}"
terraform init
terraform apply -auto-approve \
  -var="env=${ENV}" \
  -var="aws_region=${AWS_REGION}"

CLUSTER_NAME="$(terraform output -raw eks_cluster_name)"
echo "Using EKS cluster: ${CLUSTER_NAME}"

aws eks update-kubeconfig --region "${AWS_REGION}" --name "${CLUSTER_NAME}"

if [[ "${BUILD_IMAGE}" == "true" ]]; then
  echo "BUILD_IMAGE=true: building and pushing ${IMAGE_REPOSITORY}:${IMAGE_TAG}"

  if [[ "${IMAGE_REPOSITORY}" == *.dkr.ecr.*.amazonaws.com/* ]]; then
    ECR_REGISTRY="$(echo "${IMAGE_REPOSITORY}" | cut -d'/' -f1)"
    aws ecr get-login-password --region "${AWS_REGION}" | docker login --username AWS --password-stdin "${ECR_REGISTRY}"
  fi

  docker build -f "${DOCKERFILE_PATH}" -t "${IMAGE_REPOSITORY}:${IMAGE_TAG}" "${BUILD_CONTEXT}"
  docker push "${IMAGE_REPOSITORY}:${IMAGE_TAG}"
fi

if ! helm status aws-load-balancer-controller -n kube-system >/dev/null 2>&1; then
  helm repo add eks https://aws.github.io/eks-charts
  helm repo update

  # TODO: Before running in production, create/attach the IAM role for service account (IRSA)
  # and adjust serviceAccount settings below accordingly.
  helm upgrade --install aws-load-balancer-controller eks/aws-load-balancer-controller \
    -n kube-system \
    --set clusterName="${CLUSTER_NAME}" \
    --set serviceAccount.create=true \
    --set serviceAccount.name=aws-load-balancer-controller
fi

kubectl get namespace grandnode2 >/dev/null 2>&1 || kubectl create namespace grandnode2

ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
MONGODB_ENABLED="${MONGODB_ENABLED:-true}"
MONGODB_USERNAME="${MONGODB_USERNAME:-grandnodeadmin}"
MONGODB_PASSWORD="${MONGODB_PASSWORD:-ChangeMeMongoPass123}"
MONGODB_DATABASE="${MONGODB_DATABASE:-grandnode2}"

if [[ "${MONGODB_ENABLED}" == "true" ]]; then
  helm repo add bitnami https://charts.bitnami.com/bitnami >/dev/null 2>&1 || true
  helm repo update >/dev/null 2>&1

  helm upgrade --install mongodb bitnami/mongodb \
    -n grandnode2 \
    --set architecture=standalone \
    --set auth.enabled=true \
    --set auth.rootPassword="${MONGODB_PASSWORD}" \
    --set auth.usernames[0]="${MONGODB_USERNAME}" \
    --set auth.passwords[0]="${MONGODB_PASSWORD}" \
    --set auth.databases[0]="${MONGODB_DATABASE}" \
    --set persistence.enabled=false
fi

if [[ -z "${DB_CONNECTION_STRING:-}" ]]; then
  DB_CONNECTION_STRING="mongodb://${MONGODB_USERNAME}:${MONGODB_PASSWORD}@mongodb.grandnode2.svc.cluster.local:27017/${MONGODB_DATABASE}?authSource=${MONGODB_DATABASE}"
fi

helm upgrade --install grandnode2 "${ROOT_DIR}/k8s/grandnode2" \
  -n grandnode2 \
  --set image.repository="${IMAGE_REPOSITORY}" \
  --set image.tag="${IMAGE_TAG}" \
  --set env.ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT}" \
  --set env.DB_CONNECTION_STRING="${DB_CONNECTION_STRING}"

echo "Deployment complete."
echo "EKS cluster name: ${CLUSTER_NAME}"
echo "Reminder: check the ALB DNS name with: kubectl get ingress -n grandnode2"
