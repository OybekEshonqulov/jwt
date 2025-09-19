#!/bin/bash
# Log generator for Docker container

LOG_DIR="/app/logs"
mkdir -p "$LOG_DIR"

FILENAME="jwt_logs_$(date +'%Y%m%d_%H%M').txt"
FULL_PATH="$LOG_DIR/$FILENAME"

TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
CONTAINER_ID=$(hostname)

LOG_MESSAGE="[${TIMESTAMP}] JWT API CONTAINER STATUS:
- Container ID: ${CONTAINER_ID}
- Application: JWT Token API
- Status: ✅ Running
- Port: 5257
- Environment: ${ASPNETCORE_ENVIRONMENT:-Development}
- Check Time: ${TIMESTAMP}
----------------------------------------"

echo "$LOG_MESSAGE" >> "$FULL_PATH"
echo "Log created: $FULL_PATH"
