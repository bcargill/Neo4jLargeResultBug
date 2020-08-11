# Neo4jLargeResultBug
repro case for neo4j driver issue

requires a local docker of Neo4j 4.1.1:

<pre>
docker run `
    --name neo4j-foobar `
    --detach `
    --publish=7474:7474 `
    --publish=7687:7687 `
    --volume=$HOME/neo4j/data:/data `
    --volume=$HOME/neo4j/logs:/logs `
    --env NEO4J_dbms_memory_pagecache_size=2G `
    --env NEO4J_dbms_memory_heap_max__size=2G `
    --env NEO4J_AUTH=neo4j/foobar `
neo4j    
</pre>
