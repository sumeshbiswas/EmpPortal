using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.DropdownService
{
    public class DropdownService
    {
        //private readonly string _connectionString;
        // public DropdownService(IConfiguration configuration)
        //{
        //    _connectionString = configuration.GetConnectionString("ERPDB"); 
        //}
        private readonly DataBaseConnection _dbConnection;
        public DropdownService(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public List<object> GetDropdownList(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(), 
                        Text = reader[1].ToString()   
                    });
                }
            }
            return dropdownItems;   
        }
        public List<object> GetDropdownListcon(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString()
                    });
                }
            }
            return dropdownItems;
        }

        public List<object> GetDropdownListERP(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString()
                    });
                }
            }
            return dropdownItems;
        }

        public List<object> GetEmpdataList(string query)
        {
            List<object> dropdownItems = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString(),
                        Dept_id = reader[2].ToString(),
                        Desg_id = reader[3].ToString()
                    });
                }
            }
            return dropdownItems;

        }

        public List<object> GetEmpReasonList(string query)
        {
            List<object> dropdownItems = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString(),
                        deductType = reader[2].ToString()
                    });
                }
            }
            return dropdownItems;

        }
    }
}

