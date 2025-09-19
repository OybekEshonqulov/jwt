#!/bin/bash
# Cron setup for Docker container

echo "🔧 Setting up cron jobs for JWT API Container..."

# Log papkasini yaratish
mkdir -p /app/logs

# Crontab faylini yaratish
CRON_FILE="/tmp/crontab"

echo "# JWT API Container Cron Jobs" > "$CRON_FILE"
echo "*/10 * * * * /bin/bash /app/Scripts/log_generator.sh >> /app/logs/cron_setup.log 2>&1" >> "$CRON_FILE"
echo "0 2 * * * find /app/logs -name \"*.txt\" -mtime +1 -delete" >> "$CRON_FILE"

# Crontab ni o'rnatish
crontab "$CRON_FILE"
rm -f "$CRON_FILE"

echo "✅ Cron jobs installed successfully!"
crontab -l
