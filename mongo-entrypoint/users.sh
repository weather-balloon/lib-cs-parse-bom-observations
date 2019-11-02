#!/usr/bin/env bash
echo "Creating mongo users..."

mongo admin --host localhost -u dba -p mongo --eval "db.createUser({user: 'loader', pwd: 'loaderpwd', roles: [{role: 'readWrite', db: 'BOM'}]});"

echo "Mongo users created."
