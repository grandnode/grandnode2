# Grafana Configuration Guide

## Accessing Grafana
1. Run the port-forward command:
   ```bash
   kubectl port-forward svc/my-monitoring-grafana 3000:80 -n monitoring
   ```
2. Open [http://localhost:3000](http://localhost:3000) in your browser.
3. Login with `admin` / `admin` (or the password you set).

## 1. Golden Signals Dashboard (SRE Standard)
Create a new dashboard and add the following panels.

### A. Latency (Response Time)
* **Visualization**: Time Series
* **PromQL Query**:
  ```promql
  sum(rate(http_request_duration_seconds_sum[5m])) by (job) 
  / 
  sum(rate(http_request_duration_seconds_count[5m])) by (job)
  ```
  *(Note: Adjust metric name `http_request_duration_seconds` based on your ingress/app metrics)*

### B. Traffic (Requests per Second)
* **Visualization**: Time Series
* **PromQL Query**:
  ```promql
  sum(rate(http_requests_total[5m])) by (job)
  ```
  *(Note: `http_requests_total` is standard for many exporters)*

### C. Errors (Error Rate)
* **Visualization**: Time Series
* **PromQL Query (5xx Errors)**:
  ```promql
  sum(rate(http_requests_total{status=~"5.."}[5m])) 
  / 
  sum(rate(http_requests_total[5m]))
  ```

### D. Saturation (CPU/Memory)
* **Visualization**: Gauge or Time Series
* **PromQL Query (CPU)**:
  ```promql
  sum(rate(container_cpu_usage_seconds_total[5m])) by (pod)
  ```

## 2. Connecting Loki (Logs) to Grafana
1. Go to **Configuration** (Gear Icon) -> **Data Sources**.
2. Click **Add data source**.
3. Select **Loki**.
4. URL: `http://loki:3100` (This is the internal service URL in K8s).
5. Click **Save & Test**.

## 3. Investigating Spikes
1. Go to **Explore** (Compass Icon).
2. Select **Loki** as the data source at the top.
3. Open "Split" view.
4. On the right, select **Prometheus**.
5. Find a spike in your Traffic graph (Prometheus).
6. On the left (Loki), query logs for that time range:
   ```logql
   {app="your-app-name"} |= "error"
   ```
