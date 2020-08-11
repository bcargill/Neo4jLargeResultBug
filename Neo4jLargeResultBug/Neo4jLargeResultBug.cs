using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Neo4jLargeResultBug
{
    /// <summary>
    ///
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
        public void Step1_CreateTestData()
        {
            var session = _driver.AsyncSession();
            var fooCount = 50;
            var barCount = 500;

            Debug.WriteLine("Creating Foos and Bars");
            for (int i = 0; i < barCount; i++)
            {
                
                session.RunAsync("CREATE (n:Bar $properties)", new { properties = new Dictionary<string, object> { { "keyval", i } } }).Wait();
                for (int j = 0; j < fooCount; j++)
                {
                    session.RunAsync("CREATE (n:Foo $properties)",
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
                Debug.Write(".");
            }

            Debug.WriteLine("Creating Foo index");
            session.RunAsync("CREATE INDEX fooidx FOR(n:Foo) ON(n.keyval)").Wait();

            Debug.WriteLine("Creating Bar index");
            session.RunAsync("CREATE INDEX baridx FOR(n:Bar) ON(n.keyval)").Wait();

            Debug.WriteLine("Creating foobar relationships");
            for (int i = 0; i < barCount; i++)
            {
                session.RunAsync($"MATCH (f:Foo),(b:Bar) WHERE f.keyval= {i} AND b.keyval= {i} CREATE(f)-[r: foobar]->(b) RETURN 1");
                Debug.Write(".");
            }
            
            session.CloseAsync().Wait();
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
