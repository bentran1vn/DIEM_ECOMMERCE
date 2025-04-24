#!/bin/bash
set -e

echo "Setting up PostgreSQL replication directly inside containers..."

# Wait for master PostgreSQL to be ready
until docker exec diem-ecommerce-master-postgres pg_isready -h localhost -U postgres; do
  echo "Waiting for master PostgreSQL to be ready..."
  sleep 2
done

echo "Master PostgreSQL is ready"

# Modify the master's pg_hba.conf file directly inside the container
docker exec -i diem-ecommerce-master-postgres bash << EOF
cat > /var/lib/postgresql/data/pg_hba.conf << INNEREOF
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

# Allow replication connections from anywhere
host    replication     postgres        0.0.0.0/0               trust
host    replication     replicator      0.0.0.0/0               trust

# Allow all connections from anywhere
host    all             all             0.0.0.0/0               trust
INNEREOF

chown postgres:postgres /var/lib/postgresql/data/pg_hba.conf
chmod 600 /var/lib/postgresql/data/pg_hba.conf

# Update the postgresql.conf to ensure replication settings
sed -i 's/^#*wal_level.*/wal_level = replica/' /var/lib/postgresql/data/postgresql.conf
sed -i 's/^#*max_wal_senders.*/max_wal_senders = 10/' /var/lib/postgresql/data/postgresql.conf
sed -i 's/^#*max_replication_slots.*/max_replication_slots = 10/' /var/lib/postgresql/data/postgresql.conf
sed -i 's/^#*hot_standby.*/hot_standby = on/' /var/lib/postgresql/data/postgresql.conf

# Create archive directory
mkdir -p /var/lib/postgresql/data/archive
chown postgres:postgres /var/lib/postgresql/data/archive
chmod 700 /var/lib/postgresql/data/archive
EOF

# Restart the master PostgreSQL to apply changes
echo "Restarting master PostgreSQL..."
docker-compose restart diem-ecommerce-master-postgres

# Wait for master PostgreSQL to be ready after restart
until docker exec diem-ecommerce-master-postgres pg_isready -h localhost -U postgres; do
  echo "Waiting for master PostgreSQL to be ready after restart..."
  sleep 2
done

echo "Master PostgreSQL is ready after restart"

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

# Reset slave and take a base backup
echo "Taking base backup for slave..."
docker exec diem-ecommerce-slave-postgres bash -c 'rm -rf /var/lib/postgresql/data/*'
docker exec diem-ecommerce-slave-postgres bash -c 'pg_basebackup -h diem-ecommerce-master-postgres -U postgres -D /var/lib/postgresql/data -Fp -Xs -P -R'

# Create recovery.signal file in slave to start in recovery mode
docker exec diem-ecommerce-slave-postgres touch /var/lib/postgresql/data/recovery.signal

# Set proper permissions for PostgreSQL data directory
docker exec diem-ecommerce-slave-postgres bash -c 'chown -R postgres:postgres /var/lib/postgresql/data'
docker exec diem-ecommerce-slave-postgres bash -c 'chmod 700 /var/lib/postgresql/data'

# Configure slave to use replication slot
docker exec -i diem-ecommerce-slave-postgres bash << EOF
cat > /var/lib/postgresql/data/postgresql.auto.conf << INNEREOF
# Replication settings added by setup script
primary_conninfo = 'host=diem-ecommerce-master-postgres port=5432 user=postgres application_name=slave1'
primary_slot_name = 'replication_slot_slave1'
INNEREOF

chown postgres:postgres /var/lib/postgresql/data/postgresql.auto.conf
chmod 600 /var/lib/postgresql/data/postgresql.auto.conf
EOF

# Restart slave to start replication
echo "Restarting slave PostgreSQL..."
docker-compose restart diem-ecommerce-slave-postgres

echo "Waiting for slave PostgreSQL to be ready..."
sleep 10

# Check replication status
docker exec diem-ecommerce-master-postgres psql -U postgres -c "SELECT * FROM pg_stat_replication;"

echo "PostgreSQL replication setup complete"