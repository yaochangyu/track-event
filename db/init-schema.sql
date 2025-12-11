-- 使用者事件追蹤資料庫 Schema
-- 根據 CLAUDE.md 設計規格書建立

-- 建立資料庫（如果不存在）
-- CREATE DATABASE track_event;

-- 連接到資料庫
-- \c track_event

-- 建立主事件表：user_events
CREATE TABLE IF NOT EXISTS user_events (
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

-- 建立索引
-- 時間排序 / 篩選
CREATE INDEX IF NOT EXISTS idx_user_events_event_time
    ON user_events (event_time DESC);

-- 功能使用分析：某功能在一段時間的使用
CREATE INDEX IF NOT EXISTS idx_user_events_feature_time
    ON user_events (feature_id, event_time DESC);

-- 用戶留存與行為路徑
CREATE INDEX IF NOT EXISTS idx_user_events_user_time
    ON user_events (user_id, event_time DESC);

-- 裝置與 Session 分析
CREATE INDEX IF NOT EXISTS idx_user_events_client_session_time
    ON user_events (client_id, session_id, event_time DESC);

-- 來源平台過濾
CREATE INDEX IF NOT EXISTS idx_user_events_source_time
    ON user_events (source, event_time DESC);

-- 實驗分群分析範例（依需求建立）
-- CREATE INDEX idx_user_events_exp_new_onboarding
--     ON user_events ((experiments->>'exp_new_onboarding'));

-- metadata 中常用欄位範例（依需求建立）
-- CREATE INDEX idx_user_events_metadata_plan_type
--     ON user_events ((metadata->>'plan_type'));

-- 選配：功能維度表
CREATE TABLE IF NOT EXISTS features (
    feature_id      VARCHAR(128) PRIMARY KEY,
    name            VARCHAR(256),
    type            VARCHAR(64),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);

-- 選配：實驗維度表
CREATE TABLE IF NOT EXISTS experiments (
    experiment_key  VARCHAR(128) PRIMARY KEY,
    name            VARCHAR(256),
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE
);

-- 建立註解
COMMENT ON TABLE user_events IS '使用者行為事件追蹤表 (append-only)';
COMMENT ON COLUMN user_events.event_id IS '事件唯一識別碼';
COMMENT ON COLUMN user_events.user_id IS '已登入用戶 ID（匿名事件可為 NULL）';
COMMENT ON COLUMN user_events.client_id IS '裝置或瀏覽器實體 ID（必填）';
COMMENT ON COLUMN user_events.session_id IS '一次連續使用行為的 Session ID（必填）';
COMMENT ON COLUMN user_events.feature_id IS '功能唯一 ID（必填）';
COMMENT ON COLUMN user_events.experiments IS '實驗資訊 JSONB (key-value)';
COMMENT ON COLUMN user_events.metadata IS '彈性附加資訊 JSONB';
COMMENT ON COLUMN user_events.received_at IS '後端接收並寫入時間（UTC）';
