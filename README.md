# TrackEvent Web API

使用者行為事件追蹤系統 - 基於 ASP.NET Core 9.0 與 Elasticsearch

## 專案說明

本專案實作了一個完整的使用者行為事件追蹤 API，支援 Web、iOS、Android 多平台的事件追蹤，可用於：

- 功能使用熱度分析
- 漏斗分析
- 留存分析
- A/B Test 實驗分析

## 技術堆疊

- **框架**: ASP.NET Core 9.0
- **資料庫**: Elasticsearch 8.x+
- **客戶端**: Elastic.Clients.Elasticsearch
- **API 文件**: Swagger/OpenAPI

## 架構設計

採用 Clean Architecture 分層架構設計：

```
TrackEvent.WebApi/
├── Domain/              # 領域層 - 實體
│   └── Entities/
├── Contracts/           # 契約層 - DTOs、Request/Response
│   ├── Requests/
│   └── Responses/
├── Handlers/            # 處理器層 - 業務邏輯
├── Infrastructure/      # 基礎設施層 - 資料存取
│   └── Repositories/   # 儲存庫 (Elasticsearch)
├── Controllers/         # 控制器層 - API 端點
└── Middlewares/         # 中介軟體 - 全域處理
```

## 快速開始

### 1. 環境需求

- .NET 9.0 SDK
- Docker & Docker Compose

### 2. Elasticsearch 設定

本專案使用 Docker Compose 啟動 Elasticsearch 和 Kibana。

```bash
# 啟動 Elasticsearch 和 Kibana 容器
docker-compose up -d
```
- Elasticsearch 將運行於 `http://localhost:9200`
- Kibana 將運行於 `http://localhost:5601`

更多手動設定細節，請參考 [ELASTICSEARCH_SETUP.md](./ELASTICSEARCH_SETUP.md)。

### 3. 設定連線字串

編輯 `src/TrackEvent.WebApi/appsettings.Development.json`：

```json
{
  "Elasticsearch": {
    "Uri": "http://localhost:9200"
  }
}
```

### 4. 執行專案

```bash
# 編譯專案
dotnet build

# 執行 WebApi
dotnet run --project src/TrackEvent.WebApi
```

API 將會在以下位置啟動：

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## API 使用範例

### 追蹤事件 API

**Endpoint**: `POST /api/v1/track/event`

**Request Body**:

```json
{
  "user_id": "u_123456",
  "anonymous_id": "anon_abcd1234",
  "client_id": "c_browser_987654321",
  "session_id": "s_20251211_0001",
  "event_time": "2025-12-11T10:55:23Z",
  "source": "web",
  "event_type": "click",
  "feature_id": "btn_export_report",
  "feature_name": "export_report_button",
  "feature_type": "button",
  "action": "click",
  "page_url": "https://example.com/dashboard/overview",
  "page_name": "dashboard_overview",
  "device_type": "desktop",
  "os": "Windows",
  "os_version": "11",
  "browser": "Chrome",
  "browser_version": "131.0.0.0",
  "locale": "zh-TW",
  "experiments": {
    "exp_new_onboarding": "variant_B",
    "exp_pricing_layout": "control"
  },
  "metadata": {
    "button_text": "匯出報表",
    "position": "top_right",
    "plan_type": "pro",
    "is_trial_user": true
  }
}
```

**Response (200 OK)**:

```json
{
  "status": "ok",
  "event_id": "evt_20251211_0000012345",
  "received_at": "2025-12-11T10:55:24Z"
}
```

**Error Response (400 Bad Request)**:

```json
{
  "status": "error",
  "error_code": "INVALID_PAYLOAD",
  "message": "field 'client_id' is required",
  "details": {
    "field": "client_id",
    "reason": "missing"
  }
}
```

### 使用 cURL 測試

```bash
curl -X POST https://localhost:5001/api/v1/track/event \
  -H "Content-Type: application/json" \
  -d '{
    "client_id": "c_test_001",
    "session_id": "s_test_001",
    "event_time": "2025-12-11T10:55:23Z",
    "source": "web",
    "event_type": "click",
    "feature_id": "btn_test"
  }'
```

## Elasticsearch 設計

詳細的 Index Mapping、Lifecycle Policy (ILM) 與查詢策略，請參考：

- [CLAUDE.md](./CLAUDE.md) - 完整設計規格書

### 主要設計

- **Index Pattern**: `user-events-YYYY.MM.DD`，每日輪轉。
- **Write Alias**: `user-events-write`，寫入時指向當前的索引。
- **Read Alias**: `user-events-read`，讀取時涵蓋所有相關索引。
- **Index Template**: 自動為新索引套用 Mapping 和 Settings。
- **ILM**: 自動管理 Hot/Warm/Cold/Delete 階段。
- **Mapping**: 使用 `keyword` 進行精確匹配與聚合，`object` 處理彈性欄位。

## 專案結構說明

### Domain 層

- `UserEvent.cs`: 使用者事件實體，對應 Elasticsearch document。

### Contracts 層

- `TrackEventRequest.cs`: 追蹤事件請求 DTO。
- `TrackEventResponse.cs`: 成功回應 DTO。
- `ErrorResponse.cs`: 錯誤回應 DTO。

### Handlers 層

- `TrackEventHandler.cs`: 處理追蹤事件的業務邏輯，並將事件寫入 Elasticsearch。

### Infrastructure 層

- `IUserEventRepository.cs`: 儲存庫介面。
- `UserEventRepository.cs`: 使用 `Elastic.Clients.Elasticsearch` 與 Elasticsearch 互動的儲存庫實作。

### Controllers 層

- `TrackController.cs`: 追蹤事件 API 控制器。

### Middlewares 層

- `ExceptionHandlingMiddleware.cs`: 全域例外處理中介軟體。

## 設計特色

### 1. Clean Architecture

- 清晰的分層設計，將業務邏輯與基礎設施分離。
- 依賴倒置原則（DIP）。

### 2. Result Pattern

- 使用 `Result<T>` 模式進行錯誤處理，避免使用例外處理業務邏輯錯誤。

### 3. Immutable Objects

- 使用 C# record 定義不可變的 DTO，確保資料在傳遞過程中的一致性。

### 4. Elasticsearch 整合

- 利用 Elasticsearch 的 `keyword` 型別進行高效能聚合分析。
- `object` 型別提供 `experiments` 和 `metadata` 欄位的極高擴充彈性。
- 透過 Index Template 與 ILM 實現自動化的索引生命週期管理。

## 後續開發建議

- [ ] 加入 FluentValidation 進行更完整的驗證
- [ ] 實作批次追蹤 API (`POST /api/v1/track/events/batch`)
- [ ] 實作 API Key 認證
- [ ] 加入 Serilog 結構化日誌，並輸出到 Elasticsearch
- [ ] 撰寫單元測試和整合測試
- [ ] 實作 Rate Limiting
- [ ] 加入 Health Check endpoint，檢查與 Elasticsearch 的連線狀態
- [ ] 建立 Docker 映像檔
- [ ] 設定 CI/CD Pipeline

## 參考文件

- [CLAUDE.md](./CLAUDE.md) - 完整設計規格書
- [API Template](https://github.com/yaochangyu/api.template) - 參考架構設計

## 授權

MIT License
