import sqlglot
x = sqlglot.transpile("SELECT EPOCH_MS(1618088028295)", read="duckdb", write="sqlite")[0]
print(x)

x = sqlglot.transpile("SELECT STRFTIME(x, '%y-%-m-%S')", read="duckdb", write="sqlite")[0]
print(x)

# Spark SQL requires backticks (`) for delimited identifiers and uses `FLOAT` over `REAL`
sql = """WITH baz AS (SELECT a, c FROM foo WHERE a = 1) SELECT f.a, b.b, baz.c, CAST("b"."a" AS REAL) d FROM foo f JOIN bar b ON f.a = b.a LEFT JOIN baz ON f.a = baz.a"""

# Translates the query into Spark SQL, formats it, and delimits all of its identifiers
print(sqlglot.transpile(sql, write="spark", identify=True, pretty=True)[0])

sql = """
/* multi
   line
   comment
*/
SELECT
  tbl.cola /* comment 1 */ + tbl.colb /* comment 2 */,
  CAST(x AS SIGNED), # comment 3
  y               -- comment 4
FROM
  bar /* comment 5 */,
  tbl #          comment 6
"""

# Note: MySQL-specific comments (`#`) are converted into standard syntax
print(sqlglot.transpile(sql, read='mysql', pretty=True)[0])

from sqlglot import parse_one, exp

# print all column references (a and b)
for column in parse_one("SELECT a, b + 1 AS c FROM d").find_all(exp.Column):
    print(column.alias_or_name)

# find all projections in select statements (a and c)
for select in parse_one("SELECT a, b + 1 AS c FROM d").find_all(exp.Select):
    for projection in select.expressions:
        print(projection.alias_or_name)

# find all tables (x, y, z)
for table in parse_one("SELECT * FROM x JOIN y JOIN z").find_all(exp.Table):
    print(table.name)

# import sqlglot.errors
# try:
#     sqlglot.transpile("SELECT foo FROM (SELECT baz FROM t")
# except sqlglot.errors.ParseError as e:
#     print(e.errors)

from sqlglot import select, condition
where = condition("x=1").and_("y=1")
print(select("*").from_("y").where(where).sql())

from sqlglot import exp, parse_one

expression_tree = parse_one("SELECT a FROM x")

def transformer(node):
    if isinstance(node, exp.Column) and node.name == "a":
        return parse_one("FUN(a)")
    return node

transformed_tree = expression_tree.transform(transformer)
print(transformed_tree.sql())

import sqlglot
from sqlglot.optimizer import optimize

print(
    optimize(
        sqlglot.parse_one("""
            SELECT A OR (B OR (C AND D))
            FROM x
            WHERE Z = date '2021-01-01' + INTERVAL '1' month OR 1 = 0
        """),
        schema={"x": {"A": "INT", "B": "INT", "C": "INT", "D": "INT", "Z": "STRING"}}
    ).sql(pretty=True)
)

from sqlglot import parse_one
print(repr(parse_one("SELECT a + 1 AS z")))

from sqlglot import diff, parse_one
print(diff(parse_one("SELECT a + b, c, d"), parse_one("SELECT c, a - b, d")))

from sqlglot.executor import execute

tables = {
    "sushi": [
        {"id": 1, "price": 1.0},
        {"id": 2, "price": 2.0},
        {"id": 3, "price": 3.0},
    ],
    "order_items": [
        {"sushi_id": 1, "order_id": 1},
        {"sushi_id": 1, "order_id": 1},
        {"sushi_id": 2, "order_id": 1},
        {"sushi_id": 3, "order_id": 2},
    ],
    "orders": [
        {"id": 1, "user_id": 1},
        {"id": 2, "user_id": 2},
    ],
}

print(
execute(
    """
    SELECT
      o.user_id,
      SUM(s.price) AS price
    FROM orders o
    JOIN order_items i
      ON o.id = i.order_id
    JOIN sushi s
      ON i.sushi_id = s.id
    GROUP BY o.user_id
    """,
    tables=tables
)
)
