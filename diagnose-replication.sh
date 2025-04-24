#!/bin/bash
set -e

echo "Running PostgreSQL replication diagnostics..."

# Check master PostgreSQL logs
echo "=== Master PostgreSQL logs ==="
docker exec diem-ecommerce-master-postgres bash -c 'tail -n 50 /var/lib/postgresql/data/log/*.log 2>/dev/null || echo "No log files found"'

# Check slave PostgreSQL logs
echo -e "\n=== Slave PostgreSQL logs ==="
docker exec diem-ecommerce-slave-postgres bash -c 'tail -n 50 /var/lib/postgresql/data/log/*.log 2>/dev/null || echo "No log files found"'

# Check master replication settings
echo -e "\n=== Master replication settings ==="
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT name, setting FROM pg_settings WHERE name IN ('wal_level', 'max_wal_senders', 'max_replication_slots', 'hot_standby');"

# Check slave replication settings
echo -e "\n=== Slave replication settings ==="
docker exec diem-ecommerce-slave-postgres psql -U postgres -c "SELECT name, setting FROM pg_settings WHERE name IN ('hot_standby', 'primary_conninfo', 'primary_slot_name');"

# Check replication slots
echo -e "\n=== Replication slots on master ==="
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT * FROM pg_replication_slots;"

# Check WAL senders on master
echo -e "\n=== WAL senders on master ==="
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT * FROM pg_stat_replication;"

# Check pg_hba.conf on master
echo -e "\n=== Master pg_hba.conf ==="
docker exec diem-ecommerce-master-postgres bash -c 'grep -v "^#" /var/lib/postgresql/data/pg_hba.conf | grep -v "^$"'

# Check network connectivity
echo -e "\n=== Testing network connectivity from slave to master ==="
docker exec diem-ecommerce-slave-postgres bash -c 'ping -c 3 diem-ecommerce-master-postgres'

# Fix common issues
echo -e "\n=== Applying common fixes ==="

# 1. Fix slave's postgresql.auto.conf to make sure primary_conninfo is correct
echo "Updating slave's primary connection info..."
docker exec -i diem-ecommerce-slave-postgres bash << EOF
cat > /var/lib/postgresql/data/postgresql.auto.conf << INNEREOF
# Replication settings added by diagnostic script
primary_conninfo = 'host=diem-ecommerce-master-postgres port=5432 user=postgres application_name=slave1'
primary_slot_name = 'replication_slot_slave1'
INNEREOF

chown postgres:postgres /var/lib/postgresql/data/postgresql.auto.conf
chmod 600 /var/lib/postgresql/data/postgresql.auto.conf
EOF

# 2. Create recovery.signal file to trigger recovery mode
echo "Creating recovery.signal file on slave..."
docker exec diem-ecommerce-slave-postgres bash -c 'touch /var/lib/postgresql/data/recovery.signal && chown postgres:postgres /var/lib/postgresql/data/recovery.signal'

# Restart slave to apply changes
echo "Restarting slave PostgreSQL..."
docker-compose restart diem-ecommerce-slave-postgres

echo "Waiting for slave to restart..."
sleep 10

# Check if replication is now working
echo -e "\n=== Checking replication status after fixes ==="
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT * FROM pg_stat_replication;"

echo "Diagnostics complete. If replication is still not working, check the logs for more detailed information."