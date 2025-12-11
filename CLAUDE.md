# 使用者行為事件追蹤設計規格書
（API + PostgreSQL Schema）

版本：v1  
Endpoint：`POST /api/v1/track/event`  
DB 主要表：`user_events`

---

## 1. 設計目標與範疇

- 追蹤 Web / iOS / Android 上的使用者行為事件（以「功能點擊」為主）。
- 支援：
    - 功能使用熱度分析
    - 漏斗分析
    - 留存分析
    - 多個 A/B Test 實驗分析
- 後端儲存採 **Append-only event log**，使用 PostgreSQL。

特性：

- 一次只接收 **一筆事件**（非批次）。
- 支援登入與匿名用戶。
- 支援多平台（Web / App）。
- 支援多實驗（A/B Test）。

---

## 2. API 設計規則

### 2.1 Endpoint 基本資訊

- **Method**：`POST`
- **Path**：`/api/v1/track/event`
- **Content-Type**：`application/json; charset=utf-8`

### 2.2 Request Body：結構總覽

頂層欄位分類：

1. 身份識別（identity）
2. 事件核心資訊（event）
3. 功能與行為資訊（feature）
4. 場景上下文（context）
5. 環境資訊（environment）
6. 實驗資訊（experiments）
7. 彈性附加資訊（metadata）

實際 JSON：扁平欄位 + 兩個物件欄位 `experiments`、`metadata`。

#### 2.2.1 Request 範例

````markdown
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
  "screen_name": null,

  "previous_page_url": "https://example.com/login",
  "previous_page_name": "login_page",
  "previous_screen_name": null,

  "device_type": "desktop",
  "os": "Windows",
  "os_version": "11",
  "browser": "Chrome",
  "browser_version": "131.0.0.0",
  "app_version": null,
  "build_number": null,
  "network_type": "wifi",
  "locale": "zh-TW",

  "experiments": {
    "exp_new_onboarding": "variant_B",
    "exp_pricing_layout": "control"
  },

  "metadata": {
    "button_text": "匯出報表",
    "position": "top_right",
    "section": "summary_panel",
    "plan_type": "pro",
    "is_trial_user": true,
    "extra": {
      "report_type": "monthly",
      "filter_applied": true
    }
  }
}
````

> 不適用的欄位，可為 `null` 或直接不傳，由 API 驗證策略決定。

---

### 2.3 欄位規格

#### 2.3.1 身份識別（Identity）

| 欄位名稱     | 型別   | 必填 | 說明 |
|--------------|--------|------|------|
| user_id      | string | 否   | 已登入用戶 ID。匿名事件可不傳或為 null。 |
| anonymous_id | string | 否   | 匿名用戶 ID（cookie/device 產生）。用於未登入追蹤。 |
| client_id    | string | 是   | 裝置或瀏覽器實體 ID。Web=Cookie；App=Device ID/UUID。 |
| session_id   | string | 是   | 一次連續使用行為的 Session ID。 |

規則建議：

- 匿名事件：
    - 建議有：`anonymous_id`、`client_id`、`session_id`
    - `user_id` 空
- 登入事件：
    - 建議有：`user_id`、`client_id`、`session_id`
    - 若曾匿名，`anonymous_id` 保留以便行為串接。

---

#### 2.3.2 事件核心資訊（Event Core）

| 欄位名稱   | 型別   | 必填 | 說明 |
|------------|--------|------|------|
| event_time | string | 是   | ISO8601 UTC，例如 \[2025-12-11T10:55:23Z\]。 |
| source     | string | 是   | `"web"`, `"app_ios"`, `"app_android"`。 |
| event_type | string | 是   | 事件類型，如 `"click"`, `"page_view"`, `"screen_view"`, `"submit"`。目前以 `"click"` 為主。 |

---

#### 2.3.3 功能與行為資訊（Feature / Action）

| 欄位名稱     | 型別   | 必填 | 說明 |
|--------------|--------|------|------|
| feature_id   | string | 是   | 功能唯一 ID，穩定且不隨文案改變，如 `"btn_export_report"`。 |
| feature_name | string | 否   | 可讀名稱，如 `"export_report_button"`。 |
| feature_type | string | 否   | `"button"`, `"tab"`, `"link"`, `"icon"`, `"menu_item"`, `"cta"` 等。 |
| action       | string | 否   | `"click"`, `"open"`, `"close"`, `"toggle"` 等。 |

建議：

- 目前以 `event_type = "click"` + `action = "click"` 為主。
- 未來有更多互動型態時，優先用 `action` 區分。

---

#### 2.3.4 場景上下文（Context）

**Web**

| 欄位名稱           | 型別   | 必填 | 說明 |
|--------------------|--------|------|------|
| page_url           | string | 否   | 當前頁面 URL 或 path。 |
| page_name          | string | 否   | 當前頁邏輯名稱，如 `"dashboard_overview"`。 |
| previous_page_url  | string | 否   | 上一頁 URL（referrer）。 |
| previous_page_name | string | 否   | 上一頁邏輯名稱，如 `"login_page"`。 |

**App**

| 欄位名稱            | 型別   | 必填 | 說明 |
|---------------------|--------|------|------|
| screen_name         | string | 否   | 當前畫面名稱，如 `"DashboardScreen"`。 |
| previous_screen_name| string | 否   | 上一畫面名稱，如 `"LoginScreen"`。 |

---

#### 2.3.5 環境資訊（Environment）

| 欄位名稱        | 型別   | 必填 | 說明 |
|-----------------|--------|------|------|
| device_type     | string | 否   | `"desktop"`, `"mobile"`, `"tablet"`。 |
| os              | string | 否   | `"Windows"`, `"macOS"`, `"iOS"`, `"Android"`。 |
| os_version      | string | 否   | OS 版本，如 `"11"`, `"14.4"`。 |
| browser         | string | 否   | Web 瀏覽器名稱，如 `"Chrome"`。 |
| browser_version | string | 否   | 瀏覽器版本。 |
| app_version     | string | 否   | App 版本號。 |
| build_number    | string | 否   | App build number。 |
| network_type    | string | 否   | `"wifi"`, `"4G"`, `"5G"` 等。 |
| locale          | string | 否   | 語系，如 `"zh-TW"`, `"en-US"`。 |

---

#### 2.3.6 實驗資訊（Experiments）

| 欄位名稱  | 型別  | 必填 | 說明 |
|-----------|-------|------|------|
| experiments | object | 否 | key-value，key 為實驗 ID，value 為 variant 名稱。 |

範例：

````markdown
"experiments": {
  "exp_new_onboarding": "variant_B",
  "exp_pricing_layout": "control"
}
````

使用方式：

- 報表可依 `experiments.exp_new_onboarding` 分組，分析各 variant 表現。
- 無實驗時，可以不傳或傳 `{}`。

---

#### 2.3.7 彈性附加資訊（Metadata）

| 欄位名稱 | 型別  | 必填 | 說明 |
|----------|-------|------|------|
| metadata | object | 否 | 自由延伸欄位，支援巢狀 JSON，放業務/ UI 特定資訊。 |

範例：

````markdown
"metadata": {
  "button_text": "匯出報表",
  "position": "top_right",
  "section": "summary_panel",
  "plan_type": "pro",
  "is_trial_user": true,
  "extra": {
    "report_type": "monthly",
    "filter_applied": true
  }
}
````

建議：

- 先放進 `metadata`，後續若變成通用且穩定的欄位，再拉到頂層欄位或其他維度表。

---

### 2.4 Response 設計

#### 2.4.1 成功 Response

- HTTP Status：`200 OK`

Body：

````markdown
{
  "status": "ok",
  "event_id": "evt_20251211_0000012345",
  "received_at": "2025-12-11T10:55:24Z"
}
````

| 欄位名稱    | 型別   | 說明 |
|-------------|--------|------|
| status      | string | `"ok"` 表示成功。 |
| event_id    | string | 後端生成的唯一事件 ID，對應 DB 欄位。 |
| received_at | string | 後端接收並寫入時間（ISO 8601 UTC）。 |

---

#### 2.4.2 失敗 Response

常見 HTTP Status：

- `400 Bad Request`：payload 缺少必填欄位、格式錯誤
- `401 Unauthorized`：驗證失敗（若有 API key / token）
- `429 Too Many Requests`：超過流量限制
- `500 Internal Server Error`：伺服器內部錯誤

Body 範例：

**欄位錯誤**

````markdown
{
  "status": "error",
  "error_code": "INVALID_PAYLOAD",
  "message": "field 'client_id' is required",
  "details": {
    "field": "client_id",
    "reason": "missing"
  }
}
````

**驗證錯誤**

````markdown
{
  "status": "error",
  "error_code": "UNAUTHORIZED",
  "message": "missing or invalid API token"
}
````

---

### 2.5 錯誤碼規則

| error_code          | 說明 |
|---------------------|------|
| INVALID_PAYLOAD     | JSON 結構錯誤、欄位型別錯誤、必填欄位缺漏。 |
| UNAUTHORIZED        | 驗證失敗（API key / token 無效或缺失）。 |
| FORBIDDEN（選配）   | 認證通過但無存取權限。 |
| RATE_LIMIT_EXCEEDED | 超過 API 流量限制。 |
| INTERNAL_ERROR      | 未預期的伺服器錯誤。 |

---

## 3. PostgreSQL DB Schema 設計規則

### 3.1 主事件表：`user_events`

**用途**：儲存每一筆使用者事件（append-only）。

```sql
CREATE TABLE user_events (
    -- 主鍵與對外 ID
    id              BIGSERIAL PRIMARY KEY,
    event_id        VARCHAR(64) NOT NULL UNIQUE,

    -- 身份識別
    user_id         VARCHAR(64),
    anonymous_id    VARCHAR(128),
    client_id       VARCHAR(128) NOT NULL,
    session_id      VARCHAR(128) NOT NULL,

    -- 事件核心資訊
    event_time      TIMESTAMPTZ NOT NULL,
    source          VARCHAR(32) NOT NULL,
    event_type      VARCHAR(32) NOT NULL,

    -- 功能與行為資訊
    feature_id      VARCHAR(128) NOT NULL,
    feature_name    VARCHAR(256),
    feature_type    VARCHAR(64),
    action          VARCHAR(64),

    -- 場景上下文 (Web)
    page_url            TEXT,
    page_name           VARCHAR(256),
    previous_page_url   TEXT,
    previous_page_name  VARCHAR(256),

    -- 場景上下文 (App)
    screen_name         VARCHAR(256),
    previous_screen_name VARCHAR(256),

    -- 環境資訊
    device_type     VARCHAR(32),
    os              VARCHAR(64),
    os_version      VARCHAR(64),
    browser         VARCHAR(64),
    browser_version VARCHAR(64),
    app_version     VARCHAR(64),
    build_number    VARCHAR(64),
    network_type    VARCHAR(32),
    locale          VARCHAR(16),

    -- 實驗 / 附加資訊
    experiments     JSONB,
    metadata        JSONB,

    -- 系統欄位
    received_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

#### 3.1.1 DB 欄位與 API 欄位對應

- `event_id`：由後端產生（可為 GUID 或自訂字串，回傳至 API 客戶端）。
- `event_time`：對應 Request 的 `event_time`（UTC）。
- `received_at`：寫入時的伺服器時間。
- `experiments`：對應 Request 的 `experiments` 物件。
- `metadata`：對應 Request 的 `metadata` 物件。

#### 3.1.2 非空限制對應 API 必填策略

- `client_id`：`NOT NULL`，API 必填。
- `session_id`：`NOT NULL`，API 必填。
- `event_time`：`NOT NULL`，API 必填。
- `source`：`NOT NULL`，API 必填。
- `event_type`：`NOT NULL`，API 必填。
- `feature_id`：`NOT NULL`，API 必填。

---

### 3.2 索引設計

針對常用查詢維度設計索引：

```sql
-- 時間排序 / 篩選
CREATE INDEX idx_user_events_event_time
    ON user_events (event_time DESC);

-- 功能使用分析：某功能在一段時間的使用
CREATE INDEX idx_user_events_feature_time
    ON user_events (feature_id, event_time DESC);

-- 用戶留存與行為路徑
CREATE INDEX idx_user_events_user_time
    ON user_events (user_id, event_time DESC);

-- 裝置與 Session 分析
CREATE INDEX idx_user_events_client_session_time
    ON user_events (client_id, session_id, event_time DESC);

-- 來源平台過濾
CREATE INDEX idx_user_events_source_time
    ON user_events (source, event_time DESC);

-- 實驗分群分析：指定實驗 key
CREATE INDEX idx_user_events_exp_new_onboarding
    ON user_events ((experiments->>'exp_new_onboarding'));

-- metadata 中常用欄位 (如 plan_type)
CREATE INDEX idx_user_events_metadata_plan_type
    ON user_events ((metadata->>'plan_type'));
```

實務建議：

- 索引數量需衡量：多索引會影響寫入效能與磁碟空間。
- 先針對「時間 + 功能 + 用戶 + source」建立索引，再依實際報表需求追加 jsonb 索引。

---

### 3.3 選配維度表設計

#### 3.3.1 功能維度表：`features`

```sql
CREATE TABLE features (
    feature_id      VARCHAR(128) PRIMARY KEY,
    name            VARCHAR(256),
    type            VARCHAR(64),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);
```

用途：

- 對 `feature_id` 進行集中管理（名稱、類型、說明）。
- 報表前端可 join 該表獲取完整功能資訊。

---

#### 3.3.2 實驗維度表（選配）：`experiments`

```sql
CREATE TABLE experiments (
    experiment_key  VARCHAR(128) PRIMARY KEY,
    name            VARCHAR(256),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);
```

用途：

- 集中管理實驗清單與說明。
- 與 `user_events.experiments` 為邏輯關聯，不強制外鍵以避免寫入成本增加。

---

### 3.4 時間與 Partition 策略（建議）

- 所有時間欄位以 UTC 儲存（`TIMESTAMPTZ`）。
- 未來事件量大時，建議用 **時間分區**（例如按月）：

```sql
-- 示例：建立分區表骨幹
CREATE TABLE user_events_partitioned (
    LIKE user_events INCLUDING ALL
) PARTITION BY RANGE (event_time);
```

後續每月建立子分區：

```sql
CREATE TABLE user_events_2025_12 PARTITION OF user_events_partitioned
FOR VALUES FROM ('2025-12-01') TO ('2026-01-01');
```

---

## 4. API 與 DB 的整體規則與實務建議

### 4.1 時間規則

- `event_time`（Request）與 `received_at`（DB/Response）：
    - 一律使用 UTC。
    - 格式：ISO 8601（例如 \[2025-12-11T10:55:23Z\]）。
    - 時區轉換放在報表與分析層處理。

### 4.2 必填欄位策略（API + DB）

最低要求（API 必填 + DB `NOT NULL`）：

- `client_id`
- `session_id`
- `event_time`
- `source`
- `event_type`
- `feature_id`

這些欄位是：

- 做漏斗分析時的關鍵（session / event_time / feature）。
- 做留存分析時的關鍵（user/client + event_time）。
- 做功能熱度分析時的關鍵（feature + source + time）。

### 4.3 Web / App 實務欄位組合

- Web 事件通常會送：
    - `user_id`（登入時）、`anonymous_id`、`client_id`、`session_id`
    - `page_url`、`page_name`、`previous_page_*`
    - `browser`、`os`、`device_type`、`locale`
- App 事件通常會送：
    - `user_id`、`anonymous_id`、`client_id`、`session_id`
    - `screen_name`、`previous_screen_name`
    - `app_version`、`os`、`device_type`、`network_type`、`locale`

### 4.4 A/B Test 實務

- 前端在 session 開始時就決定好每個實驗的分流，整個 session 都使用同一組 `experiments`。
- 所有事件都附上 `experiments`，減少後端拼接成本。
- 常用實驗 key 可建立 jsonb 索引，提升 variant 分群分析效率。

### 4.5 metadata 的演進策略

- 初期：大量資訊先進 `metadata`，快速上線。
- 收集一段時間後：
    - 觀察報表最常用的 metadata 欄位（如 `plan_type`, `is_trial_user`）。
    - 逐步拉成頂層欄位或獨立維度表，保留原欄位在 metadata 中以相容舊資料。

### 4.6 擴充：批次 API（未來）

目前：`POST /api/v1/track/event` （單筆）

未來如需批量追蹤：

- 新增：`POST /api/v1/track/events/batch`
- Request：`[{...TrackEventRequest}, {...}, ...]`
- DB：仍寫入 `user_events`，可使用 bulk insert 提升效能。

---

若你接下來需要，我可以基於這份規格，再幫你產出：

- 對應的 **OpenAPI/Swagger YAML** 範本
- ASP.NET Core Controller + EF Core Entity 的示範程式碼
- 常見分析 SQL 範例（例如：某功能一週點擊數、實驗 variant 的轉換率查詢）