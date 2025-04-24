#!/bin/bash
set -e

echo "Setting up PostgreSQL replication..."

# Create directory structure
mkdir -p postgresql/master
mkdir -p postgresql/slave
mkdir -p postgresql/master/archive

# Create master PostgreSQL configuration
cat > postgresql/master/postgresql.conf << EOF
# Basic PostgreSQL configuration for master
listen_addresses = '*'
max_connections = 100
shared_buffers = 128MB
dynamic_shared_memory_type = posix
max_wal_size = 1GB
min_wal_size = 80MB

# Replication settings - master
wal_level = replica
max_wal_senders = 10
wal_keep_size = 1GB
max_replication_slots = 10
synchronous_commit = on
archive_mode = on
archive_command = 'test ! -f /var/lib/postgresql/data/archive/%f && cp %p /var/lib/postgresql/data/archive/%f'
hot_standby = on
EOF

# Create master pg_hba.conf with proper replication permissions
cat > postgresql/master/pg_hba.conf << EOF
# TYPE  DATABASE        USER            ADDRESS                 METHOD

# "local" is for Unix domain socket connections only
local   all             all                                     trust
# IPv4 local connections:
host    all             all             127.0.0.1/32            trust
# IPv6 local connections:
host    all             all             ::1/128                 trust

# Allow replication connections from localhost, by a user with the replication privilege
local   replication     all                                     trust
host    replication     all             127.0.0.1/32            trust
host    replication     all             ::1/128                 trust

# Allow replication connections from the slave - using IP range to cover all Docker IPs
host    replication     postgres        0.0.0.0/0               trust
host    replication     replicator      0.0.0.0/0               trust

# Allow all connections from the Docker network
host    all             all             0.0.0.0/0               trust
EOF

# Create slave PostgreSQL configuration
cat > postgresql/slave/postgresql.conf << EOF
# Basic PostgreSQL configuration for slave
listen_addresses = '*'
max_connections = 100
shared_buffers = 128MB
dynamic_shared_memory_type = posix
max_wal_size = 1GB
min_wal_size = 80MB

# Replication settings - slave
wal_level = replica
max_wal_senders = 10
wal_keep_size = 1GB
max_replication_slots = 10
hot_standby = on
hot_standby_feedback = on
primary_conninfo = 'host=diem-ecommerce-master-postgres port=5432 user=postgres application_name=slave1'
primary_slot_name = 'replication_slot_slave1'
EOF

# Create slave pg_hba.conf
cat > postgresql/slave/pg_hba.conf << EOF
# TYPE  DATABASE        USER            ADDRESS                 METHOD

# "local" is for Unix domain socket connections only
local   all             all                                     trust
# IPv4 local connections:
host    all             all             127.0.0.1/32            trust
# IPv6 local connections:
host    all             all             ::1/128                 trust

# Allow replication connections from localhost, by a user with the replication privilege
local   replication     all                                     trust
host    replication     all             127.0.0.1/32            trust
host    replication     all             ::1/128                 trust

# Allow all connections from the Docker network
host    all             all             0.0.0.0/0               trust
EOF

# Restart the containers to apply the new configurations
echo "Restarting PostgreSQL containers..."
docker-compose restart diem-ecommerce-master-postgres diem-ecommerce-slave-postgres

# Wait for master PostgreSQL to be ready
until docker exec diem-ecommerce-master-postgres pg_isready -h localhost -U postgres; do
  echo "Waiting for master PostgreSQL to be ready..."
  sleep 2
done

echo "Master PostgreSQL is ready"

# Create replication user if not exists
docker exec diem-ecommerce-master-postgres psql -U postgres -c "DO \$\$ 
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'replicator') THEN
    CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'replicator';
  END IF;
END
\$\$;"

# Create replication slot if not exists
docker exec diem-ecommerce-master-postgres psql -U postgres -c "
DO \$\$ 
BEGIN
  IF NOT EXISTS (SELECT FROM pg_replication_slots WHERE slot_name = 'replication_slot_slave1') THEN
    PERFORM pg_create_physical_replication_slot('replication_slot_slave1', true);
  END IF;
END
\$\$;"

# Create archive directory on master
docker exec diem-ecommerce-master-postgres mkdir -p /var/lib/postgresql/data/archive
docker exec diem-ecommerce-master-postgres chmod 700 /var/lib/postgresql/data/archive

# Take a base backup for slave
echo "Taking base backup for slave..."
docker exec diem-ecommerce-slave-postgres bash -c 'rm -rf /var/lib/postgresql/data/*'
docker exec diem-ecommerce-slave-postgres bash -c 'pg_basebackup -h diem-ecommerce-master-postgres -U postgres -D /var/lib/postgresql/data -Fp -Xs -P -R'

# Create recovery.signal file in slave to start in recovery mode
docker exec diem-ecommerce-slave-postgres touch /var/lib/postgresql/data/recovery.signal

# Set proper permissions for PostgreSQL data directory
docker exec diem-ecommerce-slave-postgres bash -c 'chown -R postgres:postgres /var/lib/postgresql/data'
docker exec diem-ecommerce-slave-postgres bash -c 'chmod 700 /var/lib/postgresql/data'

# Restart slave to start replication
docker-compose restart diem-ecommerce-slave-postgres

echo "Waiting for slave PostgreSQL to be ready..."
sleep 10

# Check replication status
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT * FROM pg_stat_replication;"

echo "PostgreSQL replication setup complete"