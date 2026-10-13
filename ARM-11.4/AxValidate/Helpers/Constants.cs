namespace AxValidate.Helpers
{
    public class Consts
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
            VIEWFILTERS
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
            MSSQL
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
            ENTITY_FORM
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
        
         public const int CONST_MAX_DATASETS = 10;
        // public const string DBTYPE = "POSTGRE";
        public const string xml_Name = "a1";
        public const string xml_Caption = "a2";
        public const string xml_Purpose = "a59";
    //tstruct properties
        public const string xml_Transid        = "a1";
    //  public const string xml_Folder         = "a3";
        public const string xml_SaveControl    = "a4";
        public const string xml_DelControl     = "a5";
        public const string xml_Track          = "a6";
        public const string xml_NoChange       = "a7";
        public const string xml_ExprSet        = "a8";
        public const string xml_DateField      = "a9";

        public const string xml_OnOpen         = "a10";
        public const string xml_AfterLoadData  = "a11";
        public const string xml_BeforeSave      = "a12";
        public const string xml_AfterSave      = "a13";
        public const string xml_BeforeDelete   = "a14";
        public const string xml_AfterDelete    = "a15";
        public const string xml_navigate = "a16";
        public const string xml_TreeParents  = "a17";
        public const string xml_Attach = "a18";
        public const string xml_SearchCondition = "a19";
        public const string xml_ListView = "a20";
        public const string xml_TrackUsers = "a21";
        public const string xml_TrackFields = "a22";
        public const string xml_BlockSize  = "a23";
        public const string xml_Schema = "a24";
        public const string xml_WorkFlow = "a25";
        public const string xml_SaveDraft = "a26";
        public const string xml_Traceability = "a27";     //ch1
        public const string xml_CachingType = "a28";
        public const string xml_DcNavigation = "a29";
        public const string xml_Pos = "a30";

    //dc properties
        public const string xml_FrameNo        = "a3";
        public const string xml_PageNo         = "a4";
        public const string xml_Table          = "a5";
        public const string xml_AsGrid         = "a6";
        public const string xml_AllowChange = "a7";
        public const string xml_Pop = "a8";
        public const string xml_PopUp = "a9";
        public const string xml_PopParent = "a10";
        public const string xml_PopControl = "a11";
        public const string xml_PopOn = "a12";
        public const string xml_PopValidate = "a13";
        public const string xml_dcborder="a14";

        public const string xml_OnEnterDC="a15";
        public const string xml_OnExitDC="a16";
        public const string xml_DcAllowEmpty="a17";
        public const string xml_Heading="a18";
        public const string xml_ParentDC="a19";
        public const string xml_ParentFlds="a20";
        public const string xml_ShowButton="a21";
        public const string xml_PopAutoShow="a22";
        public const string xml_PopCondition="a23";
        public const string xml_AutoFill="a24";
        public const string xml_DispSum="a25";
        public const string xml_AddDcRows="a26";
        public const string xml_DeleteDcRows="a27";

    //fields properties
        public const string xml_DataType = "a3";
        public const string xml_DataWidth = "a4";
        public const string xml_Dec = "a5";
        public const string xml_MOE = "a6";
        public const string xml_Details = "a7";
        public const string xml_Hide = "a8";
        public const string xml_Empty = "a9";
        public const string xml_ReadOnly = "a10";
        public const string xml_SetCarry = "a11";
        public const string xml_Save = "a12";
        public const string xml_Expr = "a13";
        public const string xml_VExp = "a14";
        public const string xml_Duplicate = "a15";
        public const string xml_OnlyPositive = "a16";
        public const string xml_SourceKey = "a17";
        public const string xml_CField = "a18";
        public const string xml_DispTot = "a19";
        public const string xml_SGWidth = "a20";
        public const string xml_SGHeight = "a21";
        public const string xml_SQL = "a22";
        public const string xml_AutoSelect = "a23";
        public const string xml_Refresh = "a24";
        public const string xml_List = "a25";
        public const string xml_Source = "a26";
        public const string xml_FParent = "a27";
        public const string xml_FSource = "a28";
        public const string xml_SourceTable = "a29";
        public const string xml_Suggestive = "a30";
        public const string xml_gui = "a31";
        public const string xml_cwidth = "a32";
        public const string xml_tlhw = "a33";
        public const string xml_cfont = "a34";
        public const string xml_ccolor = "a35";
        public const string xml_bordercolor = "a36";
        public const string xml_showsno="a37";
        public const string xml_Trans = "a40";
        public const string xml_Sequence = "a41";
        public const string xml_Prefix = "a42";
        public const string xml_Desc = "a43";
        public const string xml_StartNo = "a44";
        public const string xml_SActive = "a45";
        public const string xml_PrefixField = "a53";
        public const string xml_Digits = "a54";

        public const string xml_OnEnterField="a46";
        public const string xml_OnExitField="a47";

        public const string xml_Mask = "a48";
        public const string xml_Pattern = "a49";
        public const string xml_PWordChar = "a50";
        public const string xml_Hint = "a51";
        public const string xml_ApplyComma = "a52";
        public const string xml_CValidate = "a55";
        public const string xml_FormCtrl  = "a56";
        public const string xml_ExprFlds = "a57";
        public const string xml_Deps = "a58";
        public const string xml_DetStruct = "a60";
        public const string xml_DetTransid = "a61";
        public const string xml_DetMap = "a62";
        public const string xml_Dynamic = "a63";
        public const string xml_SearchSQL = "a64";
        public const string xml_displaydetail = "a65";
        public const string xml_ParentDeps = "a66"; //only for web
        public const string xml_Tabstop ="a67";     // focus the read only field
        public const string xml_CustDType = "a69";
        public const string xml_TableTypeDetails = "a70";
    //fillgrid properties;
        public const string xml_IView = "a2";
        public const string xml_FGCaption = "a4";
        public const string xml_FGSQL = "a5";
        public const string xml_FGTarget = "a6";
        public const string xml_FMap = "a7";
        public const string xml_Multi = "a8";
        public const string xml_AutoShow = "a9";
        public const string xml_SourceDC = "a10";
        public const string xml_FGValidate = "a11";
        public const string xml_OnFill = "a12";
        public const string xml_ExeOnSave = "a13";
        public const string xml_FirmBind = "a14";
        public const string xml_SelectOn = "a15";
        public const string xml_FooterStr = "a16";
        public const string xml_FGVExp  = "a17";
        public const string xml_AddRows  = "a18";
        public const string xml_FGColProps = "a19";
        public const string xml_SelectAllRows = "a20";

    //genmap properties;
        public const string xml_GenTarget = "a3";
        public const string xml_GenParent = "a4";
        public const string xml_GenControl = "a5";
        public const string xml_Active = "a6";
        public const string xml_GenCField = "a7";
        public const string xml_GMap = "a8";
        public const string xml_OnPost = "a9";
        public const string xml_GenTransId = "a10";
        public const string xml_OnApprove = "a17"; // this is applicable for mdmap also.
        public const string xml_OnReject = "a18"; // this is applicable for mdmap also.

    //mdmap properties
        public const string xml_Master = "a3";
        public const string xml_MField = "a4";
        public const string xml_DField = "a5";
        public const string xml_MSField = "a6";
        public const string xml_DSField = "a7";
        public const string xml_MTable = "a8";
        public const string xml_Type = "a9";
        public const string xml_Tree = "a10";
        public const string xml_MDControl = "a11";
        public const string xml_Append = "a12";
        public const string xml_InitOnDel = "a13";
        public const string xml_MasterFieldDec = "a14";
        public const string xml_MapText = "a15";
        public const string xml_Extended = "a16";

    }
}
