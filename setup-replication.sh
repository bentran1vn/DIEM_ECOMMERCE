#!/bin/bash
set -e

echo "Setting up PostgreSQL replication..."

# Create directory structure
mkdir -p postgresql/master
mkdir -p postgresql/slave
mkdir -p postgresql/master/archive

# Create configuration files (these will be mounted in the containers)
# Files contents should be based on the artifacts you've created

# Wait for master PostgreSQL to be ready
until docker exec diem-ecommerce-master-postgres pg_isready -h localhost -U postgres; do
  echo "Waiting for master PostgreSQL to be ready..."
  sleep 2
done

echo "Master PostgreSQL is ready"

# Create replication user and slot on master
docker exec diem-ecommerce-master-postgres psql -U postgres -c "CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD '${DB_PASSWORD}';"
docker exec diem-ecommerce-master-postgres psql -U postgres -c "CREATE REPLICATION SLOT replication_slot_slave1 PERMANENT;"

# Create archive directory on master
docker exec diem-ecommerce-master-postgres mkdir -p /var/lib/postgresql/data/archive

# Take a base backup for slave
echo "Taking base backup for slave..."
docker exec diem-ecommerce-slave-postgres bash -c 'rm -rf /var/lib/postgresql/data/*'
docker exec diem-ecommerce-slave-postgres bash -c 'pg_basebackup -h diem-ecommerce-master-postgres -U postgres -D /var/lib/postgresql/data -Fp -Xs -P -R'

# Create recovery.signal file in slave to start in recovery mode
docker exec diem-ecommerce-slave-postgres touch /var/lib/postgresql/data/recovery.signal

# Restart slave to start replication
docker-compose restart diem-ecommerce-slave-postgres

echo "Waiting for slave PostgreSQL to be ready..."
sleep 10

# Check replication status
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT * FROM pg_stat_replication;"

echo "PostgreSQL replication setup complete"