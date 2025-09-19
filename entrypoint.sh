#!/bin/bash
# entrypoint.sh

# Cron service ni ishga tushirish
service cron start

# Cron joblarni sozlash
/app/CronJobs/setup_cron.sh

# Dasturni ishga tushirish
exec dotnet /app/jwtDocker.dll
