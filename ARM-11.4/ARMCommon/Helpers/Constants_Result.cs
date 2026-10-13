namespace ARMCommon.Helpers
{
    public class Constants_Result
    {
        public Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public Constants_Result()
        {
            #region APPSTATUS
            result.Add("APPISRUNNING", "Application is running successfully");
            #endregion
            #region LOGIN
            result.Add("SignIn", "SignIn Successful");
            result.Add("APPNOTEXIST", "App does not exist");
            result.Add("USERGROUPNOTEXIST" , "UserGroup does not exist");
            result.Add("INVALIDCREDENTIAL", "Invalid credentials.Username/Password/UserGroup is not valid.");
            result.Add("TOKENGENERATIONFAILED", "Issue in Signin token generation. Please try again later.");
            result.Add("EMAILEXISTS", "User is already register with given email address.");
            result.Add("USERIDEXISTS", "User is already register with given UserId.");
            result.Add( "REQUIREDFIELDMISS", "Required fields (Username/Email) is missing in the input.");
            result.Add("ALREADYREGISTERED" , "User is already registered in the system.");
            //result.Add("" , "Required fields(UserId) is missing in the input.");
            result.Add("INTERNALAUTHFAIED" , "Internal authentication failed. No matching user found.");
            result.Add("USERNAME/EMAIL_MISSING" , "Required fields (Username/Email) is missing from the internal authentication.");
            result.Add("NOREGISTRATIONFORPOWERUSERS" , "Registration is not allowed for Power User.");
            result.Add("ERRORINSENDINGOTP" , "Error in sending OTP to user email.");
            result.Add("OTPSENT",  "OTP sent successfully");
            result.Add("USERREGISTERED" , "User registered successfully.");
            result.Add("USERREGISTRATIONFAILED", "User registration failed. Please try again.");
            result.Add("OTPUNMATCHED", "OTP does not match.");
            result.Add("NOATTEMPTSLEFT", "No OTP attempts left. Please try to register/signup again.");
            result.Add("NOREGISTRATIONDATAFOUND", "No registration data found for given user. Please try to register/signup again.");
            result.Add("SSOFOREXTERNALONLY", " Google SSO Login is only for External Users.");
            result.Add("INVALIDCREDENTIALORTOKEN", "Invalid User Credentials/Token");
            result.Add("USERNOTEXISTS", "User doesn’t exist, Please register User");
            result.Add("USERREGISTEREDANDSIGNIN", " User Registered & SignIn Successful");
            result.Add("INVALIDBIOMETRIC", "Invalid Biometric");
            result.Add("BIOMETRICDISABLED", "Biometric Functionality is disabled for  the application");
            result.Add("INVALIDDEVICEID", "Invalid Device Id");
            result.Add("INTERNALEXTERNALUSERNOTEXISTS", "Internal / External User does not exist.");
            result.Add("NOPOWERUSERFOUND", "No Power User Found.");
            result.Add("DEVICEREGISTRATIONFAILED", "Device registration failed. Please try again.");
            result.Add("USERSIGNOUT", "User SignOut Successful");
            result.Add("PASSWORDSHOULDBEDIFFERENT", "Updated Password should be diffrent from current password");
            result.Add("INCORRECTPASSWORD", "Incorrect Password.Please try with correct Password");
            result.Add("PASSWORDUPDATIONFAILED" , "Updating Password Failed. Please try again after sometime.");
            result.Add("POWERUSERCANTUPDATEPASSWORD", "Power User can not update/reset password");
            result.Add("PASSWORDUPDATED", "Password Updated Sucessfully");
            result.Add("EMAILNOTEXISTS", "User Email doesn't exist");
            result.Add("REGID/OTP/EMAIL_ISMISSING", "Required fields(Regid / OTP / updatedPassword / email) is missing.");
            result.Add("PASSWORDRESETSUCCESSFULLY", "Password reset successfully.");
            result.Add("PASSWORDRESETFAILED", "Password reset Failed. Please try again.");
            result.Add("CONNECTIONESTABLISHED", "Connection Established");
            result.Add("ARMAUTHFAILED", "ARM authentication failed.");
            result.Add("AXPERTCONNECTIONESTABLISHED", "Axpert Connection Established.");
            result.Add("INVALIDSESSION", "SessionId is not valid");
            result.Add("NOUSERGROUPASSCIATED", "No UserGroup assosciated with this App");
            result.Add("INAVALIDUSERID", "Invalid UserId");
            result.Add("NOUSERDETAILSFOUND", "Unable to fetch User Details");
            result.Add("USERDETAILSFETCEHD", "User details fetched successfully");
            result.Add("DUPLICATEMOBILENO", "Another User is already registered with the same MobileNo");
            result.Add("EMAIL/MOBILERESETSUCCESSFULLY", "Email / Mobile reset successfully.");
            result.Add("EMAIL/MOBILERESETFAILED", "Email/Mobile reset Failed. Please try again.");
            result.Add("INACTIVEUSER", "Inactive User.");
            #endregion
            #region GETMENU
            result.Add("NORECORDINAXPAGES", "No Record exist in Axpages.");
            result.Add("NORECORD", "No Record exist.");
            result.Add("SUCCESS", "success");

            #endregion
            #region MobileNotification
            result.Add("RECORDUPDATED", "Record Updated Successfully");
            result.Add("RECORDINSERTED", "Record Inserted Successfully");
            #endregion
            #region InlineForm
            result.Add("FORMNAMEISMISSING", "Required fields (FormName) is missing in the input.");
            result.Add("INLINEFORMHASNORECORD", "No Record exist in Inline Form.");
            result.Add("1059", "No Record exist in  given PageName.");
            result.Add("1060", "Page registered successful");
            result.Add("1061", "No Record exist in Module Page.");
            result.Add("1062", "There is no status value defined in form");
            result.Add("1063", " Status Updated Successfully");
            result.Add("1064", "Issue in updating record");
            result.Add("1065", "Updated Status doesn't exist in form");
            #endregion
            #region InlineForm
            result.Add("QUEUEDATAINSERTEDSUCESSFULLY", "queue Inserted Successfully");
            result.Add("QUEUEDATAINSERTIONFAILED", "Pushing Data to queue failed");
          

            #endregion
            #region ARMPEG
            result.Add("PROCESSDATANOTAVAILABLE", "Process data is not available. Please try again.");
            result.Add("SESSIONEXPIRED", "Session is not available. Please re-login and try again.");
            result.Add("TASKDATANOTAVAILABLE", "Task data is not available. Please try again.");
            result.Add("INVALIDTASKTYPE", "Invalid Task type.");
            result.Add("NOTASKRESULTFOUND", "No Task Result Found");
            result.Add("ERRORWHILEREADING", "Error while reading data from file: ");
            result.Add("FILENOTFOUND", "file not found");
            result.Add("QUEUEDATA/QUEUENAME_MISSING", "Required fields (queuedata/queuename) is missing in the input.");
            result.Add("1068", "Process data is not available. Please try again.");
            result.Add("1069", "Session is not available. Please re-login and try again.");
            result.Add("1070", "Process data is not available. Please try again.");
            result.Add("1071", "Task data is not available. Please try again.");
            result.Add("1072", "Invalid Task type.");
            result.Add("1073", "No Task Result Found");
            result.Add("1074", "Error while reading data from file: ");
            result.Add("1075", "file not found");
            result.Add("1076", "Required fields (queuedata/queuename) is missing in the input.");
            result.Add("1078", "Required fields (queuedata/queuename) is missing in the input.");
            result.Add("PEG_APPROVED", "Approved successfully.");
            result.Add("PEG_REJECTED", "Rejected successfully.");
            result.Add("PEG_RETURNED", "Returned successfully.");
            result.Add("PEG_FORWARDED", "Forwarded successfully.");
            result.Add("PEG_CHECKED", "Checked successfully.");
            result.Add("PEG_SENT", "Sent successfully.");
            result.Add("PEG_SKIP", "Skipped successfully.");
            result.Add("PEG_BULKAPPROVED", "Bulk Approved successfully.");
            result.Add("INITIATOR_CANNOT_APPROVE", "Initiator is not allowed to perform action on this task.");
            #endregion

            result.Add("FORGOTPASSWORDUPDATED", "Password updated successfully. Please check to your registered mail.");
            result.Add("CHANGEPASSWORD", "Please Change Password");
            result.Add("NOTIFICATIONDISABLED", "Notification Disabled for user");
            result.Add("RECEIVEDNOTIFICATION", "Received Notification");
            result.Add("INVAILDDETAILS", "Invalid Details");
            result.Add("USERGROUPDOESNTEXIST", "UserGroup Doesnt Exist");
            result.Add("FILEEXTENSIONNOTSUPPORTED", "The target file extension is not supported.");
            result.Add("DATAINSERTIONFAILED", "Inserting data to Geofencing table failed");
            result.Add("NODATAFOUNDINSQLDATASOURCE", "No data found in SQLDataSource with given appname and datasource");
            result.Add("NODATAFOUNDINAPIDEFINITION", "No data found in APIDefinition with given appname and datasource");
            result.Add("SKIPNOTALLOWED", " Action Skip is not allowed");
            result.Add("TARGETFILEUNSUPPORTABLE" , "Target Type not supportable");
        }
    }

}
