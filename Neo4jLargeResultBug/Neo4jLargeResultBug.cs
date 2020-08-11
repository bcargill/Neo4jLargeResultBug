using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo4jLargeResultBug
{
    /// <summary>
    /// uses the following docker setup:
    /// docker run `
	///      --name neo4j-foobar `
	///      --detach `
    ///      --publish=7474:7474 --publish=7687:7687 `
    ///      --volume=$HOME/neo4j/data:/data --volume=$HOME/neo4j/logs:/logs `
	///      --env NEO4J_dbms_memory_pagecache_size = 1G `
	///      --env NEO4J_dbms_memory_heap_max__size = 1G `
	///      --env NEO4J_AUTH = neo4j / foobar `
    ///      neo4j
    /// </summary>
    [TestClass]
    public class Neo4jLargeResultBug
    {
        private static IDriver _driver = null;
        [TestInitialize]
        public void Initialization()
        {
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "foobar"));
        }

        [TestCleanup]
        public void Teardown()
        {
            _driver.CloseAsync().Wait();
        }

        [TestMethod]
        public void Step2_RunTest()
        {
            var i = 0;
            var retval = new List<IRecord>();

            var session = _driver.AsyncSession();
            var cursor = session.RunAsync("match (p:Foo)-[:foobar]->(b:Bar) return p, b.keyval").Result;
            while (cursor.FetchAsync().Result)
            {
                i++;
                retval.Add(cursor.Current);
                Debug.WriteLine("Record: " + i);
            }
            Debug.WriteLine("Done");
            Debug.WriteLine("Total count: " + retval.Count);

            session.CloseAsync().Wait();
        }


        [TestMethod]
        public void Step1_CreateTestData()
        {
            var session = _driver.AsyncSession();

            Debug.WriteLine("Creating Foos");
            for (int i = 0; i < 50; i++)
            {
                var tx = session.BeginTransactionAsync().Result;
                for (int j = 0; j < 1000; j++)
                {
                    tx.RunAsync("CREATE (n:Foo $properties)",
                        new
                        {
                            properties = new Dictionary<string, object>
                                         {
                                            { "keyval", i },
                                            { "guid2", Guid.NewGuid().ToString() },
                                            { "guid3", Guid.NewGuid().ToString() },
                                            { "guid4", Guid.NewGuid().ToString() },
                                            { "guid5", Guid.NewGuid().ToString() },
                                            { "guid6", Guid.NewGuid().ToString() },
                                            { "guid7", Guid.NewGuid().ToString() },
                                            { "guid8", Guid.NewGuid().ToString() },
                                            { "guid9", Guid.NewGuid().ToString() },
                                            { "guid10", Guid.NewGuid().ToString() },
                                            { "guid11", Guid.NewGuid().ToString() },
                                            { "guid12", Guid.NewGuid().ToString() },
                                            { "guid13", Guid.NewGuid().ToString() }
                                         }
                        }).Wait();
                }
                tx.CommitAsync().Wait();
                Debug.Write(".");
            }

            Debug.WriteLine("Creating Bars");
            var bartx = session.BeginTransactionAsync().Result;
            for (int i = 0; i < 100; i++)
            {
                bartx.RunAsync("CREATE (n:Bar $properties)", new { properties = new Dictionary<string, object> { { "keyval", i } } }).Wait();
            }
            bartx.CommitAsync().Wait();

            Debug.WriteLine("Creating Foo index");
            session.RunAsync("CREATE INDEX fooidx FOR(n:Foo) ON(n.keyval)").Wait();

            Debug.WriteLine("Creating Bar index");
            session.RunAsync("CREATE INDEX baridx FOR(n:Bar) ON(n.keyval)").Wait();

            Debug.WriteLine("Creating foobar relationships");
            var reltx = session.BeginTransactionAsync().Result;
            for (int i = 0; i < 100; i++)
            {
                reltx.RunAsync($"MATCH (f:Foo),(b:Bar) WHERE f.keyval= {i} AND b.keyval= {i} CREATE(f)-[r: foobar]->(b) RETURN 1");
                Debug.Write(".");
            }
            reltx.CommitAsync().Wait();

            session.CloseAsync().Wait();
        }


        [TestMethod]
        public void Step3_DeleteTestData()
        {
            var session = _driver.AsyncSession();

            Debug.WriteLine("Deleting Foos");
            session.RunAsync("MATCH (n:Foo) DETACH DELETE n").Wait();

            Debug.WriteLine("Deleting Bars");
            session.RunAsync("MATCH (n:Bar) DETACH DELETE n").Wait();

            Debug.WriteLine("Deleting Fooidx");
            try { session.RunAsync("drop INDEX fooidx").Wait(); } catch (Exception) { }

            Debug.WriteLine("Deleting Baridx");
            try { session.RunAsync("drop INDEX baridx").Wait(); } catch (Exception) { }

            session.CloseAsync().Wait();
        }
    }
}