using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDb.Enums;
using SharpDb.Models;
using SharpDb.Services;
using SharpDb.Services.Parsers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpDbUnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void GetColumns_Happy()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable";

            SelectParser selectParser = new SelectParser();

            //act
            IList<string> columns = selectParser.GetColumns(query);

            //assert
            Assert.AreEqual(columns[0], "name");
            Assert.AreEqual(columns[1], "origin");
            Assert.AreEqual(columns[2], "space");
            Assert.AreEqual(columns[3], "truck");
        }

        [TestMethod]
        public void GetTableName_Happy()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable where origin > 2";

            SelectParser selectParser = new SelectParser();

            //act
            string tableName = selectParser.GetTableName(query);

            //assert
            Assert.AreEqual(tableName, "sometable");
        }

        [TestMethod]
        public void QueryHasWhereClause()
        {
            //arrange
            string query = "select name, origin, space, truck from someTable where origin > 2";

            SelectParser selectParser = new SelectParser();

            //act
            int idx = selectParser.IndexOfWhereClause(query, "sometable");

            //assert
            Assert.AreEqual(7, idx);
        }

        [TestMethod]
        public void QueryHasWhereClause_No_False_Alarm()
        {
            //arrange
            string query = "select where, origin, space, where from someTable";

            SelectParser selectParser = new SelectParser();

            //act
            int idx = selectParser.IndexOfWhereClause(query, "someTable");

            //assert
            Assert.AreEqual(-1, idx);
        }

        [TestMethod]
        public void ParsePredicates_One_Predicate()
        {
            //arrange
            string query = "select where, origin, space, where from someTable where origin > 8";

            SelectParser selectParser = new SelectParser();

            //act
            List<string> predicates = selectParser.ParsePredicates(query);

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
        }

        [TestMethod]
        public void ParsePredicates_Multiple_Predicates()
        {
            //arrange
            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'ford' 
                            OR space = 98";

            SelectParser selectParser = new SelectParser();

            //act
            List<string> predicates = selectParser.ParsePredicates(query);

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("AND truck = 'ford'", predicates[1]);
            Assert.AreEqual("OR space = 98", predicates[2]);
        }

        [TestMethod]
        public void ParsePredicates_Multiple_Predicates_With_Spaces_In_Strings()
        {
            //arrange
            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = '   ford' 
                            OR space = 98";

            SelectParser selectParser = new SelectParser();

            //act
            List<string> predicates = selectParser.ParsePredicates(query);

            //assert
            Assert.AreEqual("where origin > 8", predicates[0]);
            Assert.AreEqual("AND truck = '   ford'", predicates[1]);
            Assert.AreEqual("OR space = 98", predicates[2]);
        }

        [TestMethod]
        public void GetFirstMostInnerSelectStatement_Happy()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (select truck from someTruckTable) 
                            OR space = 98";


            //act
            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            //assert
            Assert.AreEqual("select truck from someTruckTable", subquery.Statement);
            Assert.AreEqual(130, subquery.StartIndexOfOpenParantheses);
            Assert.AreEqual(163, subquery.EndIndexOfCloseParantheses);
        }


        [TestMethod]
        public void GetFirstMostInnerSelectStatement_Parses_With_Spaces()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (   select truck from someTruckTable   ) 
                            OR space = 98";


            //act
            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            //assert
            Assert.AreEqual("   select truck from someTruckTable   ", subquery.Statement);
            Assert.AreEqual(130, subquery.StartIndexOfOpenParantheses);
            Assert.AreEqual(169, subquery.EndIndexOfCloseParantheses);
        }

        [TestMethod]
        public void ReplaceSubqueryWithValue()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (   select truck from someTruckTable   ) 
                            OR space = 98";

            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            var interpreter = new Interpreter(
                new SelectParser(),
                new InsertParser(new SchemaFetcher()),
                new Reader(),
                new Writer(),
                new SchemaFetcher(),
                new GeneralParser(),
                new CreateParser());

            var expected = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'F-150' 
                            OR space = 98";

            //act
            var newQuery = interpreter.ReplaceSubqueryWithValue(query, subquery, "F-150", TypeEnums.String);


            //assert
            Assert.AreEqual(expected, newQuery);
        }

        [TestMethod]
        public void ReplaceSubqueryWithValue_Handle_New_Lines()
        {
            //arrange
            SelectParser selectParser = new SelectParser();

            string query = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = (  
                                            select truck from someTruckTable   
                                        ) 
                            OR space = 98";

            InnerStatement subquery = selectParser.GetFirstMostInnerParantheses(query);

            var interpreter = new Interpreter(
                new SelectParser(),
                new InsertParser(new SchemaFetcher()),
                new Reader(),
                new Writer(),
                new SchemaFetcher(),
                new GeneralParser(),
                new CreateParser());

            var expected = @"select truck, origin, space
                            from someTable where origin > 8
                            AND truck = 'F-150' 
                            OR space = 98";

            //act
            var newQuery = interpreter.ReplaceSubqueryWithValue(query, subquery, "F-150", TypeEnums.String);


            //assert
            Assert.AreEqual(expected, newQuery);
        }

        [TestMethod]
        public void InsertParser_ParseTableName()
        {
            //arrange
            var interpreter = new Interpreter(
                new SelectParser(),
                new InsertParser(new SchemaFetcher()),
                new Reader(),
                new Writer(),
                new SchemaFetcher(),
                new GeneralParser(),
                new CreateParser());

            var insertParser = new InsertParser(new SchemaFetcher());

            string dml = "insert into myTable VALUES ('one', 'two', 'three')";
            string expected = "mytable";

            //act
            string tableName = insertParser.ParseTableName(dml);

            //assert
            Assert.AreEqual(expected, tableName);
        }

        [TestMethod]
        public void GetOuterMostParantheses()
        {
            //arrange
            var genParser = new GeneralParser();

            string dml = @"create table houses(
                                Address varchar(100),
                                Price decimal
                            )";

            string expected = @"
                                Address varchar(100),
                                Price decimal
                            ";

            //act
            string statement = genParser.GetOuterMostParantheses(dml).Statement;

            //assert
            Assert.AreEqual(expected, statement);
        }

        [TestMethod]
        public void GetInnerMostSelectStatement()
        {
            //arrange
            string select = @"select * from somTable WHERE col IN (3,4,5) and colz = (select * from blag)";

            var parser = new SelectParser();

            //act

            string result = parser.GetInnerMostSelectStatement(select).Statement;

            Assert.AreEqual("select * from blag", result);
        }

        [TestMethod]
        public void GetInnerMostSelectStatement_WithNewLines()
        {
            //arrange
            string select = @"select * from somTable WHERE col IN (3,4,5) and colz = (
                                    select * from blag

)";


            var parser = new SelectParser();

            var expected = @"
                                    select * from blag

";

            //act

            string result = parser.GetInnerMostSelectStatement(select).Statement;

            Assert.AreEqual(expected, result);
        }

    }
}
