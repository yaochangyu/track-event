#!/bin/bash

# Elasticsearch 初始化腳本
# 用途：建立 Index Template、ILM Policy 和初始索引

ELASTICSEARCH_URL="${ELASTICSEARCH_URL:-http://localhost:9200}"

echo "🔍 檢查 Elasticsearch 連線..."
if ! curl -s "$ELASTICSEARCH_URL" > /dev/null; then
    echo "❌ 無法連線到 Elasticsearch ($ELASTICSEARCH_URL)"
    echo "請確保 Elasticsearch 正在運行"
    exit 1
fi

echo "✅ Elasticsearch 連線成功"

# 1. 建立 ILM Policy
echo ""
echo "📋 建立 ILM Policy: user-events-policy"
curl -X PUT "$ELASTICSEARCH_URL/_ilm/policy/user-events-policy" \
  -H 'Content-Type: application/json' \
  -d '{
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
      "delete": {
        "min_age": "90d",
        "actions": {
          "delete": {}
        }
      }
    }
  }
}'

# 2. 建立 Index Template
echo ""
echo "📋 建立 Index Template: user-events"
curl -X PUT "$ELASTICSEARCH_URL/_index_template/user-events" \
  -H 'Content-Type: application/json' \
  -d '{
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
        "eventId": { "type": "keyword" },
        "userId": { "type": "keyword" },
        "anonymousId": { "type": "keyword" },
        "clientId": { "type": "keyword" },
        "sessionId": { "type": "keyword" },
        "eventTime": {
          "type": "date",
          "format": "strict_date_optional_time||epoch_millis"
        },
        "source": { "type": "keyword" },
        "eventType": { "type": "keyword" },
        "featureId": { "type": "keyword" },
        "featureName": { "type": "keyword" },
        "featureType": { "type": "keyword" },
        "action": { "type": "keyword" },
        "pageUrl": {
          "type": "keyword",
          "fields": { "text": { "type": "text" } }
        },
        "pageName": { "type": "keyword" },
        "previousPageUrl": { "type": "keyword" },
        "previousPageName": { "type": "keyword" },
        "screenName": { "type": "keyword" },
        "previousScreenName": { "type": "keyword" },
        "deviceType": { "type": "keyword" },
        "os": { "type": "keyword" },
        "osVersion": { "type": "keyword" },
        "browser": { "type": "keyword" },
        "browserVersion": { "type": "keyword" },
        "appVersion": { "type": "keyword" },
        "buildNumber": { "type": "keyword" },
        "networkType": { "type": "keyword" },
        "locale": { "type": "keyword" },
        "experiments": {
          "type": "object",
          "dynamic": true
        },
        "metadata": {
          "type": "object",
          "dynamic": true
        },
        "receivedAt": {
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
}'

# 3. 建立初始索引
CURRENT_DATE=$(date +%Y.%m.%d)
INDEX_NAME="user-events-${CURRENT_DATE}"

echo ""
echo "📋 建立初始索引: $INDEX_NAME"
curl -X PUT "$ELASTICSEARCH_URL/$INDEX_NAME" \
  -H 'Content-Type: application/json' \
  -d '{
  "aliases": {
    "user-events-write": {
      "is_write_index": true
    }
  }
}'

echo ""
echo ""
echo "✅ Elasticsearch 初始化完成！"
echo ""
echo "📊 索引資訊："
echo "  - Index Template: user-events"
echo "  - ILM Policy: user-events-policy"
echo "  - 當前索引: $INDEX_NAME"
echo "  - 寫入別名: user-events-write"
echo "  - 讀取別名: user-events-read"
echo ""
echo "🌐 Kibana 管理介面: http://localhost:5601"
