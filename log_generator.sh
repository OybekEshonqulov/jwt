#!/bin/bash
# log_generator.sh

# Log papkasini aniqlaymiz
LOG_DIR="/var/logs/myapp"
mkdir -p $LOG_DIR

# Fayl nomini yaratamiz (sana va soat bilan)
FILENAME="logs_$(date +'%Y%m%d_%H%M').txt"
FULL_PATH="$LOG_DIR/$FILENAME"

# Log xabarini yaratamiz
LOG_MESSAGE="System check at $(date '+%Y-%m-%d %H:%M:%S') - Status: OK"

# Faylga yozamiz
echo "$LOG_MESSAGE" >> "$FULL_PATH"

# .NET API ga ham yuborish (optional)
curl -X POST http://localhost:5000/api/logs/write \
  -H "Content-Type: application/json" \
  -d "{\"message\":\"$LOG_MESSAGE\", \"level\":\"INFO\"}" \
  --silent

echo "Log created: $FULL_PATH"
