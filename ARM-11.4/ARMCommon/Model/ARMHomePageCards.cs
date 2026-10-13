using System.ComponentModel.DataAnnotations;

namespace ARMCommon.Model
{
    public class AxHomePageCard
    {
        [Key]
        public int CardId { get; set; }
        public string Caption { get; set; }
        public string PageCaption { get; set; }
        public string DisplayIcon { get; set; }
        public string Stransid { get; set; }
        public string Datasource { get; set; }
        public string MoreOption { get; set; }
        public string ColorCode { get; set; }
        public string GroupFolder { get; set; }
        public int GrpPageId { get; set; }
    }
}
