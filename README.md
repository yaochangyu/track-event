# TrackEvent Web API

使用者行為事件追蹤系統 - 基於 ASP.NET Core 9.0 與 PostgreSQL

## 專案說明

本專案實作了一個完整的使用者行為事件追蹤 API，支援 Web、iOS、Android 多平台的事件追蹤，可用於：

- 功能使用熱度分析
- 漏斗分析
- 留存分析
- A/B Test 實驗分析

## 技術堆疊

- **框架**: ASP.NET Core 9.0
- **資料庫**: PostgreSQL 14+
- **ORM**: Entity Framework Core 9.0
- **API 文件**: Swagger/OpenAPI

## 架構設計

採用 Clean Architecture 分層架構設計：

```
TrackEvent.WebApi/
├── Domain/              # 領域層 - 實體和值物件
│   └── Entities/
├── Contracts/           # 契約層 - DTOs、Request/Response
│   ├── Requests/
│   └── Responses/
├── Handlers/            # 處理器層 - 業務邏輯
├── Infrastructure/      # 基礎設施層 - 資料存取
│   ├── Data/           # DbContext
│   └── Repositories/   # 儲存庫
├── Controllers/         # 控制器層 - API 端點
└── Middlewares/         # 中介軟體 - 全域處理
```

## 快速開始

### 1. 環境需求

- .NET 9.0 SDK
- PostgreSQL 14+
- (選用) Docker & Docker Compose

### 2. 資料庫設定

#### 方式一：使用 PostgreSQL

```bash
# 1. 建立資料庫
createdb track_event

# 2. 執行 Schema 腳本
psql -d track_event -f db/init-schema.sql
```

#### 方式二：使用 Docker Compose（推薦）

```bash
# 啟動 PostgreSQL 容器
docker-compose up -d
```

### 3. 設定連線字串

編輯 `src/TrackEvent.WebApi/appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=track_event;Username=postgres;Password=postgres"
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

## 資料庫 Schema

詳細的資料庫 Schema 設計請參考：
- [CLAUDE.md](./CLAUDE.md) - 完整設計規格書
- [db/init-schema.sql](./db/init-schema.sql) - SQL Schema 腳本

### 主要資料表

#### `user_events`
儲存所有使用者事件（append-only），包含：
- 身份識別（user_id, client_id, session_id）
- 事件資訊（event_time, source, event_type, feature_id）
- 場景上下文（page_url, screen_name）
- 環境資訊（device_type, os, browser）
- 實驗資訊（experiments - JSONB）
- 附加資訊（metadata - JSONB）

## 專案結構說明

### Domain 層
- `UserEvent.cs`: 使用者事件實體，對應 `user_events` 資料表

### Contracts 層
- `TrackEventRequest.cs`: 追蹤事件請求 DTO
- `TrackEventResponse.cs`: 成功回應 DTO
- `ErrorResponse.cs`: 錯誤回應 DTO

### Handlers 層
- `TrackEventHandler.cs`: 處理追蹤事件的業務邏輯
  - 實作 Result Pattern 進行錯誤處理
  - 驗證必填欄位
  - 產生唯一 EventId
  - 將事件儲存至資料庫

### Infrastructure 層
- `TrackEventDbContext.cs`: EF Core 資料庫上下文
- `IUserEventRepository.cs`: 儲存庫介面
- `UserEventRepository.cs`: 儲存庫實作

### Controllers 層
- `TrackController.cs`: 追蹤事件 API 控制器

### Middlewares 層
- `ExceptionHandlingMiddleware.cs`: 全域例外處理中介軟體

## 設計特色

### 1. Clean Architecture
- 清晰的分層設計
- 依賴倒置原則（DIP）
- 關注點分離（SoC）

### 2. Result Pattern
- 使用 Result<T> 模式進行錯誤處理
- 避免使用例外處理業務邏輯錯誤
- 提供更好的錯誤追蹤

### 3. Immutable Objects
- 使用 C# record 定義不可變的 DTO
- 確保資料一致性

### 4. 全域錯誤處理
- Middleware 集中處理未預期的例外
- 統一的錯誤回應格式

### 5. PostgreSQL JSONB
- 彈性的 experiments 和 metadata 欄位
- 支援未來擴充而不需修改 Schema

## 後續開發建議

- [ ] 加入 FluentValidation 進行更完整的驗證
- [ ] 實作批次追蹤 API (`POST /api/v1/track/events/batch`)
- [ ] 加入 Redis 快取層
- [ ] 實作 API Key 認證
- [ ] 加入 Serilog 結構化日誌
- [ ] 撰寫單元測試和整合測試
- [ ] 實作 Rate Limiting
- [ ] 加入 Health Check endpoint
- [ ] 建立 Docker 映像檔
- [ ] 設定 CI/CD Pipeline

## 參考文件

- [CLAUDE.md](./CLAUDE.md) - 完整設計規格書
- [API Template](https://github.com/yaochangyu/api.template) - 參考架構設計

## 授權

MIT License
