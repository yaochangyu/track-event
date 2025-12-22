# 使用者行為事件追蹤設計規格書
（API + Elasticsearch Schema）

版本：v2
Endpoint：`POST /api/v1/track/event`
Elasticsearch Index：`user-events-*`（按日期輪轉）

---

## 1. 設計目標與範疇

- 追蹤 Web / iOS / Android 上的使用者行為事件（以「功能點擊」為主）。
- 支援：
    - 功能使用熱度分析
    - 漏斗分析
    - 留存分析
    - 多個 A/B Test 實驗分析
- 後端儲存採 **Append-only event log**，使用 **Elasticsearch**。

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

**範例一：功能點擊事件（Click Event）**

````markdown
{
  "product_id": "prd_main",
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

**範例二：頁面離開事件（Page Leave Event）- 包含停留時間**

````markdown
{
  "product_id": "prd_main",
  "user_id": "u_123456",
  "anonymous_id": "anon_abcd1234",
  "client_id": "c_browser_987654321",
  "session_id": "s_20251211_0001",

  "event_time": "2025-12-11T10:58:45Z",
  "source": "web",
  "event_type": "page_leave",

  "page_url": "https://example.com/dashboard/overview",
  "page_name": "dashboard_overview",
  "previous_page_url": "https://example.com/login",
  "previous_page_name": "login_page",

  "duration_ms": 202000,
  "is_active_duration": true,
  "visibility_changes": 2,

  "device_type": "desktop",
  "os": "Windows",
  "os_version": "11",
  "browser": "Chrome",
  "browser_version": "131.0.0.0",
  "network_type": "wifi",
  "locale": "zh-TW",

  "experiments": {
    "exp_new_onboarding": "variant_B",
    "exp_pricing_layout": "control"
  },

  "metadata": {
    "plan_type": "pro",
    "is_trial_user": true
  }
}
````

> 不適用的欄位，可為 `null` 或直接不傳，由 API 驗證策略決定。

---

### 2.3 欄位規格

#### 2.3.1 身份識別（Identity）

| 欄位名稱     | 型別   | 必填 | 說明 |
|--------------|--------|------|------|
| product_id   | string | 是   | 產品/系統識別碼（多產品用）。建議穩定不變，如 `prd_main`、`prd_pos`。 |
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

`session_id` 在 Client 端產生建議：

- Web：
  - 建議用 `sessionStorage` 保存（Tab-scoped；關閉分頁即結束 session）。
  - 建議用 UUID 產生（`crypto.randomUUID()`），避免用時間戳 + `Math.random()` 當唯一性來源。
  - 建議加入「閒置超時換新」（例如 30 分鐘未送事件就換一個新的 `session_id`），避免一個分頁掛著太久把多段行為混成同一個 session。
- App：
  - 建議保存於本機持久化儲存（Keychain/Keystore/Preferences）。
  - 建議在「回到前景前已在背景超過閾值」（例如 30 分鐘）或「登出」時換新 `session_id`。

---

#### 2.3.2 事件核心資訊（Event Core）

| 欄位名稱   | 型別   | 必填 | 說明 |
|------------|--------|------|------|
| event_time | string | 是   | ISO8601 UTC，例如 \[2025-12-11T10:55:23Z\]。 |
| source     | string | 是   | `"web"`, `"app_ios"`, `"app_android"`。 |
| event_type | string | 是   | 事件類型，如 `"click"`, `"page_view"`, `"page_leave"`, `"screen_view"`, `"submit"`。主要類型：`"click"` 功能點擊、`"page_view"` 頁面進入、`"page_leave"` 頁面離開。 |

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
| duration_ms        | number | 否   | 頁面停留時間（毫秒）。僅在 `event_type = "page_leave"` 時提供。 |
| is_active_duration | boolean | 否  | 是否為有效停留時間（排除分頁未激活時間）。預設 false。 |
| visibility_changes | number | 否   | 頁面可見性變化次數（從隱藏到可見的次數）。 |

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

## 3. Elasticsearch Index 設計規則

### 3.1 Index 命名與輪轉策略

**Index Pattern**：`user-events-YYYY.MM.DD`

範例：
- `user-events-2025.12.11`
- `user-events-2025.12.12`

**別名（Alias）策略**：
- `user-events-write`：寫入別名，指向當前日期的索引
- `user-events-read`：讀取別名，指向所有歷史索引（`user-events-*`）

**Index Lifecycle Management (ILM)**：
- **Hot Phase**（0-7天）：最新數據，高頻查詢與寫入
- **Warm Phase**（8-30天）：降低副本數，減少資源消耗
- **Cold Phase**（31-90天）：移至低成本儲存，查詢頻率低
- **Delete Phase**（91天後）：依據資料保留政策刪除

---

### 3.2 Index Mapping 定義

**用途**：定義事件文檔結構與欄位型別。

```json
{
  "mappings": {
    "properties": {
      "event_id": {
        "type": "keyword"
      },

      "_comment_identity": "身份識別欄位",
      "product_id": {
        "type": "keyword"
      },
      "user_id": {
        "type": "keyword"
      },
      "anonymous_id": {
        "type": "keyword"
      },
      "client_id": {
        "type": "keyword"
      },
      "session_id": {
        "type": "keyword"
      },

      "_comment_event_core": "事件核心資訊",
      "event_time": {
        "type": "date",
        "format": "strict_date_optional_time||epoch_millis"
      },
      "source": {
        "type": "keyword"
      },
      "event_type": {
        "type": "keyword"
      },

      "_comment_feature": "功能與行為資訊",
      "feature_id": {
        "type": "keyword"
      },
      "feature_name": {
        "type": "keyword"
      },
      "feature_type": {
        "type": "keyword"
      },
      "action": {
        "type": "keyword"
      },

      "_comment_context_web": "場景上下文 (Web)",
      "page_url": {
        "type": "keyword",
        "fields": {
          "text": {
            "type": "text"
          }
        }
      },
      "page_name": {
        "type": "keyword"
      },
      "previous_page_url": {
        "type": "keyword"
      },
      "previous_page_name": {
        "type": "keyword"
      },
      "duration_ms": {
        "type": "long"
      },
      "is_active_duration": {
        "type": "boolean"
      },
      "visibility_changes": {
        "type": "integer"
      },

      "_comment_context_app": "場景上下文 (App)",
      "screen_name": {
        "type": "keyword"
      },
      "previous_screen_name": {
        "type": "keyword"
      },

      "_comment_environment": "環境資訊",
      "device_type": {
        "type": "keyword"
      },
      "os": {
        "type": "keyword"
      },
      "os_version": {
        "type": "keyword"
      },
      "browser": {
        "type": "keyword"
      },
      "browser_version": {
        "type": "keyword"
      },
      "app_version": {
        "type": "keyword"
      },
      "build_number": {
        "type": "keyword"
      },
      "network_type": {
        "type": "keyword"
      },
      "locale": {
        "type": "keyword"
      },

      "_comment_experiments": "實驗資訊",
      "experiments": {
        "type": "object",
        "dynamic": true
      },

      "_comment_metadata": "彈性附加資訊",
      "metadata": {
        "type": "object",
        "dynamic": true
      },

      "_comment_system": "系統欄位",
      "received_at": {
        "type": "date",
        "format": "strict_date_optional_time||epoch_millis"
      }
    }
  },
  "settings": {
    "number_of_shards": 3,
    "number_of_replicas": 1,
    "refresh_interval": "5s",
    "index.codec": "best_compression"
  }
}
```

#### 3.2.1 欄位型別說明

| Elasticsearch 型別 | 用途 | 範例欄位 |
|-------------------|------|---------|
| `keyword` | 精確匹配、聚合、排序 | `user_id`, `feature_id`, `source` |
| `text` | 全文搜索（分詞） | `page_url.text` |
| `date` | 時間範圍查詢、排序 | `event_time`, `received_at` |
| `long` | 大數值（整數），適合統計分析 | `duration_ms` |
| `integer` | 一般整數 | `visibility_changes` |
| `boolean` | 布林值 | `is_active_duration` |
| `object` | 巢狀 JSON 物件（扁平化） | `experiments`, `metadata` |

#### 3.2.2 Multi-field 策略

```json
"page_url": {
  "type": "keyword",
  "fields": {
    "text": {
      "type": "text"
    }
  }
}
```

用途：
- `page_url`（keyword）：精確匹配、聚合
- `page_url.text`（text）：全文搜索（例如搜尋 URL 中包含 "dashboard" 的事件）

---

### 3.3 Index Template 設定

**用途**：自動為新建的 `user-events-*` 索引套用 mapping 與 settings。

```json
{
  "index_patterns": ["user-events-*"],
  "template": {
    "settings": {
      "number_of_shards": 3,
      "number_of_replicas": 1,
      "refresh_interval": "5s",
      "index.codec": "best_compression",
      "index.lifecycle.name": "user-events-policy",
      "index.lifecycle.rollover_alias": "user-events-write"
    },
    "mappings": {
      "properties": {
        "event_id": { "type": "keyword" },
        "product_id": { "type": "keyword" },
        "user_id": { "type": "keyword" },
        "anonymous_id": { "type": "keyword" },
        "client_id": { "type": "keyword" },
        "session_id": { "type": "keyword" },
        "event_time": {
          "type": "date",
          "format": "strict_date_optional_time||epoch_millis"
        },
        "source": { "type": "keyword" },
        "event_type": { "type": "keyword" },
        "feature_id": { "type": "keyword" },
        "feature_name": { "type": "keyword" },
        "feature_type": { "type": "keyword" },
        "action": { "type": "keyword" },
        "page_url": {
          "type": "keyword",
          "fields": { "text": { "type": "text" } }
        },
        "page_name": { "type": "keyword" },
        "previous_page_url": { "type": "keyword" },
        "previous_page_name": { "type": "keyword" },
        "duration_ms": { "type": "long" },
        "is_active_duration": { "type": "boolean" },
        "visibility_changes": { "type": "integer" },
        "screen_name": { "type": "keyword" },
        "previous_screen_name": { "type": "keyword" },
        "device_type": { "type": "keyword" },
        "os": { "type": "keyword" },
        "os_version": { "type": "keyword" },
        "browser": { "type": "keyword" },
        "browser_version": { "type": "keyword" },
        "app_version": { "type": "keyword" },
        "build_number": { "type": "keyword" },
        "network_type": { "type": "keyword" },
        "locale": { "type": "keyword" },
        "experiments": {
          "type": "object",
          "dynamic": true
        },
        "metadata": {
          "type": "object",
          "dynamic": true
        },
        "received_at": {
          "type": "date",
          "format": "strict_date_optional_time||epoch_millis"
        }
      }
    },
    "aliases": {
      "user-events-read": {}
    }
  },
  "priority": 500,
  "version": 1
}
```

---

### 3.4 Index Lifecycle Policy

**用途**：自動管理索引的生命週期（輪轉、遷移、刪除）。

```json
{
  "policy": {
    "phases": {
      "hot": {
        "min_age": "0ms",
        "actions": {
          "rollover": {
            "max_primary_shard_size": "50GB",
            "max_age": "1d"
          },
          "set_priority": {
            "priority": 100
          }
        }
      },
      "warm": {
        "min_age": "7d",
        "actions": {
          "shrink": {
            "number_of_shards": 1
          },
          "forcemerge": {
            "max_num_segments": 1
          },
          "set_priority": {
            "priority": 50
          }
        }
      },
      "cold": {
        "min_age": "30d",
        "actions": {
          "searchable_snapshot": {
            "snapshot_repository": "cold-repository"
          },
          "set_priority": {
            "priority": 0
          }
        }
      },
      "delete": {
        "min_age": "90d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}
```

#### 3.4.1 Policy 階段說明

| Phase | 時間 | 動作 | 用途 |
|-------|------|------|------|
| Hot | 0-7天 | Rollover（每日或 50GB）| 活躍寫入與查詢 |
| Warm | 7-30天 | Shrink（縮減分片）、Forcemerge | 減少資源消耗 |
| Cold | 30-90天 | Searchable Snapshot | 低成本儲存 |
| Delete | 90天+ | 刪除索引 | 清理過期數據 |

---

### 3.5 維度資料管理（選配）

由於 Elasticsearch 不支援 JOIN，維度資料可採用以下策略：

#### 3.5.1 策略一：獨立索引 + 應用層 Join

**功能維度索引**：`features`

```json
{
  "mappings": {
    "properties": {
      "feature_id": { "type": "keyword" },
      "name": { "type": "text" },
      "type": { "type": "keyword" },
      "description": { "type": "text" },
      "created_at": { "type": "date" },
      "is_active": { "type": "boolean" }
    }
  }
}
```

**實驗維度索引**：`experiments`

```json
{
  "mappings": {
    "properties": {
      "experiment_key": { "type": "keyword" },
      "name": { "type": "text" },
      "description": { "type": "text" },
      "created_at": { "type": "date" },
      "is_active": { "type": "boolean" }
    }
  }
}
```

#### 3.5.2 策略二：Denormalization（推薦）

在寫入事件時，直接將功能名稱、實驗說明等資訊嵌入事件文檔：

```json
{
  "feature_id": "btn_export_report",
  "feature_name": "export_report_button",
  "feature_type": "button",
  "feature_description": "匯出報表按鈕（在儀表板右上角）"
}
```

優點：
- 查詢效能佳（無需 JOIN）
- 簡化查詢邏輯

缺點：
- 資料冗余
- 維度資訊更新需要重新索引事件（建議維度資訊穩定後再嵌入）

---

## 4. API 與 Elasticsearch 的整體規則與實務建議

### 4.1 時間規則

- `event_time`（Request）與 `received_at`（Elasticsearch Document）：
    - 一律使用 UTC。
    - 格式：ISO 8601（例如 `2025-12-11T10:55:23Z`）。
    - 時區轉換放在報表與分析層處理。

### 4.2 必填欄位策略（API + Elasticsearch）

最低要求（API 必填）：

- `product_id`
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
- Elasticsearch 的 `object` 型別支援動態欄位，可直接對 `experiments.exp_new_onboarding` 進行聚合分析。

### 4.5 metadata 的演進策略

- 初期：大量資訊先進 `metadata`，快速上線。
- 收集一段時間後：
    - 觀察報表最常用的 metadata 欄位（如 `plan_type`, `is_trial_user`）。
    - 逐步拉成頂層欄位（透過 Index Template 更新 mapping），保留原欄位在 metadata 中以相容舊資料。

### 4.6 擴充：批次 API（未來）

目前：`POST /api/v1/track/event`（單筆）

未來如需批量追蹤：

- 新增：`POST /api/v1/track/events/batch`
- Request：`[{...TrackEventRequest}, {...}, ...]`
- Elasticsearch：使用 **Bulk API** 批次寫入，提升吞吐量。

範例：

```json
POST /_bulk
{ "index": { "_index": "user-events-write" } }
{ "event_id": "evt_001", "user_id": "u_123", ... }
{ "index": { "_index": "user-events-write" } }
{ "event_id": "evt_002", "user_id": "u_456", ... }
```

---

### 4.7 常見查詢與聚合範例

#### 4.7.1 功能使用熱度分析

**查詢**：過去 7 天各功能的點擊次數（Top 10）

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "bool": {
      "filter": [
        {
          "range": {
            "event_time": {
              "gte": "now-7d/d",
              "lt": "now/d"
            }
          }
        },
        {
          "term": {
            "event_type": "click"
          }
        }
      ]
    }
  },
  "aggs": {
    "top_features": {
      "terms": {
        "field": "feature_id",
        "size": 10
      }
    }
  }
}
```

---

#### 4.7.2 漏斗分析

**查詢**：某個 session 中的事件序列（依時間排序）

```json
GET /user-events-read/_search
{
  "query": {
    "term": {
      "session_id": "s_20251211_0001"
    }
  },
  "sort": [
    {
      "event_time": {
        "order": "asc"
      }
    }
  ],
  "_source": ["event_time", "feature_id", "page_name"]
}
```

**進階**：計算從「登入頁」→「儀表板」→「匯出報表」的轉換率

使用 **Pipeline Aggregation** 或在應用層處理事件序列。

---

#### 4.7.3 留存分析

**查詢**：2025-12-01 新增用戶，在 12-08 有活動的用戶數（7日留存）

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "bool": {
      "must": [
        {
          "range": {
            "event_time": {
              "gte": "2025-12-08T00:00:00Z",
              "lt": "2025-12-09T00:00:00Z"
            }
          }
        }
      ],
      "filter": [
        {
          "terms": {
            "user_id": [
              "u_001",
              "u_002",
              "u_003"
            ]
          }
        }
      ]
    }
  },
  "aggs": {
    "retained_users": {
      "cardinality": {
        "field": "user_id"
      }
    }
  }
}
```

備註：新增用戶清單需從其他來源（如用戶註冊表）取得，或透過事件時間推算首次活動日期。

---

#### 4.7.4 A/B Test 實驗分析

**查詢**：`exp_new_onboarding` 各 variant 的事件數

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "range": {
      "event_time": {
        "gte": "now-7d/d",
        "lt": "now/d"
      }
    }
  },
  "aggs": {
    "by_variant": {
      "terms": {
        "field": "experiments.exp_new_onboarding"
      },
      "aggs": {
        "feature_clicks": {
          "terms": {
            "field": "feature_id",
            "size": 5
          }
        }
      }
    }
  }
}
```

---

#### 4.7.5 metadata 自訂欄位分析

**查詢**：Pro 方案用戶的功能使用分佈

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "bool": {
      "filter": [
        {
          "term": {
            "metadata.plan_type": "pro"
          }
        },
        {
          "range": {
            "event_time": {
              "gte": "now-30d/d"
            }
          }
        }
      ]
    }
  },
  "aggs": {
    "feature_usage": {
      "terms": {
        "field": "feature_id",
        "size": 20
      }
    }
  }
}
```

---

#### 4.7.6 頁面停留時間分析

**查詢**：過去 7 天各頁面的平均停留時間（Top 10）

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "bool": {
      "filter": [
        {
          "term": {
            "event_type": "page_leave"
          }
        },
        {
          "range": {
            "event_time": {
              "gte": "now-7d/d",
              "lt": "now/d"
            }
          }
        },
        {
          "range": {
            "duration_ms": {
              "gte": 1000,
              "lte": 3600000
            }
          }
        }
      ]
    }
  },
  "aggs": {
    "by_page": {
      "terms": {
        "field": "page_name",
        "size": 10,
        "order": {
          "avg_duration": "desc"
        }
      },
      "aggs": {
        "avg_duration": {
          "avg": {
            "field": "duration_ms"
          }
        },
        "median_duration": {
          "percentiles": {
            "field": "duration_ms",
            "percents": [50]
          }
        },
        "total_views": {
          "value_count": {
            "field": "duration_ms"
          }
        }
      }
    }
  }
}
```

**查詢**：停留時間分佈（Histogram）- 分析用戶在特定頁面的停留時間分佈

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "bool": {
      "filter": [
        {
          "term": {
            "event_type": "page_leave"
          }
        },
        {
          "term": {
            "page_name": "dashboard_overview"
          }
        },
        {
          "range": {
            "event_time": {
              "gte": "now-30d/d"
            }
          }
        }
      ]
    }
  },
  "aggs": {
    "duration_distribution": {
      "histogram": {
        "field": "duration_ms",
        "interval": 30000,
        "min_doc_count": 1
      }
    },
    "stats": {
      "stats": {
        "field": "duration_ms"
      }
    }
  }
}
```

**查詢**：比較有效停留 vs 總停留時間

```json
GET /user-events-read/_search
{
  "size": 0,
  "query": {
    "bool": {
      "filter": [
        {
          "term": {
            "event_type": "page_leave"
          }
        },
        {
          "range": {
            "event_time": {
              "gte": "now-7d/d"
            }
          }
        }
      ]
    }
  },
  "aggs": {
    "active_vs_total": {
      "filters": {
        "filters": {
          "active_duration": {
            "term": {
              "is_active_duration": true
            }
          },
          "total_duration": {
            "match_all": {}
          }
        }
      },
      "aggs": {
        "avg_duration": {
          "avg": {
            "field": "duration_ms"
          }
        }
      }
    }
  }
}
```

---

### 4.8 效能優化建議

#### 4.8.1 寫入效能

- 使用 **Bulk API** 批次寫入（建議每批 500-1000 筆）
- 調整 `refresh_interval`（預設 5s，可視需求調整為 10s 或 30s）
- 適當的 shard 數量（建議單 shard 不超過 50GB）

#### 4.8.2 查詢效能

- 使用 **Filter Context** 而非 Query Context（不計分，可快取）
- 限制聚合的 bucket 數量（避免記憶體溢位）
- 使用 `_source` 過濾，只返回需要的欄位
- 對於時間範圍查詢，使用 `now-7d/d` 這類對齊邊界的語法（利於快取）

#### 4.8.3 儲存優化

- 使用 `best_compression` codec（節省 20-30% 儲存空間）
- 定期執行 `forcemerge`（Warm Phase）
- 使用 ILM 自動移至冷儲存（Searchable Snapshot）

---

### 4.9 與現有系統整合

#### 4.9.1 ASP.NET Core 整合

使用 **Elastic.Clients.Elasticsearch** 套件：

```bash
dotnet add package Elastic.Clients.Elasticsearch
```

範例程式碼片段：

```csharp
var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("user-events-write");

var client = new ElasticsearchClient(settings);

// 寫入事件
var response = await client.IndexAsync(eventDocument, idx => idx
    .Index("user-events-write"));
```

#### 4.9.2 資料遷移策略（PostgreSQL → Elasticsearch）

若從 PostgreSQL 遷移：

1. **雙寫階段**：同時寫入 PostgreSQL 與 Elasticsearch
2. **歷史資料遷移**：使用 Logstash 或自訂 ETL 工具
3. **驗證階段**：比對兩邊資料一致性
4. **切換階段**：將查詢流量切至 Elasticsearch
5. **淘汰階段**：停止寫入 PostgreSQL（保留歷史資料或歸檔）

#### 4.9.3 前端頁面停留時間追蹤實作

**使用 Page Visibility API 追蹤有效停留時間**

```javascript
class PageDurationTracker {
  constructor() {
    this.pageEnterTime = Date.now();
    this.activeTime = 0;
    this.lastVisibleTime = Date.now();
    this.visibilityChanges = 0;
    this.isTracking = false;

    this.init();
  }

  init() {
    // 監聽頁面可見性變化
    document.addEventListener('visibilitychange', () => {
      this.handleVisibilityChange();
    });

    // 頁面離開時發送事件
    window.addEventListener('beforeunload', () => {
      this.sendPageLeaveEvent();
    });

    // SPA 路由變化時（以 React Router 為例）
    // 需要在路由變化時手動呼叫 sendPageLeaveEvent()

    this.isTracking = true;
  }

  handleVisibilityChange() {
    if (document.hidden) {
      // 頁面隱藏時，累加活躍時間
      this.activeTime += Date.now() - this.lastVisibleTime;
    } else {
      // 頁面可見時，記錄時間並增加變化次數
      this.lastVisibleTime = Date.now();
      this.visibilityChanges++;
    }
  }

  sendPageLeaveEvent() {
    if (!this.isTracking) return;

    // 計算最後一段活躍時間
    if (!document.hidden) {
      this.activeTime += Date.now() - this.lastVisibleTime;
    }

    const durationMs = Date.now() - this.pageEnterTime;

    // 過濾異常值：小於 1 秒或大於 1 小時
    if (durationMs < 1000 || durationMs > 3600000) {
      return;
    }

    const eventData = {
      client_id: this.getClientId(),
      session_id: this.getSessionId(),
      event_time: new Date().toISOString(),
      source: "web",
      event_type: "page_leave",

      page_url: window.location.href,
      page_name: this.getPageName(),

      duration_ms: durationMs,
      is_active_duration: true,
      visibility_changes: this.visibilityChanges,

      // ... 其他必填欄位
    };

    // 使用 sendBeacon 確保在頁面關閉時也能送出
    const blob = new Blob([JSON.stringify(eventData)], {
      type: 'application/json'
    });

    navigator.sendBeacon('/api/v1/track/event', blob);

    this.isTracking = false;
  }

  getClientId() {
    // 從 cookie 或 localStorage 取得
    return localStorage.getItem('client_id') || this.generateClientId();
  }

  getSessionId() {
    // 從 sessionStorage 取得
    const sessionIdKey = 'session_id';
    const lastSeenKey = 'session_last_seen_at';
    const idleTimeoutMs = 30 * 60 * 1000; // 30 分鐘
    const now = Date.now();

    const sessionId = sessionStorage.getItem(sessionIdKey);
    const lastSeen = Number(sessionStorage.getItem(lastSeenKey) || '0');

    if (!sessionId || !lastSeen || (now - lastSeen) > idleTimeoutMs) {
      return this.generateSessionId();
    }

    sessionStorage.setItem(lastSeenKey, String(now));
    return sessionId;
  }

  getPageName() {
    // 根據路由或頁面標題決定
    return document.querySelector('[data-page-name]')?.dataset.pageName
      || window.location.pathname.replace(/\//g, '_');
  }

  generateClientId() {
    const id = 'c_' + Math.random().toString(36).substr(2, 9);
    localStorage.setItem('client_id', id);
    return id;
  }

  generateSessionId() {
    const sessionIdKey = 'session_id';
    const lastSeenKey = 'session_last_seen_at';

    const uuid = (globalThis.crypto && typeof globalThis.crypto.randomUUID === 'function')
      ? globalThis.crypto.randomUUID()
      : (Date.now().toString(36) + '_' + Math.random().toString(36).slice(2, 10));

    const id = 's_' + uuid;
    sessionStorage.setItem(sessionIdKey, id);
    sessionStorage.setItem(lastSeenKey, String(Date.now()));
    return id;
  }
}

// 初始化追蹤器
const tracker = new PageDurationTracker();
```

**SPA 應用整合範例（React）**

```javascript
import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

function usePageTracking() {
  const location = useLocation();
  const trackerRef = useRef(null);

  useEffect(() => {
    // 路由變化時，先發送上一頁的 page_leave 事件
    if (trackerRef.current) {
      trackerRef.current.sendPageLeaveEvent();
    }

    // 初始化新頁面的追蹤器
    trackerRef.current = new PageDurationTracker();

    // 發送 page_view 事件
    sendPageViewEvent({
      page_url: window.location.href,
      page_name: location.pathname,
      // ... 其他欄位
    });

    return () => {
      // 清理
      if (trackerRef.current) {
        trackerRef.current.sendPageLeaveEvent();
      }
    };
  }, [location]);
}

// 在 App 組件中使用
function App() {
  usePageTracking();

  return (
    // ... your app
  );
}
```

**注意事項**

1. **精確度問題**：
   - `beforeunload` 事件在某些情況下可能不會觸發（例如強制關閉瀏覽器）
   - 使用 `navigator.sendBeacon()` 提高資料發送成功率
   - SPA 需要監聽路由變化，手動觸發 page_leave 事件

2. **異常值處理**：
   - 過短停留（< 1 秒）：可能是誤點或跳出
   - 過長停留（> 1 小時）：可能是忘記關閉分頁
   - 建議在前端先過濾，在後端分析時再次過濾

3. **效能考量**：
   - 不要在每次可見性變化時都發送 API 請求
   - 只在 page_leave 時統一發送
   - 使用 `sendBeacon` 避免阻塞頁面關閉

4. **隱私考量**：
   - 遵守 GDPR 等隱私法規
   - 提供使用者選擇退出追蹤的機制
   - 不追蹤敏感頁面（如支付頁面）的詳細停留時間

#### 4.9.4 前端追蹤 SDK 完整實作

**設計原則**

1. **一次初始化，到處使用**：單例模式，全域可用
2. **自動處理身份識別**：client_id、session_id 自動管理
3. **簡潔的 API**：提供語意化的方法名稱
4. **支援 TypeScript**：完整的型別定義
5. **錯誤處理與重試機制**：使用 sendBeacon 確保資料送達

**核心 Tracker SDK**

```typescript
// tracker.ts
interface TrackEventPayload {
  // 身份識別（自動填入）
  user_id?: string;
  anonymous_id?: string;
  client_id: string;
  session_id: string;

  // 事件核心
  event_time: string;
  source: string;
  event_type: string;

  // 功能資訊
  feature_id?: string;
  feature_name?: string;
  feature_type?: string;
  action?: string;

  // 場景上下文
  page_url?: string;
  page_name?: string;
  previous_page_url?: string;
  previous_page_name?: string;
  duration_ms?: number;
  is_active_duration?: boolean;
  visibility_changes?: number;

  // 環境資訊（自動填入）
  device_type?: string;
  os?: string;
  os_version?: string;
  browser?: string;
  browser_version?: string;
  network_type?: string;
  locale?: string;

  // 實驗與 metadata
  experiments?: Record<string, string>;
  metadata?: Record<string, any>;
}

interface TrackerConfig {
  apiEndpoint?: string;
  userId?: string;
  debug?: boolean;
  autoTrackPageView?: boolean;
  autoTrackPageLeave?: boolean;
}

class EventTracker {
  private config: TrackerConfig;
  private clientId: string;
  private sessionId: string;
  private previousPageUrl: string | null = null;
  private previousPageName: string | null = null;
  private pageEnterTime: number = 0;
  private activeTime: number = 0;
  private lastVisibleTime: number = 0;
  private visibilityChanges: number = 0;
  private isTracking: boolean = false;

  constructor(config: TrackerConfig = {}) {
    this.config = {
      apiEndpoint: '/api/v1/track/event',
      debug: false,
      autoTrackPageView: true,
      autoTrackPageLeave: true,
      ...config,
    };

    this.clientId = this.getOrCreateClientId();
    this.sessionId = this.getOrCreateSessionId();

    if (this.config.autoTrackPageView) {
      this.trackPageView();
    }

    if (this.config.autoTrackPageLeave) {
      this.initPageLeaveTracking();
    }
  }

  // ==================== 公開 API ====================

  /**
   * 追蹤功能點擊
   */
  trackClick(featureId: string, options: {
    featureName?: string;
    featureType?: string;
    metadata?: Record<string, any>;
  } = {}) {
    return this.track({
      event_type: 'click',
      feature_id: featureId,
      feature_name: options.featureName,
      feature_type: options.featureType || 'button',
      action: 'click',
      metadata: options.metadata,
    });
  }

  /**
   * 追蹤頁面瀏覽
   */
  trackPageView(pageName?: string) {
    const currentUrl = window.location.href;
    const currentPageName = pageName || this.getPageName();

    this.track({
      event_type: 'page_view',
      page_url: currentUrl,
      page_name: currentPageName,
      previous_page_url: this.previousPageUrl,
      previous_page_name: this.previousPageName,
    });

    this.previousPageUrl = currentUrl;
    this.previousPageName = currentPageName;
    this.pageEnterTime = Date.now();
    this.activeTime = 0;
    this.lastVisibleTime = Date.now();
    this.visibilityChanges = 0;
  }

  /**
   * 追蹤自訂事件
   */
  track(event: Partial<TrackEventPayload>) {
    const payload = this.buildPayload(event);

    if (this.config.debug) {
      console.log('[Tracker] Event:', payload);
    }

    return this.send(payload);
  }

  /**
   * 設定使用者 ID（登入後呼叫）
   */
  setUserId(userId: string) {
    this.config.userId = userId;
  }

  /**
   * 設定實驗分組
   */
  setExperiments(experiments: Record<string, string>) {
    sessionStorage.setItem('experiments', JSON.stringify(experiments));
  }

  // ==================== 私有方法 ====================

  private buildPayload(event: Partial<TrackEventPayload>): TrackEventPayload {
    const experiments = this.getExperiments();
    const envInfo = this.getEnvironmentInfo();

    return {
      user_id: this.config.userId,
      client_id: this.clientId,
      session_id: this.sessionId,
      event_time: new Date().toISOString(),
      source: 'web',
      ...envInfo,
      experiments: experiments || undefined,
      page_url: event.page_url || window.location.href,
      page_name: event.page_name || this.getPageName(),
      ...event,
    } as TrackEventPayload;
  }

  private send(payload: TrackEventPayload): Promise<void> {
    if (navigator.sendBeacon) {
      const blob = new Blob([JSON.stringify(payload)], {
        type: 'application/json',
      });
      const success = navigator.sendBeacon(this.config.apiEndpoint!, blob);

      if (success) {
        return Promise.resolve();
      }
    }

    return fetch(this.config.apiEndpoint!, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
      keepalive: true,
    })
      .then(response => {
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }
      })
      .catch(error => {
        if (this.config.debug) {
          console.error('[Tracker] Failed to send event:', error);
        }
      });
  }

  private initPageLeaveTracking() {
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) {
        this.activeTime += Date.now() - this.lastVisibleTime;
      } else {
        this.lastVisibleTime = Date.now();
        this.visibilityChanges++;
      }
    });

    window.addEventListener('beforeunload', () => {
      this.trackPageLeave();
    });

    this.isTracking = true;
  }

  private trackPageLeave() {
    if (!this.isTracking || this.pageEnterTime === 0) return;

    if (!document.hidden) {
      this.activeTime += Date.now() - this.lastVisibleTime;
    }

    const durationMs = Date.now() - this.pageEnterTime;

    if (durationMs < 1000 || durationMs > 3600000) {
      return;
    }

    this.track({
      event_type: 'page_leave',
      duration_ms: durationMs,
      is_active_duration: true,
      visibility_changes: this.visibilityChanges,
    });
  }

  // ==================== 輔助方法 ====================

  private getOrCreateClientId(): string {
    let clientId = localStorage.getItem('_tracker_client_id');
    if (!clientId) {
      clientId = 'c_' + this.generateId();
      localStorage.setItem('_tracker_client_id', clientId);
    }
    return clientId;
  }

  private getOrCreateSessionId(): string {
    const sessionIdKey = '_tracker_session_id';
    const lastSeenKey = '_tracker_session_last_seen_at';
    const idleTimeoutMs = 30 * 60 * 1000; // 30 分鐘

    const now = Date.now();
    const lastSeen = Number(sessionStorage.getItem(lastSeenKey) || '0');
    let sessionId = sessionStorage.getItem(sessionIdKey);

    if (!sessionId || !lastSeen || (now - lastSeen) > idleTimeoutMs) {
      sessionId = 's_' + this.generateId();
      sessionStorage.setItem(sessionIdKey, sessionId);
    }

    sessionStorage.setItem(lastSeenKey, String(now));
    return sessionId;
  }

  private generateId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }
    return Math.random().toString(36).substr(2, 9);
  }

  private getPageName(): string {
    const pageNameEl = document.querySelector('[data-page-name]');
    if (pageNameEl) {
      return (pageNameEl as HTMLElement).dataset.pageName || '';
    }
    return window.location.pathname.replace(/\//g, '_').replace(/^_/, '') || 'home';
  }

  private getExperiments(): Record<string, string> | null {
    const stored = sessionStorage.getItem('experiments');
    return stored ? JSON.parse(stored) : null;
  }

  private getEnvironmentInfo() {
    const ua = navigator.userAgent;

    return {
      device_type: this.getDeviceType(),
      browser: this.getBrowser(ua),
      browser_version: this.getBrowserVersion(ua),
      os: this.getOS(ua),
      locale: navigator.language,
      network_type: this.getNetworkType(),
    };
  }

  private getDeviceType(): string {
    const ua = navigator.userAgent;
    if (/(tablet|ipad|playbook|silk)|(android(?!.*mobi))/i.test(ua)) {
      return 'tablet';
    }
    if (/Mobile|Android|iP(hone|od)|IEMobile|BlackBerry|Kindle|Silk-Accelerated|(hpw|web)OS|Opera M(obi|ini)/.test(ua)) {
      return 'mobile';
    }
    return 'desktop';
  }

  private getBrowser(ua: string): string {
    if (ua.includes('Chrome')) return 'Chrome';
    if (ua.includes('Safari')) return 'Safari';
    if (ua.includes('Firefox')) return 'Firefox';
    if (ua.includes('Edge')) return 'Edge';
    return 'Unknown';
  }

  private getBrowserVersion(ua: string): string {
    const match = ua.match(/(Chrome|Safari|Firefox|Edge)\/(\d+\.\d+)/);
    return match ? match[2] : 'Unknown';
  }

  private getOS(ua: string): string {
    if (ua.includes('Windows')) return 'Windows';
    if (ua.includes('Mac')) return 'macOS';
    if (ua.includes('Linux')) return 'Linux';
    if (ua.includes('Android')) return 'Android';
    if (ua.includes('iOS') || ua.includes('iPhone') || ua.includes('iPad')) return 'iOS';
    return 'Unknown';
  }

  private getNetworkType(): string {
    const connection = (navigator as any).connection
      || (navigator as any).mozConnection
      || (navigator as any).webkitConnection;

    return connection?.effectiveType || 'unknown';
  }
}

// 匯出單例
export const tracker = new EventTracker();
export default EventTracker;
```

**使用範例**

**1. 基本使用（Vanilla JS）**

```javascript
// main.js
import { tracker } from './tracker';

// 追蹤按鈕點擊
document.getElementById('exportBtn').addEventListener('click', () => {
  tracker.trackClick('btn_export_report', {
    featureName: 'export_report_button',
    featureType: 'button',
    metadata: {
      button_text: '匯出報表',
      position: 'top_right',
    },
  });
});

// 登入後設定使用者 ID
function onUserLogin(userId) {
  tracker.setUserId(userId);
}

// 設定 A/B 測試分組
tracker.setExperiments({
  exp_new_onboarding: 'variant_B',
  exp_pricing_layout: 'control',
});
```

**2. React 整合**

```typescript
// useTracker.ts
import { useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { tracker } from './tracker';

export function usePageTracking() {
  const location = useLocation();
  const prevLocationRef = useRef(location.pathname);

  useEffect(() => {
    if (prevLocationRef.current !== location.pathname) {
      tracker.trackPageView();
      prevLocationRef.current = location.pathname;
    }
  }, [location]);
}

// TrackingProvider.tsx
import { ReactNode } from 'react';
import { usePageTracking } from './useTracker';

export function TrackingProvider({ children }: { children: ReactNode }) {
  usePageTracking();
  return <>{children}</>;
}

// App.tsx
function App() {
  return (
    <TrackingProvider>
      {/* 你的應用 */}
    </TrackingProvider>
  );
}

// 組件中使用
function Dashboard() {
  const handleExport = () => {
    tracker.trackClick('btn_export_report', {
      metadata: { report_type: 'monthly', format: 'pdf' },
    });
    exportReport();
  };

  return (
    <div data-page-name="dashboard_overview">
      <button onClick={handleExport}>匯出報表</button>
    </div>
  );
}
```

**3. Vue 整合**

```typescript
// plugins/tracker.ts
import { App } from 'vue';
import { tracker } from './tracker';

export default {
  install(app: App) {
    app.config.globalProperties.$tracker = tracker;

    const router = app.config.globalProperties.$router;
    if (router) {
      router.afterEach(() => {
        tracker.trackPageView();
      });
    }
  },
};

// main.ts
import { createApp } from 'vue';
import trackerPlugin from './plugins/tracker';

const app = createApp(App);
app.use(trackerPlugin);

// 組件中使用
export default {
  methods: {
    handleClick() {
      this.$tracker.trackClick('btn_export', {
        metadata: { position: 'header' }
      });
    }
  }
}
```

**4. HTML data-* 屬性自動追蹤（選配）**

```javascript
// auto-track.js
import { tracker } from './tracker';

document.addEventListener('click', (e) => {
  const target = e.target.closest('[data-track]');
  if (!target) return;

  const featureId = target.dataset.track;
  const featureName = target.dataset.trackName;
  const featureType = target.dataset.trackType || 'button';

  tracker.trackClick(featureId, {
    featureName,
    featureType,
    metadata: {
      text: target.textContent?.trim(),
    },
  });
});
```

```html
<!-- HTML 使用 -->
<button
  data-track="btn_export_report"
  data-track-name="export_report_button"
  data-track-type="button"
>
  匯出報表
</button>
```

**SDK 優勢**

1. **簡單易用**：一行代碼追蹤事件
2. **自動處理**：身份識別、環境資訊自動填入
3. **TypeScript 支援**：完整的型別定義
4. **框架無關**：可用於任何前端框架
5. **可靠性**：使用 `sendBeacon` 確保資料送達
6. **除錯友善**：debug 模式可查看所有事件

**初始化配置選項**

```typescript
const tracker = new EventTracker({
  apiEndpoint: 'https://api.example.com/api/v1/track/event', // API 端點
  userId: 'u_123456',                // 使用者 ID（可選）
  debug: true,                        // 除錯模式
  autoTrackPageView: true,            // 自動追蹤頁面瀏覽
  autoTrackPageLeave: true,           // 自動追蹤頁面離開
});
```

---

若你接下來需要，我可以基於這份規格，再幫你產出：

- 對應的 **OpenAPI/Swagger YAML** 範本
- ASP.NET Core Controller + Elasticsearch Client 的示範程式碼
- Kibana Dashboard 設定範例（視覺化分析儀表板）
- Docker Compose 本地開發環境配置