# Elasticsearch 環境設定指南

本專案已從 PostgreSQL 遷移至 Elasticsearch。以下是設定與使用指南。

---

## 📋 前置需求

- Docker Desktop（用於本地開發）
- .NET 9.0 SDK
- Bash（Windows 使用 Git Bash 或 WSL2）

---

## 🚀 快速開始

### 方法一：使用 Docker（推薦）

#### 1. 啟動 Elasticsearch 和 Kibana

```bash
# 在專案根目錄執行
docker compose up -d
```

等待容器健康檢查通過（約 30-60 秒）：

```bash
# 檢查容器狀態
docker compose ps

# 檢查 Elasticsearch 健康狀態
curl http://localhost:9200/_cluster/health
```

#### 2. 初始化 Elasticsearch

```bash
# 執行初始化腳本
./scripts/init-elasticsearch.sh
```

這個腳本會建立：
- ILM Policy（生命週期管理策略）
- Index Template（索引模板）
- 初始索引和別名

#### 3. 驗證設定

開啟瀏覽器訪問：
- **Elasticsearch**: http://localhost:9200
- **Kibana**: http://localhost:5601

在 Kibana 中檢查：
1. Management → Stack Management → Index Management
2. 應該看到 `user-events-YYYY.MM.DD` 索引

---

### 方法二：手動安裝 Elasticsearch

如果無法使用 Docker，請參考 [Elasticsearch 官方文件](https://www.elastic.co/guide/en/elasticsearch/reference/8.15/install-elasticsearch.html) 手動安裝。

安裝後，執行初始化腳本：

```bash
export ELASTICSEARCH_URL=http://localhost:9200
./scripts/init-elasticsearch.sh
```

---

## 🔧 開發環境設定

### 環境變數

確保 `.env` 檔案包含：

```env
ELASTICSEARCH_URL=http://localhost:9200
```

### 啟動 API

```bash
cd src/TrackEvent.WebApi
dotnet run
```

API 將在以下位址運行：
- **API**: https://localhost:5001 或 http://localhost:5000
- **Swagger**: https://localhost:5001/swagger

---

## 📊 測試 API

### 使用 Swagger UI

1. 開啟 https://localhost:5001/swagger
2. 展開 `POST /api/v1/track/event`
3. 點擊 "Try it out"
4. 使用以下範例請求：

```json
{
  "user_id": "u_test_001",
  "client_id": "c_browser_001",
  "session_id": "s_20251211_001",
  "event_time": "2025-12-11T14:00:00Z",
  "source": "web",
  "event_type": "click",
  "feature_id": "btn_test",
  "feature_name": "test_button",
  "page_name": "test_page",
  "experiments": {
    "exp_test": "variant_A"
  },
  "metadata": {
    "test": true
  }
}
```

### 使用 curl

```bash
curl -X POST http://localhost:5000/api/v1/track/event \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "u_test_001",
    "client_id": "c_browser_001",
    "session_id": "s_20251211_001",
    "event_time": "2025-12-11T14:00:00Z",
    "source": "web",
    "event_type": "click",
    "feature_id": "btn_test",
    "page_name": "test_page"
  }'
```

---

## 🔍 查詢 Elasticsearch 資料

### 使用 Kibana Dev Tools

1. 開啟 Kibana: http://localhost:5601
2. 左側選單 → Management → Dev Tools
3. 執行查詢：

#### 查看所有事件

```json
GET user-events-read/_search
{
  "size": 10,
  "sort": [
    {
      "eventTime": {
        "order": "desc"
      }
    }
  ]
}
```

#### 查詢特定功能的點擊

```json
GET user-events-read/_search
{
  "query": {
    "bool": {
      "must": [
        { "term": { "featureId": "btn_test" } },
        { "term": { "eventType": "click" } }
      ]
    }
  }
}
```

#### 聚合分析：各功能使用次數

```json
GET user-events-read/_search
{
  "size": 0,
  "aggs": {
    "features": {
      "terms": {
        "field": "featureId",
        "size": 20
      }
    }
  }
}
```

---

## 📈 Kibana 視覺化建議

### 建立 Data View

1. Kibana → Management → Stack Management → Data Views
2. Create data view
   - Name: `user-events`
   - Index pattern: `user-events-*`
   - Timestamp field: `eventTime`

### 建議的儀表板圖表

1. **功能使用熱度** - Vertical Bar Chart
   - X-axis: `featureId`
   - Y-axis: Count

2. **來源平台分佈** - Pie Chart
   - Slice by: `source`

3. **事件時間趨勢** - Line Chart
   - X-axis: `eventTime` (Date Histogram)
   - Y-axis: Count

4. **A/B 測試分析** - Data Table
   - Rows: `experiments.exp_test`
   - Metrics: Count, Unique `userId`

---

## 🛠 常見問題

### Docker 憑證問題

如果遇到 `docker-credential-desktop.exe` 錯誤：

**解決方法一：修改 Docker 設定**

編輯 `~/.docker/config.json`，移除或註解 `credsStore` 行：

```json
{
  // "credsStore": "desktop.exe"
}
```

**解決方法二：手動下載映像檔**

```bash
docker pull docker.elastic.co/elasticsearch/elasticsearch:8.15.0
docker pull docker.elastic.co/kibana/kibana:8.15.0
```

然後再次執行 `docker compose up -d`。

### Elasticsearch 記憶體不足

如果遇到 OOM 錯誤，調整 `docker-compose.yml` 中的 Java Heap Size：

```yaml
environment:
  - "ES_JAVA_OPTS=-Xms1g -Xmx1g"  # 調整為 1GB
```

### API 無法連線到 Elasticsearch

1. 檢查 Elasticsearch 是否運行：
   ```bash
   curl http://localhost:9200
   ```

2. 檢查 `.env` 檔案是否正確：
   ```bash
   cat .env
   ```

3. 檢查防火牆設定

---

## 📚 更多資源

- [Elasticsearch 官方文件](https://www.elastic.co/guide/en/elasticsearch/reference/8.15/index.html)
- [Kibana 官方文件](https://www.elastic.co/guide/en/kibana/8.15/index.html)
- [專案設計規格](./CLAUDE.md)

---

## 🔄 回滾到 PostgreSQL

如果需要回滾到 PostgreSQL，請參考 Git 歷史記錄：

```bash
# 查看遷移前的 commit
git log --oneline

# 回滾到特定 commit
git checkout <commit-hash>
```
