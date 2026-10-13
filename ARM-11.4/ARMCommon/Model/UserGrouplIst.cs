using System.Data;
using NpgsqlTypes;


namespace ARMCommon.Model
{

    public class UserGroupName
    {
        public string ID { get; set; }
        public string Name { get; set; }

    }
    public class UserGrouplIst
    {
        public string usergroup { get; set; }
        public string grouptype { get; set; }

    }
    public class FormlIst
    {
        public string formname { get; set; }
        public string metadata { get; set; }
        public string? statuslist { get; set; }
    }

    public class dataTablelist
    {
        public string formname { get; set; }
        public object paneldata { get; set; }
        public string status { get; set; }
        public string keyvalue { get; set; }
        public string Keyfiled { get; set; }
    }



    public class DBParamsList
    {
        public string? Name { get; set; }
        public DbType? Type { get; set; }
        public string? Value { get; set; }
    }


    public class DBParamsDetails
    {
        public List<DBParamsList>? ParamsNames { get; set; }
    }


    public class Role
    {
        public string ID { get; set; }
        public string Name { get; set; }

    }



    public class ConnectionParamsList
    {
        public string? Name { get; set; }
        public NpgsqlDbType? Type { get; set; }
        public string? Value { get; set; }
    }

    public class UserGroupDetails
    {
        public List<UserGrouplIst> userGroupNames { get; set; }
    }
    public class FormDetails
    {
        public List<FormlIst> formNames { get; set; }
    }


    public class ParamsDetails
    {
        public List<ConnectionParamsList>? ParamsNames { get; set; }
    }



}
