using System;
using System.Data;
using Core.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Core.Tests
{
    [TestClass]
    public class ImportTests
    {
        private readonly string connStr = "";
        private DataProvider dp = new DataProvider();

        [TestMethod]
        public void TestMethod1()
        {
            var ds = GetDataSet(@"select r.*,a.Url,a.FeedId from userreadarticle r
                                    inner join article a
                                    on r.ArticleId = a.Id");
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                int userId = dr.ToIntOrZero("UserId");
                int feedId = dr.ToIntOrZero("FeedId");
                string url = dr.ToStringOrNull("Url");
                int articleId = dr.ToIntOrZero("ArticleId");

                var newArticle = dp.GetFromSelect("select * from Article where Url = '" + url + "' and FeedId = " + feedId).ToArticles().FirstOrDefault();
                try
                {
                    if(newArticle != null && newArticle.Id != articleId)
                    {
                        dp.GetFromSelect("insert into UserReadArticle (UserId, ArticleId) values (" + userId + "," + newArticle.Id + ")");
                    }
                }
                catch
                {
                    
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private DataSet GetDataSet(string storedProcedure, Dictionary<string, object> parameters = null)
        {
            DataSet ds = new DataSet();

            var conn = new MySqlConnection(connStr);

            var cmd = new MySqlCommand
            {
                Connection = conn,
                CommandText = storedProcedure,
                CommandType = CommandType.Text
            };

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var paramName = p.Key;
                    cmd.Parameters.AddWithValue(paramName, p.Value);
                    cmd.Parameters[paramName].Direction = ParameterDirection.Input;
                }
            }

            try
            {
                MySqlDataAdapter da = new MySqlDataAdapter
                {
                    SelectCommand = cmd
                };
                conn.Open();
                da.Fill(ds);
                conn.Close();

                return ds;
            }
            catch
            {
                conn.Close();
                throw;
            }
        }
    }
}
