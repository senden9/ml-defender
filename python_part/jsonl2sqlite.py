import sqlite3
import json

def create_table(cursor, schema):
    columns = ', '.join([f'{column} {datatype}' for column, datatype in schema.items()])
    cursor.execute(f'CREATE TABLE IF NOT EXISTS my_table ({columns});')

def insert_data(cursor, data):
    columns = ', '.join(data.keys())
    placeholders = ', '.join(['?'] * len(data))
    values = tuple(data.values())
    cursor.execute(f'INSERT INTO my_table ({columns}) VALUES ({placeholders});', values)

def jsonl_to_sqlite(jsonl_file, db_file):
    # Connect to the SQLite database
    conn = sqlite3.connect(db_file)
    cursor = conn.cursor()

    # Read the JSONL file, derive the table schema, and insert data into the database
    with open(jsonl_file, 'r') as file:
        for line in file:
            data = json.loads(line)

            # Derive the table schema
            if 'table_schema' not in locals():
                table_schema = {key: type(value).__name__ for key, value in data.items()}
                create_table(cursor, table_schema)

            # Insert data into the database
            insert_data(cursor, data)

    # Commit the changes and close the connection
    conn.commit()
    conn.close()

# Usage example
jsonl_file = 'data.jsonl'
db_file = 'data.db'
jsonl_to_sqlite(jsonl_file, db_file)
