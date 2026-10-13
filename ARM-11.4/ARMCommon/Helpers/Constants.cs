namespace ARMCommon.Helpers
{
    public class Constants
    {

        public const string KeyForAxpertToken = "202308231803";
        #region Redis-Prefixes
        public const string sqlPrefix = "ARMSQL-";
        public const string dataPrefix = "ARMDS-";
        public const string validityPrefix = "ARMDS-Validity-";
        public const string metaDataPrefix = "ARMMETADATA-";
        public const string xmlPrefix = "ARMXML-";
        public const string structPrefix = "ARMSTRUCT-";
        #endregion

        #region Axpert-Redis-Keys
        public const string REDISOTPAUTH = "otpauth";
        public const string REDISPWDOTPAUTHLANG = "pwdotpauthlang";
        public const string AXEMAILSMTP_CONN_KEY = "AxEmailSMTPJSON";
        public const string AXPASSWORDPOL_CONN_KEY = "AxPwdPolicy";
        public const string AXLOGGEDUSER = "axloggedUser";
        #endregion

        #region ARM-Prefixes
        public const string SuccessText = "Success";
        public const string ErrorText = "Error";
        public const string RegistrationSuccess = "User added successfully";
        public const string INVALIDDATASOURCE = "Required fields (Connection details/Datasources) is missing in the input.";
        public const string INVALIDSESSION = "Invalid Session. Please try again.";
        #endregion

        public enum GROUPTYPES
        {
            POWER,
            INTERNAL,
            EXTERNAL
        };
        public enum SOURCETYPE
        {
            SQL,
            APIDEFINITION
        };
        public enum Roles
        {
            DEFAULT

        }
        public enum AXTASKS
        {
            APPROVE,
            REJECT,
            RETURN,
            FORWARD,
            MAKE,
            CHECK,
            SEND,
            BULKAPPROVE,
            SKIP,
            APPROVETO,
            RETURNTO
        }

        public enum TASKTYPE
        {
            APPROVE,
            MAKE,
            CHECK
        }
        public enum REDIS_PREFIX
        {
            ARMADDUSER,
            ARMSESSION,
            ARMRedisConfiguration,
            ARMPAGE,
            ARMUPDATEUSERDETAIL,
            AXINLINEFORM,




        }

        public enum DB_PREFIX
        {
            ARMCONNECTIONSTRING,
        }

        public enum AXPERT
        {
            AXPERT_WEB_URL,
            AXPERT_ARMWEBSCRIPTS_URL
        }

        public enum SESSION_DATA
        {
            USER_ROLES,
            ARMTOKEN,
            USERNAME,
            AXPERT_SESSIONID,
            APPNAME,
            USERGROUP,
            GROUPTYPE,
            CONNECTED_TO_AXPERT,
            PASSWORD,
            GLOBALVARS,
            USERVARS,
            USER_GROUP,
            AXAPPS,
            AXPROPS,
            SCRIPTSURL,
            SCHEMANAME,
            LANGUAGE,
            VIEWFILTERS,
            SITENO
        }

        public enum BIOMETRIC
        {
            FINGERPRINT,
            FACIALRECOGNIZATION

        }
        public enum RESULTS
        {
            INVALIDPASSWORD,
            CHANGEPASSWORD,
            NO_RECORDS,
            NO_TOKEN,
            SUCCESS,
            ERROR,
            INVALID_AXSESSION,
            INCORRECTPASSWORD,
            SAMEPASSWORD,
            POWERUSERNOTALLOWED,
            INSERTED,
            NOKEYAVAILABLEINREDIS,
            INVALIDPOWERUSER,
            INVALIDINTERNALENTERNALUSER,
            RECORDINSERTED,
            RECORDUPDATED
        }
        public enum DBTYPE
        {
            ORACLE,
            POSTGRE,
            MSSQL,
            MYSQL
        }

        public enum HTTPMETHOD
        {
            HTTPPOST,
            HTTPGET
        }
        public enum KEY
        {
            METADATA


        }

        public enum ANALYTICS_PAGES
        {
            ANALYTICS,
            ENTITY,
            ENTITY_FORM,
            DASHBOARD,
            HOMEPAGE,
            ACTIVELIST,
            INBOX,
            AXI,
            CUSTOM
        }

        public enum TSTRUCT_ACTIONS
        {
            CREATE,
            EDIT,
            DELETE,
            CANCEL,
            VIEW
        }

        public enum TSTRUCT_DATA_VARS
        {
            SELF, //Self role
            NONE, //None permission
            ALL //All permissions
        }

        public enum AXPERT_PROPERTIES
        {
            EMAIL,
            PASSWORD
        }

        public enum DATA_ACTION
        {
            INSERT,
            UPDATE,
            DELETE
        }

    }
}
