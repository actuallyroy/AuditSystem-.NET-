#!/usr/bin/env python3
"""
PostgreSQL Database Query Tool for Audit System
Allows querying any table in the database and provides various utility functions.
"""

import psycopg2
import psycopg2.extras
import json
import sys
import argparse
from datetime import datetime
from typing import List, Dict, Any, Optional
import tabulate

class DatabaseQueryTool:
    """Tool for querying PostgreSQL database"""
    
    def __init__(self, 
                 host: str = "localhost", 
                 port: int = 5432, 
                 database: str = "retail-execution-audit-system",
                 username: str = "postgres", 
                 password: str = "123456"):
        """Initialize database connection"""
        self.connection_params = {
            'host': host,
            'port': port,
            'database': database,
            'user': username,
            'password': password
        }
        self.connection = None
        self.cursor = None
    
    def connect(self):
        """Establish database connection"""
        try:
            self.connection = psycopg2.connect(**self.connection_params)
            self.cursor = self.connection.cursor(cursor_factory=psycopg2.extras.RealDictCursor)
            print(f"✅ Connected to database: {self.connection_params['database']}")
            return True
        except Exception as e:
            print(f"❌ Failed to connect to database: {e}")
            return False
    
    def disconnect(self):
        """Close database connection"""
        if self.cursor:
            self.cursor.close()
        if self.connection:
            self.connection.close()
        print("🔌 Database connection closed")
    
    def execute_query(self, query: str, params: tuple = None) -> List[Dict[str, Any]]:
        """Execute a SELECT query and return results"""
        try:
            if params:
                self.cursor.execute(query, params)
            else:
                self.cursor.execute(query)
            
            results = self.cursor.fetchall()
            return [dict(row) for row in results]
        except Exception as e:
            print(f"❌ Query execution failed: {e}")
            return []
    
    def execute_update(self, query: str, params: tuple = None) -> int:
        """Execute an UPDATE/INSERT/DELETE query and return affected rows"""
        try:
            if params:
                self.cursor.execute(query, params)
            else:
                self.cursor.execute(query)
            
            self.connection.commit()
            return self.cursor.rowcount
        except Exception as e:
            print(f"❌ Update execution failed: {e}")
            self.connection.rollback()
            return 0
    
    def list_tables(self) -> List[str]:
        """List all tables in the database"""
        query = """
        SELECT table_name 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        ORDER BY table_name;
        """
        results = self.execute_query(query)
        return [row['table_name'] for row in results]
    
    def describe_table(self, table_name: str) -> List[Dict[str, Any]]:
        """Get table schema information"""
        query = """
        SELECT 
            column_name,
            data_type,
            is_nullable,
            column_default,
            character_maximum_length,
            numeric_precision,
            numeric_scale
        FROM information_schema.columns 
        WHERE table_name = %s 
        ORDER BY ordinal_position;
        """
        return self.execute_query(query, (table_name,))
    
    def get_table_data(self, table_name: str, limit: int = 100, offset: int = 0) -> List[Dict[str, Any]]:
        """Get data from a specific table"""
        query = f"SELECT * FROM {table_name} LIMIT %s OFFSET %s;"
        return self.execute_query(query, (limit, offset))
    
    def count_table_rows(self, table_name: str) -> int:
        """Count total rows in a table"""
        query = f"SELECT COUNT(*) as count FROM {table_name};"
        result = self.execute_query(query)
        return result[0]['count'] if result else 0
    
    def search_table(self, table_name: str, column: str, value: str) -> List[Dict[str, Any]]:
        """Search for specific value in a table column"""
        query = f"SELECT * FROM {table_name} WHERE {column} ILIKE %s;"
        return self.execute_query(query, (f"%{value}%",))
    
    def get_table_stats(self, table_name: str) -> Dict[str, Any]:
        """Get comprehensive table statistics"""
        stats = {
            'table_name': table_name,
            'row_count': self.count_table_rows(table_name),
            'columns': self.describe_table(table_name),
            'sample_data': self.get_table_data(table_name, limit=5)
        }
        return stats
    
    def print_table_data(self, data: List[Dict[str, Any]], title: str = "Query Results"):
        """Print table data in a formatted way"""
        if not data:
            print(f"\n📊 {title}: No data found")
            return
        
        print(f"\n📊 {title} ({len(data)} rows)")
        print("=" * 80)
        
        # Convert data to list of lists for tabulate
        headers = list(data[0].keys())
        rows = []
        
        for row in data:
            formatted_row = []
            for key in headers:
                value = row[key]
                if isinstance(value, dict):
                    formatted_row.append(json.dumps(value, indent=2)[:100] + "..." if len(str(value)) > 100 else json.dumps(value))
                elif isinstance(value, datetime):
                    formatted_row.append(value.strftime("%Y-%m-%d %H:%M:%S"))
                else:
                    formatted_row.append(str(value)[:100] + "..." if len(str(value)) > 100 else str(value))
            rows.append(formatted_row)
        
        print(tabulate.tabulate(rows, headers=headers, tablefmt="grid"))
    
    def interactive_mode(self):
        """Interactive query mode"""
        print("\n🔍 Interactive Database Query Mode")
        print("Commands:")
        print("  'tables' - List all tables")
        print("  'desc <table>' - Describe table structure")
        print("  'select <table>' - Show table data")
        print("  'count <table>' - Count table rows")
        print("  'sql <query>' - Execute custom SQL")
        print("  'quit' - Exit interactive mode")
        print()
        
        while True:
            try:
                command = input("db> ").strip()
                
                if command.lower() == 'quit':
                    break
                elif command.lower() == 'tables':
                    tables = self.list_tables()
                    print(f"\n📋 Available tables ({len(tables)}):")
                    for table in tables:
                        print(f"  • {table}")
                
                elif command.lower().startswith('desc '):
                    table_name = command[5:].strip()
                    columns = self.describe_table(table_name)
                    if columns:
                        print(f"\n📝 Table structure for '{table_name}':")
                        for col in columns:
                            nullable = "NULL" if col['is_nullable'] == 'YES' else "NOT NULL"
                            print(f"  • {col['column_name']}: {col['data_type']} {nullable}")
                    else:
                        print(f"❌ Table '{table_name}' not found")
                
                elif command.lower().startswith('select '):
                    table_name = command[7:].strip()
                    data = self.get_table_data(table_name, limit=10)
                    self.print_table_data(data, f"Data from '{table_name}'")
                
                elif command.lower().startswith('count '):
                    table_name = command[6:].strip()
                    count = self.count_table_rows(table_name)
                    print(f"\n📊 Table '{table_name}' has {count} rows")
                
                elif command.lower().startswith('sql '):
                    sql_query = command[4:].strip()
                    if sql_query.upper().startswith('SELECT'):
                        results = self.execute_query(sql_query)
                        self.print_table_data(results, "Custom Query Results")
                    else:
                        affected = self.execute_update(sql_query)
                        print(f"✅ Query executed. {affected} rows affected.")
                
                else:
                    print("❌ Unknown command. Type 'quit' to exit.")
                    
            except KeyboardInterrupt:
                print("\n👋 Goodbye!")
                break
            except Exception as e:
                print(f"❌ Error: {e}")


def main():
    """Main function with command line interface"""
    parser = argparse.ArgumentParser(description='PostgreSQL Database Query Tool')
    parser.add_argument('--host', default='localhost', help='Database host')
    parser.add_argument('--port', type=int, default=5432, help='Database port')
    parser.add_argument('--database', default='retail-execution-audit-system', help='Database name')
    parser.add_argument('--username', default='postgres', help='Database username')
    parser.add_argument('--password', default='123456', help='Database password')
    parser.add_argument('--table', help='Table to query')
    parser.add_argument('--limit', type=int, default=100, help='Limit results')
    parser.add_argument('--interactive', action='store_true', help='Interactive mode')
    parser.add_argument('--list-tables', action='store_true', help='List all tables')
    parser.add_argument('--stats', help='Show table statistics')
    parser.add_argument('--sql', help='Execute custom SQL query')
    
    args = parser.parse_args()
    
    # Create database tool instance
    db_tool = DatabaseQueryTool(
        host=args.host,
        port=args.port,
        database=args.database,
        username=args.username,
        password=args.password
    )
    
    # Connect to database
    if not db_tool.connect():
        sys.exit(1)
    
    try:
        if args.interactive:
            db_tool.interactive_mode()
        elif args.list_tables:
            tables = db_tool.list_tables()
            print(f"\n📋 Available tables ({len(tables)}):")
            for table in tables:
                count = db_tool.count_table_rows(table)
                print(f"  • {table} ({count} rows)")
        elif args.stats:
            stats = db_tool.get_table_stats(args.stats)
            print(f"\n📊 Statistics for table '{args.stats}':")
            print(f"  Rows: {stats['row_count']}")
            print(f"  Columns: {len(stats['columns'])}")
            print("\n📝 Column details:")
            for col in stats['columns']:
                nullable = "NULL" if col['is_nullable'] == 'YES' else "NOT NULL"
                print(f"  • {col['column_name']}: {col['data_type']} {nullable}")
            
            if stats['sample_data']:
                db_tool.print_table_data(stats['sample_data'], "Sample Data")
        elif args.table:
            data = db_tool.get_table_data(args.table, limit=args.limit)
            db_tool.print_table_data(data, f"Data from '{args.table}'")
        elif args.sql:
            if args.sql.upper().startswith('SELECT'):
                results = db_tool.execute_query(args.sql)
                db_tool.print_table_data(results, "Custom Query Results")
            else:
                affected = db_tool.execute_update(args.sql)
                print(f"✅ Query executed. {affected} rows affected.")
        else:
            # Default: show all tables
            tables = db_tool.list_tables()
            print(f"\n📋 Available tables ({len(tables)}):")
            for table in tables:
                count = db_tool.count_table_rows(table)
                print(f"  • {table} ({count} rows)")
            print("\nUse --interactive for interactive mode or --help for more options.")
    
    finally:
        db_tool.disconnect()


if __name__ == "__main__":
    main() 