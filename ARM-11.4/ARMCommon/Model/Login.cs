using Org.BouncyCastle.Asn1.Mozilla;
using System.ComponentModel.DataAnnotations.Schema;

namespace ARMCommon.Model
{ 
    public class ARMLoginUser
    {
        public string? appname { get; set; }
        public string? appdomain { get; set; }
        public string username { get; set; }
        public string? deviceid { get; set; }
        public string? useremail { get; set; }
        public string? usergroup { get; set; }
        public string? password { get; set; }
        public string? token { get; set; }
        public string? ssotoken { get; set; }
        public string? ssotype { get; set; }
        public string? sessionId { get; set; }

        public bool isfirsttime { get; set; }
        public bool? Trace { get; set; }
        public string? Language { get; set; }
        public string? SId { get; set; }
    }

    public class ARMUser
    {
        public Guid ID { get; set; }
        [Column("AppName")]
        public string appname { get; set; }
        [Column("UserName")]
        public string? username { get; set; }
        [NotMapped]
        public string? userid { get; set; }

        [Column("Password")]
        public string password { get; set; }

        [Column("Email")]
        public string? email { get; set; }
        [Column("MobileNo")]
        public string? mobileno { get; set; }
        [Column("UserGroupId")]
        public Guid usergroupid { get; set; }
        [Column("UserGroup")]
        public string usergroup { get; set; }
        [Column("IsActive")]
        public bool isactive { get; set; }
        [Column("InsertedOn")]
        public DateTime? insertedon { get; set; }
        [Column("ActivatedOn")]
        public DateTime? activatedon { get; set; }
        [NotMapped]
        public List<App>? app1 { get; set; }


        [Column("IsFirstTime")]
        public string? isfirsttime { get; set; }

        [NotMapped]
        public List<UserGroupName>? UserGroupName1 { get; set; }


    }
    public class ARMLoginSSO
    {
        public string appname { get; set; }
        public string? userid { get; set; }

        public string? username { get; set; }
        public string usergroup { get; set; }
        public Guid? usergroupid { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string? mobile { get; set; }
        public string? ssotype { get; set; }
        public Dictionary<string, string>? ssodetails { get; set; }
    }

    public class ARMValidateUser
    {
        public string regid { get; set; }
        public string otp { get; set; }
    }
    public class UpdatePassword
    {
        public string ARMSessionId { get; set; }
        public string? CurrentPassword { get; set; }
        public string? UpdatedPassword { get; set; }
    }
    public class UserDevice
    {

        public Guid id { get; set; }
        public string deviceid { get; set; }
        public string appname { get; set; }

        [NotMapped]
        public Guid userid { get; set; }


        public string? username { get; set; }

        [NotMapped]
        public string? usergroup { get; set; }
        [NotMapped]
        public Guid? usergroupid { get; set; }
        [NotMapped]
        public string? password { get; set; }
        [NotMapped]
        public string biometricType { get; set; }


    }
    public class ARMForgetPassword
    {
        public string? email { get; set; }
        public string? username { get; set; }
        public string? usergroup { get; set; }
        public string? appname { get; set; }

    }

    public class ARMValidatePassword
    {
        public string email { get; set; }
        public string appname { get; set; }
        public string? updatedPassword { get; set; }
        public string? regid { get; set; }
        public string? otp { get; set; }

    }

    public class ARMSession
    {
        public string ARMSessionId { get; set; }
        public string? CardId { get; set; }

        public bool? IsHomePage { get;set; }

        public bool Trace { get; set; } = false;
        public Dictionary<string, string>? SqlParams { get; set; }
        public string UserRoles { get; set; } = string.Empty;
        public string UserResponsibilities { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AxSessionId { get; set; } = string.Empty;
    }
    public class ARMGeoFencingModel
    {
        public string? ARMSessionid { get; set; }
        public string? identifier { get; set; }
        public string? current_name { get; set; }
        public string? current_lat { get; set; }
        public string? current_long { get; set; }
        public string? src_name { get; set; }
        public string? src_lat { get; set; }
        public string? src_long { get; set; }
        public int? distance { get; set; }
        public DateTime? axtimestamp { get; set; }
        public bool? is_withinradius { get; set; }
    }

    public class ARMUpdateLocationModel
    {
        public string? project { get; set; }
        public string? username { get; set; }
        public string? current_name { get; set; }
        public string? current_loc { get; set; }
        public string? expectedlocations { get; set; }
        public int? interval { get; set; }
        public string? queuename { get; set; }
        public string? logintime { get; set; }

        public string? identifier { get; set; }
        public string? location_array { get; set; }
    }

    public class GlobalSearch
    {
        public string ARMSessionId { get; set; }
        public string? UserName { get; set; }
        public string? Project { get; set; }

        public string SearchText { get; set; }

        public bool? Trace { get; set; }
    }
}

