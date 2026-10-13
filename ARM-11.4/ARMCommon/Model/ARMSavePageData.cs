namespace ARMCommon.Model
{
    public class ARMSavePageData
    {
        public string formname { get; set; }
        public string pagename { get; set; }
        public string paneldata { get; set; }
        public string username { get; set; }
    }
    public class ARMGetPageData
    {
        public string PageName { get; set; }
        public string Keyvalue { get; set; }
    }


    public class Form
    {
        public string form { get; set; }
        public string keyfield { get; set; }
    }

    public class StatusUpdate
    {
        public string form { get; set; }
        public string pagename { get; set; }
        public string updatedstatus { get; set; }
    }

    public class ARMPageData
    {

        public Guid Id { get; set; }
        public string formname { get; set; }
        public string keyvalue { get; set; }
        public string paneldata { get; set; }
        public string? createddatetime { get; set; }
        public string formmodule { get; set; }
        public string formsubmodule { get; set; }
        public string? status { get; set; }



    }
    public class InlineForm
    {
        public string FormName { get; set; }
    }

}
