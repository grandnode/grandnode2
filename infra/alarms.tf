# ------------------------------------------------------------------------------
# ALB Alarms
# ------------------------------------------------------------------------------

resource "aws_cloudwatch_metric_alarm" "alb_high_latency" {
  count               = var.sns_topic_arn != "" && var.alb_arn_suffix != "" ? 1 : 0
  alarm_name          = "${var.project_name}-${var.env}-alb-high-latency"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "TargetResponseTime"
  namespace           = "AWS/ApplicationELB"
  period              = "60"
  statistic           = "Average"
  threshold           = "1" # 1 second latency threshold
  alarm_description   = "This metric monitors ALB target response time"
  actions_enabled     = true
  alarm_actions       = [var.sns_topic_arn]
  ok_actions          = [var.sns_topic_arn]

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
    TargetGroup  = var.target_group_arn_suffix
  }
}

resource "aws_cloudwatch_metric_alarm" "alb_5xx_errors" {
  count               = var.sns_topic_arn != "" && var.alb_arn_suffix != "" ? 1 : 0
  alarm_name          = "${var.project_name}-${var.env}-alb-high-5xx-error-rate"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "HTTPCode_Target_5XX_Count"
  namespace           = "AWS/ApplicationELB"
  period              = "60"
  statistic           = "Sum"
  threshold           = "10" # Threshold for 5XX errors
  alarm_description   = "This metric monitors 5XX errors on the ALB"
  actions_enabled     = true
  alarm_actions       = [var.sns_topic_arn]

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
  }
}

# ------------------------------------------------------------------------------
# EC2 / ASG Alarms (EKS Nodes)
# ------------------------------------------------------------------------------

resource "aws_cloudwatch_metric_alarm" "asg_cpu_utilization" {
  count               = var.sns_topic_arn != "" && var.asg_name != "" ? 1 : 0
  alarm_name          = "${var.project_name}-${var.env}-asg-high-cpu-utilization"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/EC2"
  period              = "120"
  statistic           = "Average"
  threshold           = "80" # 80% CPU Utilization
  alarm_description   = "This metric monitors ASG CPU utilization"
  actions_enabled     = true
  alarm_actions       = [var.sns_topic_arn]

  dimensions = {
    AutoScalingGroupName = var.asg_name
  }
}

# ------------------------------------------------------------------------------
# Billing Alarm
# ------------------------------------------------------------------------------

resource "aws_cloudwatch_metric_alarm" "billing_alarm" {
  count               = var.sns_topic_arn != "" ? 1 : 0
  alarm_name          = "${var.project_name}-${var.env}-billing-alarm-${var.currency}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "EstimatedCharges"
  namespace           = "AWS/Billing"
  period              = "21600" # 6 hours
  statistic           = "Maximum"
  threshold           = var.monthly_budget_limit
  alarm_description   = "Alarm when AWS charges exceed the monthly budget"
  actions_enabled     = true
  alarm_actions       = [var.sns_topic_arn]

  dimensions = {
    Currency = var.currency
  }
}
